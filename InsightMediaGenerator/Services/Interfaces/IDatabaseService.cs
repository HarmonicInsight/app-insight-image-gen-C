using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.Services.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<int> SaveJsonFileAsync(JsonFileRecord record);
    Task<IReadOnlyList<JsonFileRecord>> GetJsonFilesAsync();
    Task DeleteJsonFileAsync(int id);
    Task UpdateJsonCommentAsync(int id, string comment);
    Task SaveImageMetadataAsync(ImageMetadata metadata);
    Task<IReadOnlyList<ImageMetadata>> GetImagesAsync();
}
