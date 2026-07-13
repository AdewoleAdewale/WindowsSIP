using SipCoreMobile.Data;
using SipCoreMobile.Data.Entities;
using SipCoreMobile.Services.Api;
using SipCoreMobile.Services.Sip;
using SipCoreMobile.Services.Storage;

namespace SipCoreMobile.Services.Notifications;

/// <summary>
/// Port of SipFirebaseMessagingService.kt's *routing and handling* logic (the onMessageReceived
/// dispatch, the WorkDesk/message/meeting handlers, and the middleware conversation sync).
///
/// Deliberately NOT a port of the transport itself: the original subclassed Firebase's
/// FirebaseMessagingService, which doesn't exist on Windows. Windows push normally goes
/// through WNS (Windows Notification Service) instead of FCM -- a different registration
/// and payload-delivery model entirely. Rather than guess at your WNS setup, this class
/// takes a plain `IReadOnlyDictionary&lt;string, string&gt;` payload (the same shape as
/// FCM's `RemoteMessage.data`), so whatever actually receives the push on Windows (a WNS
/// background task, a raw notification handler, or even a websocket/long-poll fallback) can
/// call straight into `Dispatch(...)` and get the exact same routing this had on Android.
///
/// Wire the real transport by implementing an entry point that:
///   1. Receives a WNS raw notification (or channel-URI-based push) with the same data keys
///      the server already sends for FCM (type, caller, senderExtension, body, ...), and
///   2. Calls PushMessageDispatcher.DispatchAsync(payload).
/// The server-side payload shape doesn't need to change for this to work.
/// </summary>
public class PushMessageDispatcher
{
    private readonly ISipCoreApiService _api;
    private readonly ISipCoreRepository _repository;

    public PushMessageDispatcher(ISipCoreApiService api, ISipCoreRepository repository)
    {
        _api = api;
        _repository = repository;
    }

    /// <summary>Call from your token-refresh handler (WNS channel URI changes similarly to an FCM token).</summary>
    public void OnNewToken(string token) => SipCoreTokenStore.SaveToken(token);

    public async Task DispatchAsync(IReadOnlyDictionary<string, string> data)
    {
        SipPushWakeHandler.WakeAndRegister();

        var type = data.GetValueOrDefault("type", "");

        switch (type)
        {
            case "incoming_call":
                HandleIncomingCallPush(data);
                break;

            case "message":
            case "incoming_message":
            case "chat_reaction":
            case "chat_delete":
            case "chat_read":
            case "chat_delivered":
                await HandleIncomingMessagePushAsync(data, type);
                break;

            case "work_task_created":
            case "work_task_updated":
            case "work_task_comment":
            case "work_task_completed":
            case "work_task_attachment":
            case "work_task_checklist_updated":
            case "work_task_approved":
            case "work_task_rejected":
            case "work_task_escalated":
            case "work_task_watcher_added":
            case "work_task_overdue":
            case "work_task_recurring_created":
            case "work_task_approval_requested":
                HandleWorkDeskPush(data);
                break;

            case "meeting_invite":
            case "meeting_updated":
                HandleMeetingPush(data);
                break;
        }
    }

    private static void HandleIncomingCallPush(IReadOnlyDictionary<string, string> data)
    {
        // Original just logged here and waited for the real SIP INVITE to arrive over the
        // registered transport -- preserved as-is; SipPushWakeHandler.WakeAndRegister()
        // above already ensures the SIP stack is registered and ready to receive it.
        _ = data.GetValueOrDefault("caller") ?? data.GetValueOrDefault("from") ?? "Unknown";
    }

    private async Task HandleIncomingMessagePushAsync(IReadOnlyDictionary<string, string> data, string pushType)
    {
        var senderExtension = PushTextUtils.CleanExtension(
            data.GetValueOrDefault("senderExtension") ?? data.GetValueOrDefault("sender") ?? data.GetValueOrDefault("from") ?? "");

        var senderDisplay = data.GetValueOrDefault("senderDisplay") ?? data.GetValueOrDefault("sender") ?? senderExtension;

        var destination = PushTextUtils.CleanExtension(data.GetValueOrDefault("destination") ?? "");
        var body = data.GetValueOrDefault("body") ?? "";
        var preview = data.GetValueOrDefault("preview") ?? PushTextUtils.CleanMessagePreview(body);

        var myExtension = GetLoggedInExtension();

        if (string.IsNullOrEmpty(senderExtension)) return;

        if (!string.IsNullOrEmpty(myExtension) && !string.IsNullOrEmpty(destination) && destination != myExtension)
            return;

        // For normal incoming messages, skip our own outgoing notification. For chat updates
        // (delete/reaction/read/delivered) don't skip -- they need to sync against an existing local row.
        if ((pushType is "message" or "incoming_message") && !string.IsNullOrEmpty(myExtension) && senderExtension == myExtension)
            return;

        if ((pushType is "message" or "incoming_message") && PushTextUtils.IsSystemMessage(body))
            return;

        await SyncConversationFromMiddlewareAsync(myExtension, senderExtension);

        if (pushType is "message" or "incoming_message")
        {
            NotificationHelper.ShowMessage(senderDisplay, preview);
        }
    }

