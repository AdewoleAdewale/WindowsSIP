namespace SipCoreMobile.Services.Sip;

public interface ISipEvents
{
    void OnRegistered();
    void OnRegistrationFailed(string reason);
    void OnIncomingCall(string caller);
    void OnCallStateChanged(string state);
}
