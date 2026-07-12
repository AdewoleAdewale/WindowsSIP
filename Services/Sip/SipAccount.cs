using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of SipAccount.kt. The original wrapped pjsua2's Account class, which isn't
/// available on Windows the way it is via the Android NDK build.
///
/// NOTE: pjsua2's Account is a persistent object that both registers AND routes incoming
/// calls/messages to it. SIPSorcery doesn't have a matching single object, so
/// SipCoreManager.cs wires registration (SIPRegistrationUserAgent) and incoming-request
/// routing (SIPTransport.SIPTransportRequestReceived) directly rather than going through
/// this class. This file is kept as a clean, testable home for that same
/// registration/incoming-call/instant-message logic in case you'd rather have
/// SipCoreManager delegate to it -- just call these methods from the SIPTransport/
/// SIPRegistrationUserAgent event handlers in SipCoreManager instead of inlining them there.
/// </summary>
public class SipAccount
{
    private readonly SipCoreManager _manager;
    private readonly ISipEvents _events;

    public SipAccount(SipCoreManager manager, ISipEvents events)
    {
        _manager = manager;
        _events = events;
    }

    public void OnRegistrationSuccessful(SIPURI uri, SIPResponse response) =>
        _events.OnRegistered();

    public void OnRegistrationFailed(SIPURI uri, SIPResponse? response, string errorMessage) =>
        _events.OnRegistrationFailed(errorMessage);

    /// <summary>Wire this to SIPUserAgent.OnIncomingCall.</summary>
    public void OnIncomingCall(SIPUserAgent userAgent, SIPRequest incomingRequest)
    {
        var call = new SipCall(userAgent, _manager, _events);
        _manager.SetIncomingCall(call);

        var caller = ExtractExtension(incomingRequest.Header.From.FromURI.ToString());
        _events.OnIncomingCall(caller);
    }

    /// <summary>Wire this to SIPUserAgent.OnMessageReceived (SIP MESSAGE / instant message).</summary>
    public void OnInstantMessage(string fromUri, string body)
    {
        var from = ExtractExtension(fromUri);
        // Original Kotlin left this branch empty (extracted `from` but did nothing with it) --
        // preserved as-is; hook message persistence/UI update here if needed.
        _ = from;
    }

    private static string ExtractExtension(string uri)
    {
        try
        {
            var afterSip = uri.Contains("sip:") ? uri[(uri.IndexOf("sip:", StringComparison.Ordinal) + 4)..] : uri;
            var beforeAt = afterSip.Split('@')[0];
            var beforeSemicolon = beforeAt.Split(';')[0];
            return beforeSemicolon;
        }
        catch
        {
            return uri;
        }
    }
}
