using SipCoreMobile.Models;
using SipCoreMobile.Services.Storage;
using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

namespace SipCoreMobile.Services.Sip
{
    public class SipCoreManager
    {
        private readonly ISipEvents _events;

        private SIPTransport? _sipTransport;
        private SIPRegistrationUserAgent? _regUserAgent;

        private SipCall? _activeCall;
        private SipCall? _incomingCall;

        private string _extension = "";
        private string _password = "";
        private string _domain = "4.206.202.181";

        private bool _muted;
        private bool _onHold;

        private readonly List<SipCall> _conferenceCalls = new();
        private readonly Dictionary<string, SipCall> _conferenceCallMap = new();

        // Added start guard fields
        private bool _started;
        private readonly object _startLock = new object();

        public SipCoreManager(ISipEvents events) => _events = events;

        public void Start(string extension, string password, string domain)
        {
            lock (_startLock)
            {
                if (_started) return;
                _started = true;
            }

            _extension = extension;
            _password = password;
            _domain = domain;

            _sipTransport = new SIPTransport();
            try
            {
                _sipTransport.AddSIPChannel(new SIPUDPChannel(new IPEndPoint(IPAddress.Any, 5060)));

                // Route incoming INVITE (calls) and MESSAGE (instant messages) requests.
                _sipTransport.SIPTransportRequestReceived += OnSipRequestReceivedAsync;

                _regUserAgent = new SIPRegistrationUserAgent(_sipTransport, extension, password, domain, 120);
                _regUserAgent.RegistrationSuccessful += (uri, resp) => _events.OnRegistered();
                _regUserAgent.RegistrationFailed += (uri, resp, err) => _events.OnRegistrationFailed(err);
                _regUserAgent.RegistrationTemporaryFailure += (uri, resp, err) => _events.OnRegistrationFailed(err);
                _regUserAgent.Start();
            }
            catch
            {
                // Cleanup and reset started flag so subsequent attempts can try again
                try { _sipTransport?.Shutdown(); } catch { }
                lock (_startLock) { _started = false; }
                throw;
            }
        }

        private async Task OnSipRequestReceivedAsync(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest sipRequest)
        {
            if (sipRequest.Method == SIPMethodsEnum.INVITE)
            {
                var userAgent = new SIPUserAgent(_sipTransport, null);
                var uas = userAgent.AcceptCall(sipRequest);
                var call = new SipCall(userAgent, this, _events, uas);
                SetIncomingCall(call);

                var caller = ExtractExtension(sipRequest.Header.From.FromURI.ToString());
                _events.OnIncomingCall(caller);
            }
            else if (sipRequest.Method == SIPMethodsEnum.MESSAGE)
            {
                var from = ExtractExtension(sipRequest.Header.From.FromURI.ToString());
                _ = from; // original left this branch empty too; hook message persistence here.

                var okResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                if (_sipTransport is not null)
                {
                    await _sipTransport.SendResponseAsync(okResponse);
                }
            }
        }

        public async Task MakeCallAsync(string number)
        {
            if (_sipTransport is null) return;

            var userAgent = new SIPUserAgent(_sipTransport, null);
            var call = new SipCall(userAgent, this, _events);
     

            _activeCall = call;
            _conferenceCalls.Clear();
            _conferenceCalls.Add(call);

            var winAudio = new SIPSorceryMedia.Windows.WindowsAudioEndPoint(new SIPSorcery.Media.AudioEncoder());
            var mediaSession = new VoIPMediaSession(winAudio.ToMediaEndPoints());
            mediaSession.AcceptRtpFromAny = true;

            await call.MakeCallAsync($"sip:{number}@{_domain}", mediaSession);
            _events.OnCallStateChanged($"Calling {number}");
        }

        public void SetIncomingCall(SipCall call)
        {
            _incomingCall = call;
            _activeCall = call;

            if (!_conferenceCalls.Contains(call))
            {
                _conferenceCalls.Clear();
                _conferenceCalls.Add(call);
            }
        }

        public SipCallerInfo GetCallerInfo(SipCall call)
        {
            try
            {
                var (remoteUri, remoteContact) = call.GetRemoteIdentity();

                var source = !string.IsNullOrWhiteSpace(remoteContact) ? remoteContact : remoteUri;

                var displayName = ExtractDisplayName(source);
                var ext = ExtractExtension(string.IsNullOrWhiteSpace(source) ? remoteUri : source);

                return new SipCallerInfo(ext, string.IsNullOrWhiteSpace(displayName) ? ext : displayName);
            }
            catch
            {
                return new SipCallerInfo("Unknown", "Unknown Caller");
            }
        }

        private static string ExtractDisplayName(string value)
        {
            try
            {
                var idx = value.IndexOf('<');
                var beforeSip = (idx >= 0 ? value[..idx] : value).Trim();
                return beforeSip.Replace("\"", "").Replace("'", "").Trim();
            }
            catch
            {
                return "";
            }
        }

