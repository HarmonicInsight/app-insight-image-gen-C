namespace InsightMediaGenerator.Models;

public class Speaker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpeakerName { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;

    public string DisplayName => $"{SpeakerName} ({StyleName})";
}
