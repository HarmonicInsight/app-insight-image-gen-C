using System.IO;
using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.Services.Interfaces;

public interface IFileService
{
    Task<string> SaveJsonFileAsync(string fileName, Stream content);
    Task DeleteJsonFileAsync(string filePath);
    Task<IReadOnlyList<CharacterPrompt>> LoadPromptsFromJsonAsync(string filePath);
    Task SaveImageAsync(string filePath, byte[] imageData);
    IReadOnlyList<string> GetFilesWithExtension(string directory, string extension);
    string ResolvePath(string path);
}
