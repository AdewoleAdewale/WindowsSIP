namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of AudioRouteHelper.kt. Original toggled Android's AudioManager between earpiece
/// and speakerphone output -- a distinction that exists because phones have both a quiet
/// earpiece speaker and a loud speakerphone. Windows desktops/laptops don't have that
/// earpiece/speakerphone duality; there's just "the current default audio output device"
/// (built-in speakers, headphones, a USB headset, etc.), so a literal port doesn't make
/// sense here.
///
/// If you want an equivalent "speaker button" on Windows, the real feature is switching the
/// default communications output device -- e.g. between a headset and the laptop's built-in
/// speakers. That requires either:
///   - NAudio's CoreAudioApi (MMDeviceEnumerator) to enumerate render endpoints, combined
///     with the (undocumented but widely used) IPolicyConfig COM interface to actually change
///     the OS default communications device, or
///   - Microsoft.Windows.Devices / Windows.Media.Devices APIs if you only need per-session
///     endpoint selection rather than changing the system default.
/// Left as a documented no-op below rather than a guessed implementation, since this needs
/// a product decision (do you want a device picker instead of a binary toggle?) before it's
/// worth writing.
/// </summary>
public static class AudioRouteHelper
{
    public static void SetSpeaker(bool enabled)
    {
        // No-op on Windows -- see class remarks. Wire up an output-device picker here if needed.
    }
}
