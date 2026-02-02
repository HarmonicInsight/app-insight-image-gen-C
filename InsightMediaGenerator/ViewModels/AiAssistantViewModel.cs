using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.ViewModels;

public partial class AiAssistantViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;

    [ObservableProperty]
    private string _apiUrl = "https://api.openai.com/v1/chat/completions";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _selectedModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _userMessage = string.Empty;

    [ObservableProperty]
    private string _systemPrompt = "あなたはStable Diffusion用の画像生成プロンプトを作成するアシスタントです。ユーザーが日本語で画像のイメージを伝えたら、英語のStable DiffusionプロンプトとネガティブプロンプトをJSON形式で返してください。\n\n出力形式:\n{\"prompt\": \"英語プロンプト\", \"negative_prompt\": \"英語ネガティブプロンプト\"}";

    [ObservableProperty]
    private string _aiResponse = string.Empty;

    [ObservableProperty]
    private string _extractedPrompt = string.Empty;

    [ObservableProperty]
    private string _extractedNegativePrompt = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _selectedTemplate = string.Empty;

    [ObservableProperty]
    private string _requestJsonPreview = string.Empty;

    public ObservableCollection<string> Models { get; } = new()
    {
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4-turbo",
        "gpt-3.5-turbo",
        "claude-3-haiku-20240307",
        "claude-3-sonnet-20240229",
    };

    public ObservableCollection<string> Templates { get; } = new()
    {
        "プロンプト生成（日本語→英語）",
        "プロンプト改善",
        "キャラクター設定から生成",
        "風景画プロンプト生成",
        "カスタム",
    };

    public AiAssistantViewModel(HttpClient httpClient, AppConfig config)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
        _config = config;

        // 環境変数からAPIキーを読み込み
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                  ?? Environment.GetEnvironmentVariable("AI_API_KEY")
                  ?? string.Empty;
        ApiKey = envKey;

        SelectedTemplate = Templates[0];
        UpdateJsonPreview();
    }

    partial void OnSelectedTemplateChanged(string value)
    {
        SystemPrompt = value switch
        {
            "プロンプト生成（日本語→英語）" =>
                "あなたはStable Diffusion用の画像生成プロンプトを作成するアシスタントです。ユーザーが日本語で画像のイメージを伝えたら、英語のStable DiffusionプロンプトとネガティブプロンプトをJSON形式で返してください。\n\n出力形式:\n{\"prompt\": \"英語プロンプト\", \"negative_prompt\": \"英語ネガティブプロンプト\"}",

            "プロンプト改善" =>
                "あなたはStable Diffusionプロンプトの専門家です。ユーザーが入力したプロンプトを改善し、より高品質な画像が生成されるようにしてください。品質タグ、光の表現、構図の指定を追加して改善版を提案してください。\n\n出力形式:\n{\"prompt\": \"改善された英語プロンプト\", \"negative_prompt\": \"推奨ネガティブプロンプト\"}",

            "キャラクター設定から生成" =>
                "あなたはキャラクターデザイン用のプロンプト生成アシスタントです。ユーザーがキャラクターの設定（性格、外見、服装など）を日本語で伝えたら、そのキャラクターを描くためのStable Diffusionプロンプトを作成してください。\n\n出力形式:\n{\"prompt\": \"キャラクター描写の英語プロンプト\", \"negative_prompt\": \"英語ネガティブプロンプト\"}",

            "風景画プロンプト生成" =>
                "あなたは風景画・背景画のプロンプト生成アシスタントです。ユーザーが日本語で風景のイメージを伝えたら、美しい風景画を生成するためのStable Diffusionプロンプトを作成してください。光、天候、季節、時間帯の表現を豊かに含めてください。\n\n出力形式:\n{\"prompt\": \"風景描写の英語プロンプト\", \"negative_prompt\": \"英語ネガティブプロンプト\"}",

            _ => SystemPrompt
        };

        UpdateJsonPreview();
    }

    partial void OnUserMessageChanged(string value) => UpdateJsonPreview();
    partial void OnSystemPromptChanged(string value) => UpdateJsonPreview();
    partial void OnSelectedModelChanged(string value) => UpdateJsonPreview();

    private void UpdateJsonPreview()
    {
        var request = BuildRequest();
        var options = new JsonSerializerOptions { WriteIndented = true };
        RequestJsonPreview = JsonSerializer.Serialize(request, options);
    }

    private AiChatRequest BuildRequest()
    {
        return new AiChatRequest
        {
            Model = SelectedModel,
            Messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = SystemPrompt },
                new() { Role = "user", Content = string.IsNullOrEmpty(UserMessage) ? "(メッセージを入力してください)" : UserMessage }
            },
            MaxTokens = 1024,
            Temperature = 0.7
        };
    }

    [RelayCommand]
    private async Task SendToAiAsync()
    {
        if (string.IsNullOrWhiteSpace(UserMessage))
        {
            StatusMessage = "メッセージを入力してください";
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "APIキーを設定してください（環境変数 OPENAI_API_KEY または画面上で入力）";
            return;
        }

        IsSending = true;
        StatusMessage = "AIに送信中...";
        AiResponse = string.Empty;
        ExtractedPrompt = string.Empty;
        ExtractedNegativePrompt = string.Empty;

        try
        {
            var request = BuildRequest();
            var json = JsonSerializer.Serialize(request);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            httpRequest.Headers.Add("Authorization", $"Bearer {ApiKey}");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                StatusMessage = $"APIエラー: {response.StatusCode}";
                AiResponse = responseBody;
                return;
            }

            var chatResponse = JsonSerializer.Deserialize<AiChatResponse>(responseBody);
            var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            AiResponse = content;

            // JSONからプロンプトを抽出
            TryExtractPrompts(content);

            StatusMessage = "レスポンスを受信しました";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
            AiResponse = ex.ToString();
        }
        finally
        {
            IsSending = false;
        }
    }

    private void TryExtractPrompts(string content)
    {
        try
        {
            // JSON部分を探す
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var jsonPart = content.Substring(start, end - start + 1);
                using var doc = JsonDocument.Parse(jsonPart);
                var root = doc.RootElement;

                if (root.TryGetProperty("prompt", out var promptEl))
                    ExtractedPrompt = promptEl.GetString() ?? string.Empty;

                if (root.TryGetProperty("negative_prompt", out var negEl))
                    ExtractedNegativePrompt = negEl.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // JSON抽出に失敗した場合はそのまま
        }
    }

    [RelayCommand]
    private void CopyExtractedPrompt()
    {
        if (!string.IsNullOrEmpty(ExtractedPrompt))
            System.Windows.Clipboard.SetText(ExtractedPrompt);
    }

    [RelayCommand]
    private void CopyExtractedNegative()
    {
        if (!string.IsNullOrEmpty(ExtractedNegativePrompt))
            System.Windows.Clipboard.SetText(ExtractedNegativePrompt);
    }

    [RelayCommand]
    private void CopyRequestJson()
    {
        if (!string.IsNullOrEmpty(RequestJsonPreview))
            System.Windows.Clipboard.SetText(RequestJsonPreview);
    }

    [RelayCommand]
    private async Task ExportRequestJsonAsync()
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "ai_templates");
            Directory.CreateDirectory(dir);

            var fileName = $"ai_request_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(dir, fileName);

            await File.WriteAllTextAsync(filePath, RequestJsonPreview, Encoding.UTF8);
            StatusMessage = $"保存しました: {fileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存エラー: {ex.Message}";
        }
    }
}
