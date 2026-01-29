namespace InsightMediaGenerator.Models;

public class JsonFileRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Comment { get; set; }
}
