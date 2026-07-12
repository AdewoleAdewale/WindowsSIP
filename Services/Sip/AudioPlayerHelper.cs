using Windows.Media.Core;
using Windows.Media.Playback;

namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of AudioPlayerHelper.kt. Original used Android's MediaPlayer (arbitrary format,
/// async prepare, completion callback). Windows.Media.Playback.MediaPlayer (WinRT) is the
/// closest match -- it also handles multiple formats (m4a/aac voice notes, mp3 attachments,
/// etc.) and exposes a MediaEnded event equivalent to setOnCompletionListener.
/// </summary>
public static class AudioPlayerHelper
{
    private static MediaPlayer? _player;

    public static void Play(string path, Action? onComplete = null)
    {
        Stop();

        try
        {
            _player = new MediaPlayer
            {
                Source = MediaSource.CreateFromUri(new Uri(path))
            };

            _player.MediaEnded += (sender, args) =>
            {
                Stop();
                onComplete?.Invoke();
            };

            _player.MediaFailed += (sender, args) =>
            {
                Stop();
                onComplete?.Invoke();
            };

            _player.Play();
        }
        catch
        {
            onComplete?.Invoke();
        }
    }

    public static void Stop()
    {
        try
        {
            _player?.Pause();
            _player?.Dispose();
        }
        catch
        {
            // best-effort, mirrors original
        }

        _player = null;
    }
}
