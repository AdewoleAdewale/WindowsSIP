using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace SipCoreMobile.Services.Sip;

/// <summary>
/// Port of VoiceRecorder.kt. Original used Android's MediaRecorder (mic → AAC in an MPEG-4
/// container). Windows.Media.Capture.MediaCapture with MediaEncodingProfile.CreateM4a is the
/// direct WinRT equivalent -- same source (microphone), same output container/codec.
/// Requires the "microphone" capability in Package.appxmanifest (already added per the
/// SIPCOREMOBILE Windows migration work).
/// </summary>
public class VoiceRecorder
{
    private MediaCapture? _mediaCapture;
    private StorageFile? _outputFile;

    public async Task<StorageFile> StartAsync()
    {
        var fileName = $"voice_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.m4a";
        var cacheFolder = ApplicationData.Current.TemporaryFolder;
        _outputFile = await cacheFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

        _mediaCapture = new MediaCapture();
        await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            StreamingCaptureMode = StreamingCaptureMode.Audio
        });

        var profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Medium);
        await _mediaCapture.StartRecordToStorageFileAsync(profile, _outputFile);

        return _outputFile;
    }

    public async Task<StorageFile?> StopAsync()
    {
        try
        {
            if (_mediaCapture is not null)
            {
                await _mediaCapture.StopRecordAsync();
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }

            return _outputFile;
        }
        catch
        {
            return null;
        }
    }

    public async void Cancel()
    {
        try
        {
            if (_mediaCapture is not null)
            {
                await _mediaCapture.StopRecordAsync();
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }

            if (_outputFile is not null)
            {
                await _outputFile.DeleteAsync();
            }
        }
        catch
        {
            // best-effort, mirrors original
        }
        finally
        {
            _outputFile = null;
        }
    }
}
