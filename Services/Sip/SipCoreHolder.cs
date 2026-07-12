namespace SipCoreMobile.Services.Sip;

public static class SipCoreHolder
{
    public static SipCoreManager? Manager { get; set; }

    public static bool IsCallActive { get; set; }
    public static string ActiveNumber { get; set; } = "";
    public static string CallStatus { get; set; } = "";
}
