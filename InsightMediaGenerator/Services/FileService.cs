using System.IO;
using System.Text.Json;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Services;

public class FileService : IFileService
{
    private readonly string _basePath;

    public FileService()
    {
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    public string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (Path.IsPathRooted(path))
            return path;

        if (path.StartsWith("./"))
            path = path[2..];

        return Path.Combine(_basePath, path);
    }

    public async Task<string> SaveJsonFileAsync(string fileName, Stream content)
    {
        var jsonDir = ResolvePath("data/json_files");
        Directory.CreateDirectory(jsonDir);

        var filePath = Path.Combine(jsonDir, fileName);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);

        return filePath;
    }

    public Task DeleteJsonFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<CharacterPrompt>> LoadPromptsFromJsonAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Array.Empty<CharacterPrompt>();

        var json = await File.ReadAllTextAsync(filePath);
        var prompts = JsonSerializer.Deserialize<List<CharacterPrompt>>(json);

        return prompts ?? new List<CharacterPrompt>();
    }

    public async Task SaveImageAsync(string filePath, byte[] imageData)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, imageData);
    }

    public IReadOnlyList<string> GetFilesWithExtension(string directory, string extension)
    {
        var resolvedPath = ResolvePath(directory);

        if (!Directory.Exists(resolvedPath))
            return Array.Empty<string>();

        return Directory.GetFiles(resolvedPath, $"*{extension}")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();
    }
}
