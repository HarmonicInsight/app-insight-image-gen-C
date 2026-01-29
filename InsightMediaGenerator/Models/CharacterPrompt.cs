using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace InsightMediaGenerator.Models;

public partial class CharacterPrompt : ObservableObject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}
