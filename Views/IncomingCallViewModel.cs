using SipCoreMobile.Models;
using SipCoreMobile.Services.Notifications;
using SipCoreMobile.Services.Sip;

namespace SipCoreMobile.Views;

/// <summary>
/// Port of IncomingCallActivity.kt's answer/reject logic. Original was a whole separate
/// Android Activity (shown full-screen, often over the lock screen) hosting a Compose
/// IncomingCallScreen. MAUI/Windows has no direct "Activity" equivalent; the natural mapping
/// is either:
///   (a) a Shell-routed IncomingCallPage within the main window, or
///   (b) a separate always-on-top MAUI Window (new Window(new IncomingCallPage())) if you
///       want it to visually interrupt like the original full-screen Activity did.
/// This class holds just the answer/reject logic (the part that doesn't depend on which
/// hosting approach you pick) so it's ready once the corresponding XAML page is built in the
/// main-UI batch. Bind IncomingCallPage's Answer/Reject buttons to OnAnswerAsync/OnReject.
/// </summary>
public class IncomingCallViewModel
{
    public string Caller { get; }

    /// <summary>Raised when answer/reject completes so the host page/window can close/navigate away.</summary>
    public event Action<bool>? Completed; // bool = wasAnswered

    public IncomingCallViewModel(string caller)
    {
        Caller = string.IsNullOrEmpty(caller) ? "Unknown" : caller;
    }

    public async Task OnAnswerAsync()
    {
        NotificationHelper.StopRingtone();
        NotificationHelper.CancelIncomingCall();

        SipCoreHolder.IsCallActive = true;
        SipCoreHolder.ActiveNumber = Caller;
        SipCoreHolder.CallStatus = "Connecting...";

        if (SipCoreHolder.Manager is not null)
        {
            await SipCoreHolder.Manager.AnswerCallAsync();
        }

        // Original navigated to MainActivity with openActiveCall=true + caller extras and
        // finished this Activity. With Shell navigation that becomes something like:
        //   await Shell.Current.GoToAsync($"//ActiveCall?caller={Uri.EscapeDataString(Caller)}");
        // Left as an event instead of a hard Shell dependency so this class stays testable
        // independent of the routing table, which isn't defined until the main UI batch.
        Completed?.Invoke(true);
    }

    public void OnReject()
    {
        NotificationHelper.StopRingtone();
        NotificationHelper.CancelIncomingCall();

        SipCoreHolder.IsCallActive = false;
        SipCoreHolder.ActiveNumber = "";
        SipCoreHolder.CallStatus = "";

        SipCoreHolder.Manager?.RejectCall();

        // Original navigated to MainActivity with callRejected=true + caller extras.
        // Same note as above re: Shell.Current.GoToAsync once routes exist.
        Completed?.Invoke(false);
    }
}
