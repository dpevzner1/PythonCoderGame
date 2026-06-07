using System.Reflection;
using NAudio.Wave;

namespace PythonCoderGame;

internal sealed class AudioSystem : IDisposable
{
    private readonly string _cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PythonCoderGame",
        "music");

    private readonly string[] _dashboardTracks = ["Skynet.mp3"];
    private readonly string[] _authTracks = ["Dread.mp3"];
    private readonly string[] _gameTracks = ["Battlefield.mp3", "Battlefield2.mp3", "Virus1.mp3", "MEgaVirus.mp3", "Boss.mp3"];
    private readonly string[] _playlist = ["Battlefield.mp3", "Battlefield2.mp3", "Boss.mp3", "CyborgHunter.mp3", "Dread.mp3", "MEgaVirus.mp3", "Skynet.mp3", "Virus1.mp3"];

    private int _playlistIndex;
    private string _currentTrack = "";
    private WaveOutEvent? _musicOut;
    private AudioFileReader? _musicReader;
    private bool _stopping;

    public bool Enabled { get; private set; } = true;

    public string CurrentTrack => string.IsNullOrWhiteSpace(_currentTrack) ? "music off" : _currentTrack;

    public void PlayForScreen(AppScreen screen, int lessonIndex = 0)
    {
        if (!Enabled)
        {
            return;
        }

        var track = screen switch
        {
            AppScreen.Boot => "CyborgHunter.mp3",
            AppScreen.Auth => _authTracks[0],
            AppScreen.Game => _gameTracks[Math.Abs(lessonIndex) % _gameTracks.Length],
            _ => _dashboardTracks[0]
        };

        Play(track);
    }

    public void Toggle(AppScreen screen, int lessonIndex)
    {
        Enabled = !Enabled;
        if (Enabled)
        {
            PlayForScreen(screen, lessonIndex);
        }
        else
        {
            Stop();
        }
    }

    public void Next()
    {
        if (!Enabled)
        {
            Enabled = true;
        }

        _playlistIndex = (_playlistIndex + 1) % _playlist.Length;
        Play(_playlist[_playlistIndex]);
    }

    public void Success()
    {
        _ = Task.Run(() =>
        {
            SafeBeep(660, 45);
            SafeBeep(880, 55);
            SafeBeep(1175, 65);
        });
    }

    public void Error()
    {
        _ = Task.Run(() =>
        {
            SafeBeep(180, 80);
            SafeBeep(120, 90);
        });
    }

    public void Complete()
    {
        _ = Task.Run(() =>
        {
            SafeBeep(523, 70);
            SafeBeep(659, 70);
            SafeBeep(784, 90);
            SafeBeep(1047, 140);
        });
    }

    public void KeyTick()
    {
        _ = Task.Run(() => SafeBeep(920, 18));
    }

    private void Play(string trackName)
    {
        try
        {
            var path = EnsureTrack(trackName);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Stop();
            _musicReader = new AudioFileReader(path) { Volume = 0.72f };
            _musicOut = new WaveOutEvent { DesiredLatency = 140 };
            _musicOut.Init(_musicReader);
            _musicOut.PlaybackStopped += (_, _) =>
            {
                if (!_stopping && Enabled && _musicReader is not null && _currentTrack == trackName)
                {
                    try
                    {
                        _musicReader.Position = 0;
                        _musicOut?.Play();
                    }
                    catch
                    {
                        _currentTrack = "";
                    }
                }
            };
            _musicOut.Play();
            _currentTrack = trackName;
            var idx = Array.IndexOf(_playlist, trackName);
            if (idx >= 0)
            {
                _playlistIndex = idx;
            }
        }
        catch
        {
            _currentTrack = "";
        }
    }

    private string EnsureTrack(string trackName)
    {
        var localPath = Path.Combine(AppContext.BaseDirectory, "Music Resources", trackName);
        if (File.Exists(localPath))
        {
            return localPath;
        }

        var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Music", trackName);
        if (File.Exists(assetPath))
        {
            return assetPath;
        }

        Directory.CreateDirectory(_cacheDir);
        var path = Path.Combine(_cacheDir, trackName);
        var resourceName = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith($".{trackName}", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return "";
        }

        using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (source is null)
        {
            return "";
        }

        if (File.Exists(path) && new FileInfo(path).Length == source.Length)
        {
            return path;
        }

        using var target = File.Create(path);
        source.CopyTo(target);
        return path;
    }

    private void Stop()
    {
        try
        {
            _stopping = true;
            _musicOut?.Stop();
        }
        catch
        {
        }
        _musicOut?.Dispose();
        _musicReader?.Dispose();
        _musicOut = null;
        _musicReader = null;
        _stopping = false;
    }

    private static void SafeBeep(int frequency, int duration)
    {
        try
        {
            Console.Beep(frequency, duration);
        }
        catch
        {
            System.Media.SystemSounds.Beep.Play();
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