    private void HandleMeetingPush(IReadOnlyDictionary<string, string> data)
    {
        var title = data.GetValueOrDefault("title") ?? "SIPCore Meeting";
        var body = data.GetValueOrDefault("body") ?? "Meeting invitation";
        var meetingId = data.GetValueOrDefault("meetingId") ?? "";
        var companyId = data.GetValueOrDefault("companyId") ?? "";
        var conferenceNumber = data.GetValueOrDefault("conferenceNumber") ?? "";

        NotificationHelper.ShowMeeting(title, body, meetingId, companyId, conferenceNumber);
    }

    private void HandleWorkDeskPush(IReadOnlyDictionary<string, string> data)
    {
        var title = data.GetValueOrDefault("title") ?? "WorkDesk";
        var body = data.GetValueOrDefault("body") ?? "Task updated";
        var taskId = data.GetValueOrDefault("taskId") ?? "";
        var companyId = data.GetValueOrDefault("companyId") ?? "";

        NotificationHelper.ShowWorkDeskTask(title, body, taskId, companyId);
    }

    private async Task SyncConversationFromMiddlewareAsync(string myExtension, string otherExtension)
    {
        var me = PushTextUtils.CleanExtension(myExtension);
        var other = PushTextUtils.CleanExtension(otherExtension);
        if (string.IsNullOrEmpty(me) || string.IsNullOrEmpty(other)) return;

        try
        {
            var remoteMessages = await _api.GetConversationAsync(me, other);

            foreach (var remote in remoteMessages)
            {
                var sender = PushTextUtils.CleanExtension(remote.SenderExtension);
                var receiver = PushTextUtils.CleanExtension(remote.ReceiverExtension);
                var conversationExtension = sender == me ? receiver : sender;
                if (string.IsNullOrEmpty(conversationExtension)) continue;

                var remoteMessageId = string.IsNullOrEmpty(remote.MessageId) ? Guid.NewGuid().ToString() : remote.MessageId;
                var existing = await _repository.GetMessageByMessageIdAsync(remoteMessageId);

                if (existing is null)
                {
                    await _repository.InsertMessageAsync(new ChatMessageEntity
                    {
                        MessageId = remoteMessageId,
                        ConversationId = conversationExtension,
                        Sender = sender,
                        Body = PushTextUtils.CleanMessagePreview(remote.Body),
                        IsMine = sender == me,
                        SentAt = ParseRemoteTime(remote.SentAt),
                        Status = string.IsNullOrEmpty(remote.Status) ? (sender == me ? "Sent" : "Received") : remote.Status,
                        IsDeleted = remote.IsDeleted,
                        Reaction = remote.Reaction,
                        ReplyToId = remote.ReplyToId,
                        ReplyPreview = remote.ReplyPreview
                    });
                }
                else
                {
                    await _repository.UpdateMessageFromMiddlewareAsync(
                        remoteMessageId,
                        PushTextUtils.CleanMessagePreview(remote.Body),
                        string.IsNullOrEmpty(remote.Status) ? existing.Status : remote.Status,
                        remote.IsDeleted,
                        remote.Reaction,
                        remote.ReplyToId,
                        remote.ReplyPreview);
                }
            }

            var latest = remoteMessages
                .OrderByDescending(m => ParseRemoteTime(m.SentAt))
                .FirstOrDefault();

            if (latest is not null)
            {
                var existingConversation = await _repository.GetConversationByExtensionAsync(other);
                var shouldIncreaseUnread = PushTextUtils.CleanExtension(latest.SenderExtension) != me;
                var unread = shouldIncreaseUnread ? (existingConversation?.UnreadCount ?? 0) + 1 : existingConversation?.UnreadCount ?? 0;

                await _repository.SaveConversationAsync(new ConversationEntity
                {
                    Extension = other,
                    LastMessage = PushTextUtils.CleanMessagePreview(latest.Body),
                    LastMessageAt = ParseRemoteTime(latest.SentAt),
                    UnreadCount = unread
                });
            }
        }
        catch
        {
            // best-effort sync, mirrors original's swallowed exception + logging
        }
    }

    private static long ParseRemoteTime(string value)
    {
        return DateTimeOffset.TryParse(value, out var dto)
            ? dto.ToUnixTimeMilliseconds()
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Original read from Android SharedPreferences ("sipcore_prefs"). Ported to
    /// Microsoft.Maui.Storage.Preferences, same key names.
    /// </summary>
    private static string GetLoggedInExtension()
    {
        var value = Preferences.Default.Get("extension", "", "sipcore_prefs");
        if (string.IsNullOrEmpty(value)) value = Preferences.Default.Get("username", "", "sipcore_prefs");
        if (string.IsNullOrEmpty(value)) value = Preferences.Default.Get("sipUsername", "", "sipcore_prefs");
        return PushTextUtils.CleanExtension(value);
    }
}
