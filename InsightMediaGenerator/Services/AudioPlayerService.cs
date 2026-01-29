using System.IO;
using NAudio.Wave;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Services;

public class AudioPlayerService : IAudioPlayerService
{
    private WaveOutEvent? _waveOut;
    private WaveStream? _audioStream;
    private MemoryStream? _audioMemoryStream;
    private bool _disposed;

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    public TimeSpan Position
    {
        get => _audioStream?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_audioStream != null)
                _audioStream.CurrentTime = value;
        }
    }

    public TimeSpan Duration => _audioStream?.TotalTime ?? TimeSpan.Zero;

    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;

    public void LoadAudio(byte[] audioData)
    {
        Stop();
        DisposeAudio();

        _audioMemoryStream = new MemoryStream(audioData);
        _audioStream = new WaveFileReader(_audioMemoryStream);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_audioStream);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { IsPlaying = false });
    }

    public void Play()
    {
        if (_audioStream != null && _waveOut != null)
        {
            if (_audioStream.Position >= _audioStream.Length)
            {
                _audioStream.Position = 0;
            }
            _waveOut.Play();
            PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { IsPlaying = true });
        }
    }

    public void Pause()
    {
        _waveOut?.Pause();
        PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { IsPlaying = false });
    }

    public void Stop()
    {
        _waveOut?.Stop();
        if (_audioStream != null)
        {
            _audioStream.Position = 0;
        }
        PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { IsPlaying = false });
    }

    private void DisposeAudio()
    {
        _waveOut?.Dispose();
        _waveOut = null;

        _audioStream?.Dispose();
        _audioStream = null;

        _audioMemoryStream?.Dispose();
        _audioMemoryStream = null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        DisposeAudio();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
