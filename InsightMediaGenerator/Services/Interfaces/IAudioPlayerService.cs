namespace InsightMediaGenerator.Services.Interfaces;

public interface IAudioPlayerService : IDisposable
{
    void LoadAudio(byte[] audioData);
    void Play();
    void Pause();
    void Stop();
    bool IsPlaying { get; }
    TimeSpan Position { get; set; }
    TimeSpan Duration { get; }
    event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
}

public class PlaybackStateChangedEventArgs : EventArgs
{
    public bool IsPlaying { get; set; }
}
