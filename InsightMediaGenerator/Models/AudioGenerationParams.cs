namespace InsightMediaGenerator.Models;

public class AudioGenerationParams
{
    public string Text { get; set; } = string.Empty;
    public int SpeakerId { get; set; }
    public double Speed { get; set; } = 1.0;
    public double Pitch { get; set; } = 0.0;
    public double Intonation { get; set; } = 1.0;
    public double Volume { get; set; } = 1.0;
    public bool SaveFile { get; set; } = true;
    public string? FileName { get; set; }
}

public class AudioGenerationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? AudioData { get; set; }
    public string? FilePath { get; set; }
}
