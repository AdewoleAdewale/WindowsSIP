namespace SipCoreMobile.Services.Storage;

/// <summary>
/// Original used Android SharedPreferences for the FCM push token.
/// Windows has no FCM; if push is needed on Windows it'll go through WNS instead,
/// but the key/value storage pattern is preserved via Microsoft.Maui.Storage.Preferences
/// in case the token store is still used for a WNS channel URI or similar.
/// </summary>
public static class SipCoreTokenStore
{
    private const string Pref = "sipcore_token_store";
    private const string TokenKey = "fcm_token";

    public static void SaveToken(string token)
    {
        Preferences.Default.Set(TokenKey, token, Pref);
    }

    public static string GetToken()
    {
        return Preferences.Default.Get(TokenKey, string.Empty, Pref);
    }
}
