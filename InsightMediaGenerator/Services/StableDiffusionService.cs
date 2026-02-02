using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Services;

public class StableDiffusionService : IStableDiffusionService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly IFileService _fileService;

    public StableDiffusionService(HttpClient httpClient, AppConfig config, IFileService fileService)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _config = config;
        _fileService = fileService;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/sdapi/v1/sd-models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<IReadOnlyList<string>> GetModelsAsync()
    {
        var modelsPath = _fileService.ResolvePath(_config.StableDiffusion.ModelsPath);
        var models = new List<string>();

        if (Directory.Exists(modelsPath))
        {
            var files = Directory.GetFiles(modelsPath, "*.safetensors")
                .Concat(Directory.GetFiles(modelsPath, "*.ckpt"));

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    models.Add(fileName);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(models.OrderBy(m => m).ToList());
    }

    public Task<IReadOnlyList<string>> GetLorasAsync()
    {
        var loraPath = _fileService.ResolvePath(_config.StableDiffusion.LoraPath);
        var loras = new List<string>();

        if (Directory.Exists(loraPath))
        {
            var files = Directory.GetFiles(loraPath, "*.safetensors");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!string.IsNullOrEmpty(fileName))
                {
                    loras.Add(fileName);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(loras.OrderBy(l => l).ToList());
    }

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var finalPrompt = request.Prompt;
            if (!string.IsNullOrEmpty(request.Lora) && request.Lora != "None")
            {
                var loraName = Path.GetFileNameWithoutExtension(request.Lora);
                finalPrompt = $"<lora:{loraName}:{request.LoraWeight}>, {request.Prompt}";
            }

            var payload = new
            {
                prompt = finalPrompt,
                negative_prompt = request.NegativePrompt,
                steps = request.Steps,
                width = request.Width,
                height = request.Height,
                sampler_name = request.SamplerName,
                cfg_scale = request.CfgScale,
                seed = -1,
                batch_size = 1,
                n_iter = 1,
                save_images = false,
                override_settings = new { sd_model_checkpoint = request.Model }
            };

            var response = await _httpClient.PostAsJsonAsync(
                _config.StableDiffusion.ApiUrl,
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SdApiResponse>(cancellationToken: cancellationToken);

            if (result?.Images == null || result.Images.Count == 0)
            {
                return new ImageGenerationResult { Success = false, ErrorMessage = "No images generated" };
            }

            var generatedImages = new List<ImageMetadata>();
            var outputPath = _fileService.ResolvePath(_config.StableDiffusion.OutputPath);
            Directory.CreateDirectory(outputPath);

            for (int i = 0; i < result.Images.Count; i++)
            {
                var imageBytes = Convert.FromBase64String(result.Images[i]);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var fileName = request.BatchCount > 1
                    ? $"{request.CharName}_batch{i + 1:D2}_{timestamp}.png"
                    : $"{request.CharName}_{timestamp}.png";
                var filePath = Path.Combine(outputPath, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

                generatedImages.Add(new ImageMetadata
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Timestamp = DateTime.Now,
                    Model = request.Model,
                    Lora = request.Lora,
                    LoraWeight = request.LoraWeight,
                    Prompt = request.Prompt,
                    NegativePrompt = request.NegativePrompt,
                    Steps = request.Steps,
                    Width = request.Width,
                    Height = request.Height,
                    SamplerName = request.SamplerName,
                    CfgScale = request.CfgScale,
                    CharName = request.CharName,
                    JsonFileName = request.JsonFileName,
                    JsonFileId = request.JsonFileId,
                    BatchIndex = i + 1
                });
            }

            return new ImageGenerationResult { Success = true, GeneratedImages = generatedImages };
        }
        catch (OperationCanceledException)
        {
            return new ImageGenerationResult { Success = false, ErrorMessage = "Generation cancelled" };
        }
        catch (Exception ex)
        {
            return new ImageGenerationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private string GetBaseUrl()
    {
        var apiUrl = _config.StableDiffusion.ApiUrl;
        var index = apiUrl.IndexOf("/sdapi");
        return index > 0 ? apiUrl[..index] : apiUrl;
    }

    private class SdApiResponse
    {
        public List<string> Images { get; set; } = new();
    }
}
