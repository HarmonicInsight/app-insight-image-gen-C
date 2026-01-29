using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.Services.Interfaces;

public interface IStableDiffusionService
{
    Task<bool> CheckConnectionAsync();
    Task<IReadOnlyList<string>> GetModelsAsync();
    Task<IReadOnlyList<string>> GetLorasAsync();
    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default);
}
