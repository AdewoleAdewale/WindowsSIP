using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SipCoreMobile.Data;
using SipCoreMobile.Data.Entities;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services.Api;

namespace SipCoreMobile.ViewModels;

/// <summary>
/// Port of the messaging slice of MainActivity.kt: sendTextMessage(), openConversation(),
/// syncConversationFromMiddleware(), startChatRefresh(), reloadSelectedConversationFromDb(),
/// markVisibleIncomingMessagesRead(). Feature-scoped per the note in AppStateViewModel's
/// class remarks (Batch 4) -- this is the "ChatViewModel" that note said should exist
/// alongside the Chat screen rather than being crammed into AppStateViewModel.
///
/// Deliberately NOT covered in this pass: voice-note recording/playback UI state, the
/// long-press reaction/delete/reply message menu, and per-day message grouping in the list
/// (all present in the 1,508-line original ChatScreen). See README caveats for this batch.
/// </summary>
public partial class ChatViewModel : ObservableObject
{
    private readonly ISipCoreApiService _api;
    private readonly ISipCoreRepository _repository;
    private readonly AppStateViewModel _appState;

    [ObservableProperty] private string activeExtension = "";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string status = "";

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private CancellationTokenSource? _refreshCts;

    public ChatViewModel(ISipCoreApiService api, ISipCoreRepository repository, AppStateViewModel appState)
    {
        _api = api;
        _repository = repository;
        _appState = appState;
    }

    /// <summary>Port of visibleMessageBody(): strips the "SIPCORE_MSG|" wrapper for display.</summary>
    public static string VisibleMessageBody(string body)
    {
        if (!body.StartsWith("SIPCORE_MSG|", StringComparison.Ordinal)) return body;
        var parts = body.Split('|', 3);
        return parts.Length >= 3 ? parts[2] : body;
    }

    /// <summary>Port of getConversationId(): sorted, underscore-joined pair of extensions.</summary>
    private static string GetConversationId(string ext1, string ext2)
    {
        var pair = new[] { ext1, ext2 };
        Array.Sort(pair, StringComparer.Ordinal);
        return string.Join("_", pair);
    }

    // ---------------------------------------------------------------------
    // Open / load conversation
    // ---------------------------------------------------------------------

    /// <summary>Port of navigating to Chat with a blank extension ("New Message" mode in ChatScreen).</summary>
    public void StartNewMessage()
    {
        StopChatRefresh();
        ActiveExtension = "";
        Messages.Clear();
        Status = "";
    }