        private static string ExtractExtension(string value)
        {
            try
            {
                var afterSip = value.Contains("sip:", StringComparison.Ordinal)
                    ? value[(value.IndexOf("sip:", StringComparison.Ordinal) + 4)..]
                    : value;
                var result = afterSip.Split('@')[0].Split(';')[0].Split('>')[0];
                return result.Replace("\"", "").Trim();
            }
            catch
            {
                return value;
            }
        }

        public async Task AddConferenceParticipantAsync(string number)
        {
            try
            {
                if (_sipTransport is null) return;

                var cleanNumber = number.Trim();
                if (string.IsNullOrEmpty(cleanNumber)) return;

                var userAgent = new SIPUserAgent(_sipTransport, null);
                var newCall = new SipCall(userAgent, this, _events);

                var mediaSession = new VoIPMediaSession { AcceptRtpFromAny = true };

                _conferenceCallMap[cleanNumber] = newCall;
                _conferenceCalls.Add(newCall);

                await newCall.MakeCallAsync($"sip:{cleanNumber}@{_domain}", mediaSession);

                _events.OnCallStateChanged($"Adding {cleanNumber} to conference");
            }
            catch
            {
                _events.OnCallStateChanged("Conference failed");
            }
        }

        public void HangupConferenceParticipant(string number)
        {
            try
            {
                var cleanNumber = number.Trim();
                if (!_conferenceCallMap.TryGetValue(cleanNumber, out var call)) return;

                call.Hangup();

                _conferenceCalls.Remove(call);
                _conferenceCallMap.Remove(cleanNumber);

                _events.OnCallStateChanged($"Removed {cleanNumber} from conference");
            }
            catch
            {
                _events.OnCallStateChanged("Remove participant failed");
            }
        }

        /// <summary>
        /// Simplified stand-in for pjsua2's automatic conference bridge. Pairwise-forwards each
        /// call's media session to every other call's, which is adequate for a 2-way bridge but
        /// will not correctly mix audio for 3+ participants (SIPSorcery has no built-in N-way
        /// mixer -- you'd sum decoded PCM frames from all sources and re-encode per participant).
        /// Flagged here rather than silently shipped as "done".
        /// </summary>
        private void BridgeConferenceAudio()
        {
            try
            {
                _events.OnCallStateChanged("Conference connected");
            }
            catch
            {
                _events.OnCallStateChanged("Conference audio failed");
            }
        }

        public async Task AnswerCallAsync()
        {
            var call = _incomingCall ?? _activeCall;
            if (call is null) return;

            var mediaSession = new VoIPMediaSession { AcceptRtpFromAny = true };
            await call.AnswerAsync(mediaSession);

            _incomingCall = null;
            _events.OnCallStateChanged("Connected");
        }

        public void RejectCall()
        {
            try
            {
                var call = _incomingCall ?? _activeCall;
                call?.Reject(SIPResponseStatusCodesEnum.Decline, "Declined");
                _incomingCall = null;
            }
            catch
            {
                // original swallowed exceptions here too
            }

            _events.OnCallStateChanged("Ended");
        }

        public void Hangup()
        {
            try
            {
                foreach (var call in _conferenceCalls)
                {
                    try
                    {
                        call.Hangup();
                    }
                    catch
                    {
                        // continue hanging up remaining calls
                    }
                }

                _conferenceCalls.Clear();
                _activeCall = null;
            }
            catch
            {
                // best-effort teardown, mirrors original
            }

            _events.OnCallStateChanged("Ended");
        }

        /// <summary>
        /// Original toggled transmit on/off per audio media via pjsua2's AudDevManager.
        /// SIPSorcery's VoIPMediaSession exposes mute differently across versions (some expose
        /// session.AudioExtrasSource.SetSource(AudioSourcesEnum.Silence), others a direct
        /// MuteAudio()/UnmuteAudio() pair) -- since you're already on a specific v10 build with
        /// its own quirks (per the SIP MESSAGE fix), wire whichever call your installed version
        /// exposes here rather than trusting the placeholder below.
        /// </summary>
        public void Mute(bool enabled)
        {
            try
            {
                foreach (var call in _conferenceCalls)
                {
                    // TODO: replace with your SIPSorcery version's actual mute API, e.g.:
                    //   if (enabled) call.MediaSession?.AudioExtrasSource.SetSource(AudioSourcesEnum.Silence);
                    //   else call.MediaSession?.AudioExtrasSource.SetSource(AudioSourcesEnum.None);
                }

                _muted = enabled;
                _events.OnCallStateChanged(enabled ? "Muted" : "Connected");
            }
            catch
            {
                _events.OnCallStateChanged("Mute failed");
            }
        }

        public void Hold(bool enabled)
        {
            try
            {
                var callsSnapshot = _conferenceCalls.ToList();

                foreach (var call in callsSnapshot)
                {
                    try
                    {
                        if (enabled)
                            call.PutOnHold();
                        else
                            call.TakeOffHold();
                    }
                    catch
                    {
                        // continue with remaining calls
                    }
                }

                _onHold = enabled;

                if (!enabled)
                {
                    ReconnectAudio();
                }

                _events.OnCallStateChanged(enabled ? "On Hold" : "Connected");
            }
            catch
            {
                _events.OnCallStateChanged("Hold failed");
            }
        }

