namespace InsightMediaGenerator.Models;

public class ImageMetadata
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Model { get; set; }
    public string? Lora { get; set; }
    public double LoraWeight { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public int Steps { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string SamplerName { get; set; } = string.Empty;
    public double CfgScale { get; set; }
    public string CharName { get; set; } = string.Empty;
    public string? JsonFileName { get; set; }
    public int? JsonFileId { get; set; }
    public int BatchIndex { get; set; }
}
