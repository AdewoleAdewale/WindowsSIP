using System.Media;

namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of RingbackPlayer.kt (the tone the caller hears while a call is ringing out).
/// Distinct from the incoming ringtone (see NotificationHelper.StartRingtone), which per
/// your existing SIPCOREMOBILE Windows work already loops a WAV via a dedicated
/// RingtoneService using WinRT/SoundPlayer -- this follows the same SoundPlayer-looping
/// pattern for consistency. If you already have a shared "loop this WAV" helper from that
/// RingtoneService work, prefer reusing it here instead of a second implementation.
/// Expects a "ringback.wav" resource; original used R.raw.ringback.
/// </summary>
public static class RingbackPlayer
{
    private static SoundPlayer? _player;

    public static void Start(string wavFilePath)
    {
        try
        {
            Stop();

            _player = new SoundPlayer(wavFilePath);
            _player.Load();
            _player.PlayLooping();
        }
        catch
        {
            // best-effort, mirrors original's swallowed exception
        }
    }

    public static void Stop()
    {
        try
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
        }
        catch
        {
            // best-effort, mirrors original
        }
    }
}