    public async Task OpenConversationAsync(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return;

        var cleanExtension = extension.Trim();
        ActiveExtension = cleanExtension;
        IsLoading = true;
        Status = "Loading conversation...";

        try
        {
            await SyncConversationFromMiddlewareAsync(cleanExtension);
            await _repository.MarkConversationReadAsync(cleanExtension);
            await ReloadFromDbAsync(cleanExtension);
            await _appState.LoadConversationsAsync();

            Status = "Conversation loaded";
            StartChatRefresh();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void StopChatRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts = null;
    }

    private void StartChatRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;
        var conversationExtension = ActiveExtension;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested && ActiveExtension == conversationExtension)
                {
                    try
                    {
                        await SyncConversationFromMiddlewareAsync(conversationExtension);
                        await MarkVisibleIncomingMessagesReadAsync(conversationExtension);
                        await ReloadFromDbAsync(conversationExtension);
                        await _appState.LoadConversationsAsync();
                    }
                    catch
                    {
                        // best-effort, mirrors original's safeRunSuspend wrapper
                    }

                    await Task.Delay(2000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected when StopChatRefresh() cancels the token
            }
        }, token);
    }

    // ---------------------------------------------------------------------
    // Send
    // ---------------------------------------------------------------------

    public async Task SendTextMessageAsync(string to, string text)
    {

        var cleanTo = to.Trim();
        var cleanText = text.Trim();
        var cleanFrom = _appState.Extension.Trim();
        if (string.IsNullOrEmpty(cleanTo) || string.IsNullOrEmpty(cleanText) || string.IsNullOrEmpty(cleanFrom)) return;

        var contact = _appState.Contacts.FirstOrDefault(c => c.Extension == cleanTo);
        if (contact is not null && contact.Status != "Active")
        {
            Status = "Extension is not active";
            return;
        }

        var visibleText = VisibleMessageBody(cleanText);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var messageId = Guid.NewGuid().ToString();

        var outgoing = new ChatMessage
        {
            Id = messageId,
            Extension = cleanTo,
            Body = visibleText,
            IsMine = true,
            Time = now,
            Status = MessageStatus.Sending
        };

        Messages.Add(outgoing);
        Status = "Sending message...";

        await _repository.InsertMessageAsync(new ChatMessageEntity
        {
            MessageId = messageId,
            ConversationId = cleanTo,
            Sender = cleanFrom,
            Body = visibleText,
            IsMine = true,
            SentAt = now,
            Status = MessageStatus.Sending.ToString(),
            IsDeleted = false
        });

        await _repository.SaveConversationAsync(new ConversationEntity
        {
            Extension = cleanTo,
            LastMessage = visibleText,
            LastMessageAt = now,
            UnreadCount = 0
        });

        try
        {
            await _api.SaveChatMessageAsync(new SaveChatMessageRequest
            {
                MessageId = messageId,
                ConversationId = GetConversationId(cleanFrom, cleanTo),
                SenderExtension = cleanFrom,
                ReceiverExtension = cleanTo,
                Body = visibleText,
                Status = MessageStatus.Sent.ToString(),
                IsDeleted = false
            });

            await _repository.UpdateMessageStatusAsync(messageId, MessageStatus.Sent.ToString());

            await SyncConversationFromMiddlewareAsync(cleanTo);
            await ReloadFromDbAsync(cleanTo);
            await _appState.LoadConversationsAsync();

            Status = "Message sent";
        }
        catch
        {
            await _repository.UpdateMessageStatusAsync(messageId, MessageStatus.Failed.ToString());
            var idx = Messages.ToList().FindIndex(m => m.Id == messageId);
            if (idx >= 0) Messages[idx] = outgoing with { Status = MessageStatus.Failed };

            Status = "Message failed";
        }
    }

    // ---------------------------------------------------------------------
    // Middleware sync / local reload (mirrors PushMessageDispatcher's version,
    // but keyed off "my extension" from AppStateViewModel rather than a push payload)
    // ---------------------------------------------------------------------

    private async Task SyncConversationFromMiddlewareAsync(string otherExtensionRaw)
    {
        var me = _appState.Extension.Trim();
        var other = otherExtensionRaw.Trim();
        if (string.IsNullOrEmpty(me) || string.IsNullOrEmpty(other)) return;

        var remoteMessages = await _api.GetConversationAsync(me, other);

        foreach (var remote in remoteMessages)
        {
            var sender = remote.SenderExtension.Trim();
            var receiver = remote.ReceiverExtension.Trim();
            var conversationId = sender == me ? receiver : sender;

            var existing = await _repository.GetMessageByMessageIdAsync(remote.MessageId);
            var remoteTime = ParseRemoteTimeOrNull(remote.SentAt);
            var finalSentAt = remoteTime ?? existing?.SentAt ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await _repository.UpsertMessageAsync(new ChatMessageEntity
            {
                MessageId = remote.MessageId,
                ConversationId = conversationId,
                Sender = sender,
                Body = VisibleMessageBody(remote.Body),
                IsMine = sender == me,
                SentAt = finalSentAt,
                Status = string.IsNullOrEmpty(remote.Status) ? (existing?.Status ?? MessageStatus.Sent.ToString()) : remote.Status,
                IsDeleted = remote.IsDeleted,
                Reaction = remote.Reaction,
                ReplyToId = remote.ReplyToId,
                ReplyPreview = remote.ReplyPreview
            });
            Controls.InAppNotifier.Show("New message", $"From {sender}", Controls.SnackKind.Message, onTap: () => { /* open that conversation */ });
        }

        var latest = remoteMessages
            .OrderByDescending(m => ParseRemoteTimeOrNull(m.SentAt) ?? 0)
            .FirstOrDefault();

        if (latest is not null)
        {
            var existingConversation = await _repository.GetConversationByExtensionAsync(other);
            var latestTime = ParseRemoteTimeOrNull(latest.SentAt) ?? existingConversation?.LastMessageAt ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var isChatOpen = ActiveExtension == other;
            var unreadCount = isChatOpen
                ? 0
                : remoteMessages.Count(m => m.ReceiverExtension.Trim() == me && m.SenderExtension.Trim() == other
                                             && !string.Equals(m.Status, "read", StringComparison.OrdinalIgnoreCase));

            await _repository.SaveConversationAsync(new ConversationEntity
            {
                Extension = other,
                LastMessage = VisibleMessageBody(latest.Body),
                LastMessageAt = latestTime,
                UnreadCount = unreadCount
            });
        }
    }

    private static long? ParseRemoteTimeOrNull(string? value)
    {
        var clean = value?.Trim().Trim('"');
        if (string.IsNullOrEmpty(clean)) return null;
        return DateTimeOffset.TryParse(clean, out var dto) ? dto.ToUnixTimeMilliseconds() : null;
    }

    private async Task ReloadFromDbAsync(string conversationExtension)
    {
        var cleanExtension = conversationExtension.Trim();
        if (string.IsNullOrEmpty(cleanExtension)) return;

        var rows = await _repository.GetMessagesAsync(cleanExtension);
        var items = rows.Select(ToChatMessage).OrderBy(m => m.Time).ToList();

        Messages.Clear();
        foreach (var item in items) Messages.Add(item);
    }

    private async Task MarkVisibleIncomingMessagesReadAsync(string conversationExtension)
    {
        var me = _appState.Extension.Trim();
        var other = conversationExtension.Trim();
        if (string.IsNullOrEmpty(me) || string.IsNullOrEmpty(other)) return;

        var rows = await _repository.GetMessagesAsync(other);

        foreach (var message in rows.Where(m => !m.IsMine && m.Sender == other
                                                 && !string.Equals(m.Status, "read", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                await _api.MarkMessageReadAsync(new MessageStatusActionRequest { MessageId = message.MessageId, ActorExtension = me });
                await _repository.UpdateMessageStatusAsync(message.MessageId, MessageStatus.Read.ToString());
            }
            catch
            {
                // best-effort, mirrors original
            }
        }
    }

    private static ChatMessage ToChatMessage(ChatMessageEntity e) => new()
    {
        Id = e.MessageId,
        Extension = e.ConversationId,
        Body = e.Body,
        IsMine = e.IsMine,
        Time = e.SentAt,
        Status = Enum.TryParse<MessageStatus>(e.Status, out var s) ? s : MessageStatus.Sent,
        IsDeleted = e.IsDeleted,
        ReplyToId = e.ReplyToId,
        ReplyPreview = e.ReplyPreview,
        Reaction = e.Reaction
    };



}
