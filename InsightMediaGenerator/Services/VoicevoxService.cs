using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Services;

public class VoicevoxService : IVoicevoxService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly IFileService _fileService;
    private string _baseUrl;

    public VoicevoxService(HttpClient httpClient, AppConfig config, IFileService fileService)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _config = config;
        _fileService = fileService;
        _baseUrl = config.Voicevox.ApiUrl;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/version");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DiscoverEngineAsync()
    {
        // Try default port first
        if (await TryPortAsync(50021))
            return true;

        // Scan port range
        for (int port = 50020; port <= 50100; port++)
        {
            if (await TryPortAsync(port))
                return true;
        }

        return false;
    }

    private async Task<bool> TryPortAsync(int port)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var response = await _httpClient.GetAsync($"http://127.0.0.1:{port}/version", cts.Token);
            if (response.IsSuccessStatusCode)
            {
                _baseUrl = $"http://127.0.0.1:{port}";
                return true;
            }
        }
        catch { }
        return false;
    }

    public async Task<IReadOnlyList<Speaker>> GetSpeakersAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/speakers");
        response.EnsureSuccessStatusCode();

        var rawSpeakers = await response.Content.ReadFromJsonAsync<List<RawSpeaker>>();
        var speakers = new List<Speaker>();

        foreach (var speaker in rawSpeakers ?? new())
        {
            foreach (var style in speaker.Styles ?? new())
            {
                speakers.Add(new Speaker
                {
                    Id = style.Id,
                    Name = $"{speaker.Name} ({style.Name})",
                    SpeakerName = speaker.Name ?? "",
                    StyleName = style.Name ?? ""
                });
            }
        }

        return speakers;
    }

    public async Task<AudioGenerationResult> GenerateAudioAsync(AudioGenerationParams parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create audio query
            var queryUrl = $"{_baseUrl}/audio_query?text={Uri.EscapeDataString(parameters.Text)}&speaker={parameters.SpeakerId}";
            var queryResponse = await _httpClient.PostAsync(queryUrl, null, cancellationToken);
            queryResponse.EnsureSuccessStatusCode();

            var queryJson = await queryResponse.Content.ReadAsStringAsync(cancellationToken);
            using var queryDoc = JsonDocument.Parse(queryJson);
            var queryDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(queryJson);

            if (queryDict == null)
            {
                return new AudioGenerationResult { Success = false, ErrorMessage = "Failed to create audio query" };
            }

            // Update parameters
            queryDict["speedScale"] = JsonSerializer.SerializeToElement(parameters.Speed);
            queryDict["pitchScale"] = JsonSerializer.SerializeToElement(parameters.Pitch);
            queryDict["intonationScale"] = JsonSerializer.SerializeToElement(parameters.Intonation);
            queryDict["volumeScale"] = JsonSerializer.SerializeToElement(parameters.Volume);

            // Synthesize
            var synthesisUrl = $"{_baseUrl}/synthesis?speaker={parameters.SpeakerId}";
            var content = new StringContent(JsonSerializer.Serialize(queryDict), Encoding.UTF8, "application/json");
            var synthesisResponse = await _httpClient.PostAsync(synthesisUrl, content, cancellationToken);
            synthesisResponse.EnsureSuccessStatusCode();

            var audioData = await synthesisResponse.Content.ReadAsByteArrayAsync(cancellationToken);

            string? filePath = null;
            if (parameters.SaveFile)
            {
                var outputDir = _fileService.ResolvePath(_config.Voicevox.OutputPath);
                Directory.CreateDirectory(outputDir);

                var fileName = string.IsNullOrEmpty(parameters.FileName)
                    ? $"audio_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.wav"
                    : parameters.FileName.EndsWith(".wav") ? parameters.FileName : $"{parameters.FileName}.wav";

                filePath = Path.Combine(outputDir, fileName);
                await File.WriteAllBytesAsync(filePath, audioData, cancellationToken);
            }

            return new AudioGenerationResult
            {
                Success = true,
                AudioData = audioData,
                FilePath = filePath
            };
        }
        catch (OperationCanceledException)
        {
            return new AudioGenerationResult { Success = false, ErrorMessage = "Generation cancelled" };
        }
        catch (Exception ex)
        {
            return new AudioGenerationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private class RawSpeaker
    {
        public string? Name { get; set; }
        public List<RawStyle>? Styles { get; set; }
    }

    private class RawStyle
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