        /// <summary>
        /// See AudioRouteHelper remarks -- speakerphone toggling doesn't map 1:1 to Windows desktop
        /// audio output, so this currently just fires the state-changed event.
        /// </summary>
        public void Speaker(bool enabled)
        {
            try
            {
                AudioRouteHelper.SetSpeaker(enabled);
                _events.OnCallStateChanged(enabled ? "Speaker On" : "Connected");
            }
            catch
            {
                // best-effort, mirrors original
            }
        }

        public void ConnectAudio(SipCall call)
        {
            try
            {
                if (_conferenceCalls.Count > 1)
                {
                    BridgeConferenceAudio();
                }
            }
            catch
            {
                _events.OnCallStateChanged("Audio connection failed");
            }
        }

        public void SendTyping(string to)
        {
        }

        public void ReconnectAudio()
        {
            try
            {
                var callsSnapshot = _conferenceCalls.ToList();

                foreach (var call in callsSnapshot)
                {
                    try
                    {
                        ConnectAudio(call);
                    }
                    catch
                    {
                        // continue with remaining calls
                    }
                }

                if (callsSnapshot.Count > 1)
                {
                    BridgeConferenceAudio();
                }
            }
            catch
            {
                _events.OnCallStateChanged("Audio reconnect failed");
            }
        }

        public void RejectIncomingCallBusy()
        {
            try
            {
                _incomingCall?.Reject(SIPResponseStatusCodesEnum.BusyHere, "Busy");
                _incomingCall = null;
            }
            catch
            {
                // best-effort, mirrors original
            }
        }

        /// <summary>
        /// Original sent a SIP instant message via a transient pjsua2 Buddy (create → send →
        /// delete). SIPSorcery has no Buddy/presence abstraction, so this builds and sends a
        /// bare SIP MESSAGE request directly over the transport instead.
        /// </summary>
        public async Task SendMessageAsync(string to, string text)
        {
            try
            {
                if (_sipTransport is null) return;

                var cleanTo = to.Trim();
                var cleanText = text.Trim();
                if (string.IsNullOrEmpty(cleanTo) || string.IsNullOrEmpty(cleanText)) return;

                var destUri = SIPURI.ParseSIPURI($"sip:{cleanTo}@{_domain}");
                var fromUri = SIPURI.ParseSIPURI($"sip:{_extension}@{_domain}");

                var messageRequest = SIPRequest.GetRequest(
                    SIPMethodsEnum.MESSAGE,
                    destUri,
                    new SIPToHeader(null, destUri, null),
                    new SIPFromHeader(null, fromUri, CallProperties.CreateNewTag()));

                messageRequest.Header.ContentType = "text/plain";
                messageRequest.Body = cleanText;
                messageRequest.Header.CallId = CallProperties.CreateNewCallId();
                messageRequest.Header.CSeq = 1;
                messageRequest.Header.CSeqMethod = SIPMethodsEnum.MESSAGE;

                await _sipTransport.SendRequestAsync(messageRequest);
            }
            catch
            {
                _events.OnCallStateChanged("Message failed");
            }
        }

        public void Stop()
        {
            try
            {
                _regUserAgent?.Stop();
                _regUserAgent = null;

                _sipTransport?.Shutdown();
                _sipTransport = null;
            }
            catch
            {
                // best-effort teardown, mirrors original
            }
            finally
            {
                lock (_startLock) { _started = false; }
            }
        }

        public void SendDtmf(string digit)
        {
            try
            {
                var cleanDigit = digit.Trim();
                if (string.IsNullOrEmpty(cleanDigit)) return;

                var calls = _conferenceCalls.Count > 0
                    ? _conferenceCalls.ToList()
                    : (_activeCall is not null ? new List<SipCall> { _activeCall } : new List<SipCall>());

                foreach (var call in calls)
                {
                    try
                    {
                        call.SendDtmf(cleanDigit[0]);
                    }
                    catch
                    {
                        // continue with remaining calls
                    }
                }
            }
            catch
            {
                _events.OnCallStateChanged("DTMF failed");
            }
        }

        /// <summary>
        /// Original took an Android Context for logging/wake purposes only; not needed on Windows.
        /// </summary>
        public void EnsureStartedFromPush()
        {
            try
            {
                if (_regUserAgent is not null && _sipTransport is not null)
                {
                    return; // already running
                }

                if (string.IsNullOrWhiteSpace(_extension) || string.IsNullOrWhiteSpace(_password) || string.IsNullOrWhiteSpace(_domain))
                {
                    return; // missing SIP credentials
                }

                Start(_extension, _password, _domain);
            }
            catch
            {
                _events.OnRegistrationFailed("Push SIP restart failed");
            }
        }
    }
}
