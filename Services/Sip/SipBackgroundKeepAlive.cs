using Windows.System.Display;

namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of SipForegroundService.kt. Most of the original doesn't apply on Windows:
/// - No foreground-service/notification ceremony needed (Android requires a persistent
///   notification to keep a background service alive; a Windows app just keeps running
///   while its process/window is open).
/// - No PowerManager.WakeLock concept; the closest equivalent is
///   Windows.System.Display.DisplayRequest, which just prevents the display from turning
///   off/locking (useful during an active call so the screen doesn't sleep mid-call) --
///   it does NOT prevent the OS from suspending the app the way a wake lock does, because
///   that's not a concern for a running desktop app the way it is for backgrounded Android.
///
/// Call RequestKeepAwake() when a call starts and ReleaseKeepAwake() when it ends (mirrors
/// the original's acquireWakeLock/releaseWakeLock, called from onCreate/onStartCommand/onDestroy).
/// </summary>
public class SipBackgroundKeepAlive
{
    private DisplayRequest? _displayRequest;

    public void RequestKeepAwake()
    {
        try
        {
            if (_displayRequest is not null) return;

            _displayRequest = new DisplayRequest();
            _displayRequest.RequestActive();
        }
        catch
        {
            // best-effort, mirrors original
        }
    }

    public void ReleaseKeepAwake()
    {
        try
        {
            if (_displayRequest is null) return;

            _displayRequest.RequestRelease();
            _displayRequest = null;
        }
        catch
        {
            // best-effort, mirrors original
        }
    }
}
