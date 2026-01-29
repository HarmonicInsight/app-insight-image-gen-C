namespace InsightMediaGenerator.Models;

public class ImageGenerationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Lora { get; set; }
    public double LoraWeight { get; set; }
    public int Steps { get; set; }
    public double CfgScale { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string SamplerName { get; set; } = "DPM++ 2M Karras";
    public string CharName { get; set; } = string.Empty;
    public string? JsonFileName { get; set; }
    public int? JsonFileId { get; set; }
    public int BatchCount { get; set; } = 1;
}

public class ImageGenerationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ImageMetadata> GeneratedImages { get; set; } = new();
}
