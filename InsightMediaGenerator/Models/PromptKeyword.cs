using CommunityToolkit.Mvvm.ComponentModel;

namespace InsightMediaGenerator.Models;

/// <summary>
/// プロンプトビルダー用のキーワード（選択可能なタグ）
/// </summary>
public partial class PromptKeyword : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>日本語表示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>実際にプロンプトに挿入される英語キーワード</summary>
    public string PromptValue { get; set; } = string.Empty;

    /// <summary>カテゴリ名</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// ネガティブプロンプト用のキーワード（トグル可能）
/// </summary>
public partial class NegativeKeyword : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    /// <summary>日本語表示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>実際にネガティブプロンプトに挿入される英語キーワード</summary>
    public string PromptValue { get; set; } = string.Empty;
}

/// <summary>
/// キーワードカテゴリ
/// </summary>
public class KeywordCategory
{
    public string Name { get; set; } = string.Empty;
    public List<PromptKeyword> Keywords { get; set; } = new();
}
