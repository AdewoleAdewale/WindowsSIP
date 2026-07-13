using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System.Media;


namespace SipCoreMobile.Services.Notifications;


public static class NotificationHelper
{
    public const string IncomingCallTag = "incoming-call";
    public const string ForegroundTag = "foreground";

    private static SoundPlayer? _ringtonePlayer;

    private static string _lastIncomingCaller = "";
    private static long _lastIncomingCallTime;

    private static readonly Dictionary<string, long> RecentMessageNotifications = new();

    private const long CallNotificationCooldownMs = 7000;
    private const long MessageNotificationCooldownMs = 5000;
    private const long MessageNotificationCacheTtlMs = 60_000;

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string MessageTag(string from) => $"message-{PushTextUtils.CleanExtension(from)}";

    /// <summary>No-op on Windows -- see class remarks.</summary>
    public static void CreateChannels()
    {
    }

    public static void CancelIncomingCall()
    {
        AppNotificationManager.Default.RemoveByTagAsync(IncomingCallTag);
        _lastIncomingCaller = "";
        _lastIncomingCallTime = 0;
        StopRingtone();
    }

    public static void CancelMessage(string extension)
    {
        AppNotificationManager.Default.RemoveByTagAsync(MessageTag(extension));
    }

    public static void ShowMessage(string from, string body)
    {
        var cleanFrom = PushTextUtils.CleanExtension(from);
        var tag = MessageTag(cleanFrom);

        var key = $"{cleanFrom}|{body.Trim()}";
        var now = NowMs;

        foreach (var staleKey in RecentMessageNotifications
                     .Where(kv => now - kv.Value > MessageNotificationCacheTtlMs)
                     .Select(kv => kv.Key).ToList())
        {
            RecentMessageNotifications.Remove(staleKey);
        }

        var lastShown = RecentMessageNotifications.GetValueOrDefault(key, 0);
        if (now - lastShown < MessageNotificationCooldownMs) return;

        RecentMessageNotifications[key] = now;

        var builder = new AppNotificationBuilder()
            .AddArgument("action", "openChat")
            .AddArgument("conversationExtension", cleanFrom)
            .SetTag(tag)
            .SetGroup("sipcore_messages")
            .AddText($"Message from {from}")
            .AddText(body);

        AppNotificationManager.Default.Show(builder.BuildNotification());

        PlayMessageSound();
    }

    public static void ShowIncomingCall(string caller)
    {
        var cleanCaller = PushTextUtils.CleanExtension(caller);
        var now = NowMs;

        if (cleanCaller == _lastIncomingCaller && now - _lastIncomingCallTime < CallNotificationCooldownMs)
            return;

        _lastIncomingCaller = cleanCaller;
        _lastIncomingCallTime = now;

        var builder = new AppNotificationBuilder()
            .AddArgument("action", "incomingCall")
            .AddArgument("caller", caller)
            .SetTag(IncomingCallTag)
            .AddText("Incoming call")
            .AddText(caller);

        AppNotificationManager.Default.Show(builder.BuildNotification());

        StartRingtone();
    }

    private static void PlayMessageSound()
    {
        try
        {
            // Expects a "message.wav" asset; original used R.raw.message.
            using var player = new SoundPlayer("Assets/Sounds/message.wav");
            player.Play();
        }
        catch
        {
            // best-effort, mirrors original
        }
    }

    public static void StartRingtone()
    {
        try
        {
            StopRingtone();

            // Expects a "ringtone.wav" asset; original used R.raw.ringtone. If you already
            // have the RingtoneService built during the earlier Windows migration work
            // (WinRT/SoundPlayer, looped), prefer calling that instead of this local player.
            _ringtonePlayer = new SoundPlayer("Assets/Sounds/ringtone.wav");
            _ringtonePlayer.Load();
            _ringtonePlayer.PlayLooping();
        }
        catch
        {
            // best-effort, mirrors original
        }
    }

    public static void StopRingtone()
    {
        try
        {
            _ringtonePlayer?.Stop();
            _ringtonePlayer?.Dispose();
            _ringtonePlayer = null;
        }
        catch
        {
            // best-effort, mirrors original
        }
    }

    public static void ShowMeeting(string title, string body, string meetingId, string companyId, string conferenceNumber)
    {
        var tag = !string.IsNullOrWhiteSpace(meetingId) ? $"meeting-{meetingId}" : $"meeting-{NowMs}";

        var builder = new AppNotificationBuilder()
            .AddArgument("action", "openMeeting")
            .AddArgument("meetingId", meetingId)
            .AddArgument("companyId", companyId)
            .AddArgument("conferenceNumber", conferenceNumber)
            .SetTag(tag)
            .SetGroup("sipcore_meetings")
            .AddText(string.IsNullOrWhiteSpace(title) ? "SIPCore Meeting" : title)
            .AddText(string.IsNullOrWhiteSpace(body) ? "Meeting invitation" : body);

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }

    public static void ShowWorkDeskTask(string title, string body, string taskId, string companyId)
    {
        var tag = !string.IsNullOrWhiteSpace(taskId) ? $"workdesk-{taskId}" : $"workdesk-{NowMs}";

        var builder = new AppNotificationBuilder()
            .AddArgument("action", "openWorkDeskTask")
            .AddArgument("taskId", taskId)
            .AddArgument("companyId", companyId)
            .SetTag(tag)
            .SetGroup("sipcore_workdesk")
            .AddText(string.IsNullOrWhiteSpace(title) ? "WorkDesk" : title)
            .AddText(string.IsNullOrWhiteSpace(body) ? "Task updated" : body);

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }
}
