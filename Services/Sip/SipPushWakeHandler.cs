namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of SipPushWakeHandler.kt. Original started a foreground service (to survive Android's
/// background execution limits) then asked the SIP manager to re-register. A Windows desktop
/// app has no equivalent background-execution limit while it's running, so this is much
/// simpler: just ensure the SIP stack is registered. Call this from wherever your Windows
/// push transport lands (see PushMessageDispatcher remarks) or on app resume/reconnect.
/// </summary>
public static class SipPushWakeHandler
{
    public static void WakeAndRegister()
    {
        try
        {
            SipCoreHolder.Manager?.EnsureStartedFromPush();
        }
        catch
        {
            // best-effort, mirrors original
        }
    }
}
