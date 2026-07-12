namespace SipCoreMobile.Services.Storage;

/// <summary>
/// Original used Android EncryptedSharedPreferences (AES256-GCM/SIV via a MasterKey).
/// On Windows, Microsoft.Maui.Storage.SecureStorage wraps the Windows Credential
/// Locker, giving the same "encrypted at rest, per-app" guarantee without needing
/// to manage a master key ourselves.
/// </summary>
public static class SecureCredentialStore
{
    private const string KeyPassword = "sip_password";

    public static async Task SavePasswordAsync(string password)
    {
        await SecureStorage.Default.SetAsync(KeyPassword, password);
    }

    public static async Task<string?> GetPasswordAsync()
    {
        return await SecureStorage.Default.GetAsync(KeyPassword);
    }

    public static void Clear()
    {
        SecureStorage.Default.RemoveAll();
    }
}
