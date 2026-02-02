using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.ViewModels;

public partial class PromptBuilderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _builtPrompt = string.Empty;

    [ObservableProperty]
    private string _builtNegativePrompt = string.Empty;

    [ObservableProperty]
    private string _customKeyword = string.Empty;

    [ObservableProperty]
    private string _customNegativeKeyword = string.Empty;

    public ObservableCollection<KeywordCategory> Categories { get; } = new();
    public ObservableCollection<NegativeKeyword> NegativeKeywords { get; } = new();

    public PromptBuilderViewModel()
    {
        InitializeKeywords();
        InitializeNegativeKeywords();
        RebuildNegativePrompt();
    }

    private void InitializeKeywords()
    {
        // ========== 画風・スタイル ==========
        var styleCategory = new KeywordCategory
        {
            Name = "画風・スタイル",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "アニメ風", PromptValue = "anime style", Category = "画風・スタイル" },
                new() { DisplayName = "リアル風", PromptValue = "photorealistic", Category = "画風・スタイル" },
                new() { DisplayName = "水彩画風", PromptValue = "watercolor painting", Category = "画風・スタイル" },
                new() { DisplayName = "油絵風", PromptValue = "oil painting", Category = "画風・スタイル" },
                new() { DisplayName = "イラスト風", PromptValue = "illustration", Category = "画風・スタイル" },
                new() { DisplayName = "3DCG風", PromptValue = "3d render", Category = "画風・スタイル" },
                new() { DisplayName = "ピクセルアート", PromptValue = "pixel art", Category = "画風・スタイル" },
                new() { DisplayName = "スケッチ風", PromptValue = "pencil sketch", Category = "画風・スタイル" },
                new() { DisplayName = "浮世絵風", PromptValue = "ukiyo-e style", Category = "画風・スタイル" },
                new() { DisplayName = "コンセプトアート", PromptValue = "concept art", Category = "画風・スタイル" },
                new() { DisplayName = "ファンタジー", PromptValue = "fantasy art", Category = "画風・スタイル" },
                new() { DisplayName = "サイバーパンク", PromptValue = "cyberpunk style", Category = "画風・スタイル" },
            }
        };

        // ========== 品質・クオリティ ==========
        var qualityCategory = new KeywordCategory
        {
            Name = "品質・クオリティ",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "高品質", PromptValue = "masterpiece, best quality", Category = "品質・クオリティ" },
                new() { DisplayName = "超高解像度", PromptValue = "ultra detailed, 8k", Category = "品質・クオリティ" },
                new() { DisplayName = "繊細な描写", PromptValue = "highly detailed", Category = "品質・クオリティ" },
                new() { DisplayName = "シャープ", PromptValue = "sharp focus", Category = "品質・クオリティ" },
                new() { DisplayName = "美しい色彩", PromptValue = "vibrant colors", Category = "品質・クオリティ" },
                new() { DisplayName = "プロ品質", PromptValue = "professional quality", Category = "品質・クオリティ" },
            }
        };

        // ========== 光・雰囲気 ==========
        var lightCategory = new KeywordCategory
        {
            Name = "光・雰囲気",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "柔らかい光", PromptValue = "soft lighting", Category = "光・雰囲気" },
                new() { DisplayName = "ドラマチック", PromptValue = "dramatic lighting", Category = "光・雰囲気" },
                new() { DisplayName = "逆光", PromptValue = "backlit", Category = "光・雰囲気" },
                new() { DisplayName = "夕焼け色", PromptValue = "golden hour lighting", Category = "光・雰囲気" },
                new() { DisplayName = "ネオンライト", PromptValue = "neon lighting", Category = "光・雰囲気" },
                new() { DisplayName = "暗い雰囲気", PromptValue = "dark atmosphere", Category = "光・雰囲気" },
                new() { DisplayName = "明るく鮮やか", PromptValue = "bright and vivid", Category = "光・雰囲気" },
                new() { DisplayName = "映画的", PromptValue = "cinematic lighting", Category = "光・雰囲気" },
                new() { DisplayName = "幻想的", PromptValue = "ethereal glow", Category = "光・雰囲気" },
            }
        };

        // ========== 人物・キャラクター ==========
        var characterCategory = new KeywordCategory
        {
            Name = "人物・キャラクター",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "女の子", PromptValue = "1girl", Category = "人物・キャラクター" },
                new() { DisplayName = "男の子", PromptValue = "1boy", Category = "人物・キャラクター" },
                new() { DisplayName = "笑顔", PromptValue = "smile, happy expression", Category = "人物・キャラクター" },
                new() { DisplayName = "クールな表情", PromptValue = "cool expression", Category = "人物・キャラクター" },
                new() { DisplayName = "黒髪ロング", PromptValue = "long black hair", Category = "人物・キャラクター" },
                new() { DisplayName = "金髪ショート", PromptValue = "short blonde hair", Category = "人物・キャラクター" },
                new() { DisplayName = "銀髪", PromptValue = "silver hair", Category = "人物・キャラクター" },
                new() { DisplayName = "赤い瞳", PromptValue = "red eyes", Category = "人物・キャラクター" },
                new() { DisplayName = "青い瞳", PromptValue = "blue eyes", Category = "人物・キャラクター" },
                new() { DisplayName = "制服", PromptValue = "school uniform", Category = "人物・キャラクター" },
                new() { DisplayName = "ドレス", PromptValue = "elegant dress", Category = "人物・キャラクター" },
                new() { DisplayName = "着物", PromptValue = "kimono", Category = "人物・キャラクター" },
                new() { DisplayName = "鎧・アーマー", PromptValue = "armor", Category = "人物・キャラクター" },
                new() { DisplayName = "カジュアル服", PromptValue = "casual outfit", Category = "人物・キャラクター" },
            }
        };

        // ========== 背景・場所 ==========
        var bgCategory = new KeywordCategory
        {
            Name = "背景・場所",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "街並み", PromptValue = "city street background", Category = "背景・場所" },
                new() { DisplayName = "森・自然", PromptValue = "forest, nature background", Category = "背景・場所" },
                new() { DisplayName = "海辺", PromptValue = "beach, ocean background", Category = "背景・場所" },
                new() { DisplayName = "夜空・星空", PromptValue = "night sky, starry sky", Category = "背景・場所" },
                new() { DisplayName = "桜並木", PromptValue = "cherry blossom trees", Category = "背景・場所" },
                new() { DisplayName = "教室", PromptValue = "classroom background", Category = "背景・場所" },
                new() { DisplayName = "城・ファンタジー", PromptValue = "fantasy castle background", Category = "背景・場所" },
                new() { DisplayName = "近未来都市", PromptValue = "futuristic city", Category = "背景・場所" },
                new() { DisplayName = "和風庭園", PromptValue = "japanese garden", Category = "背景・場所" },
                new() { DisplayName = "白背景", PromptValue = "white background, simple background", Category = "背景・場所" },
            }
        };

        // ========== 構図・アングル ==========
        var compositionCategory = new KeywordCategory
        {
            Name = "構図・アングル",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "アップ（顔）", PromptValue = "close-up, portrait", Category = "構図・アングル" },
                new() { DisplayName = "上半身", PromptValue = "upper body", Category = "構図・アングル" },
                new() { DisplayName = "全身", PromptValue = "full body", Category = "構図・アングル" },
                new() { DisplayName = "俯瞰", PromptValue = "bird's eye view", Category = "構図・アングル" },
                new() { DisplayName = "あおり", PromptValue = "low angle", Category = "構図・アングル" },
                new() { DisplayName = "横顔", PromptValue = "side view, profile", Category = "構図・アングル" },
                new() { DisplayName = "後ろ姿", PromptValue = "from behind", Category = "構図・アングル" },
                new() { DisplayName = "ダイナミック", PromptValue = "dynamic angle", Category = "構図・アングル" },
            }
        };

        // ========== 物・アイテム ==========
        var itemCategory = new KeywordCategory
        {
            Name = "物・アイテム",
            Keywords = new List<PromptKeyword>
            {
                new() { DisplayName = "剣", PromptValue = "sword, holding sword", Category = "物・アイテム" },
                new() { DisplayName = "魔法の杖", PromptValue = "magic staff", Category = "物・アイテム" },
                new() { DisplayName = "花", PromptValue = "flowers", Category = "物・アイテム" },
                new() { DisplayName = "本", PromptValue = "book, holding book", Category = "物・アイテム" },
                new() { DisplayName = "傘", PromptValue = "umbrella", Category = "物・アイテム" },
                new() { DisplayName = "猫", PromptValue = "cat", Category = "物・アイテム" },
                new() { DisplayName = "翼・羽", PromptValue = "wings, angel wings", Category = "物・アイテム" },
                new() { DisplayName = "眼鏡", PromptValue = "glasses", Category = "物・アイテム" },
                new() { DisplayName = "帽子", PromptValue = "hat", Category = "物・アイテム" },
                new() { DisplayName = "リボン", PromptValue = "ribbon", Category = "物・アイテム" },
            }
        };

        Categories.Add(qualityCategory);
        Categories.Add(styleCategory);
        Categories.Add(lightCategory);
        Categories.Add(characterCategory);
        Categories.Add(bgCategory);
        Categories.Add(compositionCategory);
        Categories.Add(itemCategory);

        // キーワードの選択状態変更を監視
        foreach (var cat in Categories)
        {
            foreach (var kw in cat.Keywords)
            {
                kw.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PromptKeyword.IsSelected))
                        RebuildPrompt();
                };
            }
        }
    }

    private void InitializeNegativeKeywords()
    {
        var defaults = new List<NegativeKeyword>
        {
            new() { DisplayName = "低品質", PromptValue = "low quality", IsSelected = true },
            new() { DisplayName = "最低品質", PromptValue = "worst quality", IsSelected = true },
            new() { DisplayName = "低解像度", PromptValue = "low resolution", IsSelected = true },
            new() { DisplayName = "崩れた体", PromptValue = "bad anatomy", IsSelected = true },
            new() { DisplayName = "崩れた手", PromptValue = "bad hands, mutated hands", IsSelected = true },
            new() { DisplayName = "崩れた顔", PromptValue = "deformed face", IsSelected = true },
            new() { DisplayName = "余分な指", PromptValue = "extra fingers, extra limbs", IsSelected = true },
            new() { DisplayName = "ぼやけ", PromptValue = "blurry", IsSelected = true },
            new() { DisplayName = "ノイズ", PromptValue = "noise, grainy", IsSelected = false },
            new() { DisplayName = "テキスト混入", PromptValue = "text, watermark, signature", IsSelected = true },
            new() { DisplayName = "不自然な目", PromptValue = "ugly eyes, asymmetrical eyes", IsSelected = false },
            new() { DisplayName = "切断", PromptValue = "cropped, cut off", IsSelected = false },
            new() { DisplayName = "重複", PromptValue = "duplicate, clone", IsSelected = false },
            new() { DisplayName = "NSFW", PromptValue = "nsfw, nude", IsSelected = false },
        };

        foreach (var nk in defaults)
        {
            nk.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NegativeKeyword.IsSelected))
                    RebuildNegativePrompt();
            };
            NegativeKeywords.Add(nk);
        }
    }

    private void RebuildPrompt()
    {
        var selected = Categories
            .SelectMany(c => c.Keywords)
            .Where(k => k.IsSelected)
            .Select(k => k.PromptValue);

        BuiltPrompt = string.Join(", ", selected);
    }

    private void RebuildNegativePrompt()
    {
        var selected = NegativeKeywords
            .Where(k => k.IsSelected)
            .Select(k => k.PromptValue);

        BuiltNegativePrompt = string.Join(", ", selected);
    }

    [RelayCommand]
    private void AddCustomKeyword()
    {
        if (string.IsNullOrWhiteSpace(CustomKeyword)) return;

        if (!string.IsNullOrEmpty(BuiltPrompt))
            BuiltPrompt += ", ";
        BuiltPrompt += CustomKeyword.Trim();
        CustomKeyword = string.Empty;
    }

    [RelayCommand]
    private void AddCustomNegativeKeyword()
    {
        if (string.IsNullOrWhiteSpace(CustomNegativeKeyword)) return;

        var nk = new NegativeKeyword
        {
            DisplayName = CustomNegativeKeyword.Trim(),
            PromptValue = CustomNegativeKeyword.Trim(),
            IsSelected = true
        };
        nk.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NegativeKeyword.IsSelected))
                RebuildNegativePrompt();
        };
        NegativeKeywords.Add(nk);
        CustomNegativeKeyword = string.Empty;
        RebuildNegativePrompt();
    }

    [RelayCommand]
    private void RemoveNegativeKeyword(NegativeKeyword keyword)
    {
        NegativeKeywords.Remove(keyword);
        RebuildNegativePrompt();
    }

    [RelayCommand]
    private void ClearAllSelections()
    {
        foreach (var cat in Categories)
            foreach (var kw in cat.Keywords)
                kw.IsSelected = false;

        BuiltPrompt = string.Empty;
    }

    [RelayCommand]
    private void CopyPromptToClipboard()
    {
        if (!string.IsNullOrEmpty(BuiltPrompt))
            System.Windows.Clipboard.SetText(BuiltPrompt);
    }

    [RelayCommand]
    private void CopyNegativeToClipboard()
    {
        if (!string.IsNullOrEmpty(BuiltNegativePrompt))
            System.Windows.Clipboard.SetText(BuiltNegativePrompt);
    }
}
