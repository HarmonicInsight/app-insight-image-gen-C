using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.Services.Interfaces;

public interface IVoicevoxService
{
    Task<bool> CheckConnectionAsync();
    Task<bool> DiscoverEngineAsync();
    Task<IReadOnlyList<Speaker>> GetSpeakersAsync();
    Task<AudioGenerationResult> GenerateAudioAsync(AudioGenerationParams parameters, CancellationToken cancellationToken = default);
}
