namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Original Kotlin version held an Android application Context for singleton
/// classes (SharedPreferences, etc.) that needed it outside the Activity lifecycle.
/// On Windows-only MAUI this generally isn't needed -- Preferences.Default and
/// SecureStorage.Default don't require a Context -- but kept as a lightweight
/// holder in case a service needs a reference to IServiceProvider / MauiContext.
/// </summary>
public static class SipCoreAppContext
{
    public static IServiceProvider? Services { get; set; }
}
