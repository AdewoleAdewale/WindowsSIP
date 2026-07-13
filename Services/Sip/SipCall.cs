using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of SipCall.kt. Original overrode pjsua2's Call.onCallState/onCallMediaState against
/// a native pjsua2 Call object; here a single SIPUserAgent + VoIPMediaSession per call plays
/// the same role (SIPSorcery creates one SIPUserAgent per call rather than one persistent
/// "Account" that spawns Call objects the way pjsua2 does).
/// </summary>
public class SipCall
{
    private readonly SIPUserAgent _userAgent;
    private readonly SIPServerUserAgent? _uas;
    private readonly SipCoreManager _manager;
    private readonly ISipEvents _events;
    private VoIPMediaSession? _mediaSession;

    public SipCall(SIPUserAgent userAgent, SipCoreManager manager, ISipEvents events, SIPServerUserAgent? uas = null)
    {
        _userAgent = userAgent;
        _uas = uas;
        _manager = manager;
        _events = events;

        _userAgent.ClientCallTrying += (uac, resp) => _events.OnCallStateChanged("Calling");
        _userAgent.ClientCallRinging += (uac, resp) => _events.OnCallStateChanged("Ringing");
        _userAgent.OnCallHungup += (dialogue) => _events.OnCallStateChanged("Ended");
        _userAgent.ClientCallAnswered += (uac, resp) =>
        {
            _events.OnCallStateChanged("Connected");
            _manager.ConnectAudio(this);
        };
        _userAgent.ClientCallFailed += (uac, error, resp) =>_events.OnCallStateChanged($"Call failed: {error}");
    }

    public SIPUserAgent UserAgent => _userAgent;

    public VoIPMediaSession? MediaSession
    {
        get => _mediaSession;
        set => _mediaSession = value;
    }

    public SIPDialogue? Dialogue => _userAgent.Dialogue;

    /// <summary>Best-effort mirror of pjsua2's CallInfo.remoteUri/remoteContact used for caller-id parsing.</summary>
    public (string RemoteUri, string RemoteContact) GetRemoteIdentity()
    {
        var dialogue = _userAgent.Dialogue;
        var remoteUri = dialogue?.RemoteTarget?.ToString() ?? "";
        var remoteContact = dialogue?.RemoteTarget?.ToString() ?? "";
        return (remoteUri, remoteContact);
    }
    public async Task<bool> MakeCallAsync(string destination, VoIPMediaSession mediaSession)
    {
        _mediaSession = mediaSession;
        return await _userAgent.Call(destination, null, null, mediaSession);
    }

    public async Task<bool> AnswerAsync(VoIPMediaSession mediaSession)
    {
        _mediaSession = mediaSession;
        if (_uas is null) return false;

        var answered = await _userAgent.Answer(_uas, mediaSession);
        if (answered)
        {
            _events.OnCallStateChanged("Connected");
            _manager.ConnectAudio(this);
        }
        return answered;
    }

    public void Hangup() => _userAgent.Hangup();

    public void Reject(SIPResponseStatusCodesEnum status, string reason) =>
        _uas?.Reject(status, reason, null);
  
    public bool PutOnHold()
    {
        _userAgent.PutOnHold();
        return true;
    }

    public bool TakeOffHold()
    {
        _userAgent.TakeOffHold();
        return true;
    }

    public void SendDtmf(char digit)
    {
        if (byte.TryParse(digit.ToString(), out var b))
        {
            _ = _userAgent.SendDtmf(b);
        }
    }

    public bool IsConnected => _userAgent.IsCallActive;

}
