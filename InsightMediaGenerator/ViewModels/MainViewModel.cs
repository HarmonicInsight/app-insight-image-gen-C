using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppConfig _config;

    public SimpleImageViewModel SimpleImage { get; }
    public BatchImageViewModel BatchImage { get; }
    public AudioViewModel Audio { get; }
    public PromptBuilderViewModel PromptBuilder { get; }
    public AiAssistantViewModel AiAssistant { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _statusMessage = "待機中";

    [ObservableProperty]
    private string _currentPlan = "FREE";

    [ObservableProperty]
    private string _stableDiffusionStatus = "未接続";

    [ObservableProperty]
    private string _voicevoxStatus = "未接続";

    [ObservableProperty]
    private string? _licenseKey;

    [ObservableProperty]
    private DateTime? _licenseExpiresAt;

    [ObservableProperty]
    private bool _canLaunchSd;

    public MainViewModel(
        SimpleImageViewModel simpleImage,
        BatchImageViewModel batchImage,
        AudioViewModel audio,
        PromptBuilderViewModel promptBuilder,
        AiAssistantViewModel aiAssistant,
        AppConfig config)
    {
        SimpleImage = simpleImage;
        BatchImage = batchImage;
        Audio = audio;
        PromptBuilder = promptBuilder;
        AiAssistant = aiAssistant;
        _config = config;
        CanLaunchSd = !string.IsNullOrEmpty(config.StableDiffusion.WebuiBatPath);
    }

    public async Task InitializeAsync()
    {
        // Load license info
        await LoadLicenseInfoAsync();

        // Initialize sub view models
        await Task.WhenAll(
            SimpleImage.InitializeAsync(),
            BatchImage.InitializeAsync(),
            Audio.InitializeAsync()
        );

        // Check service connections
        await CheckServiceConnectionsAsync();
    }

    private async Task LoadLicenseInfoAsync()
    {
        // TODO: Implement license loading from local storage or API
        // For now, default to FREE plan
        await Task.CompletedTask;

        // Load from environment or config
        var licenseKey = Environment.GetEnvironmentVariable("INIG_LICENSE_KEY");
        if (!string.IsNullOrEmpty(licenseKey))
        {
            LicenseKey = licenseKey;
            // TODO: Validate license and set CurrentPlan accordingly
            // CurrentPlan = await ValidateLicenseAsync(licenseKey);
        }
    }

    private async Task CheckServiceConnectionsAsync()
    {
        // TODO: Implement actual connection checks
        await Task.CompletedTask;

        // These will be updated by the services when they connect
    }

    public void UpdateStableDiffusionStatus(bool connected)
    {
        StableDiffusionStatus = connected ? "接続中" : "未接続";
        UpdateStatusMessage();
    }

    public void UpdateVoicevoxStatus(bool connected)
    {
        VoicevoxStatus = connected ? "接続中" : "未接続";
        UpdateStatusMessage();
    }

    private void UpdateStatusMessage()
    {
        var sdConnected = StableDiffusionStatus == "接続中";
        var vvConnected = VoicevoxStatus == "接続中";

        if (sdConnected && vvConnected)
        {
            StatusMessage = "待機中 - 全サービス接続済み";
        }
        else if (sdConnected || vvConnected)
        {
            StatusMessage = "待機中 - 一部サービス未接続";
        }
        else
        {
            StatusMessage = "待機中 - サービス未接続";
        }
    }

    [RelayCommand]
    private void LaunchStableDiffusion()
    {
        var batPath = _config.StableDiffusion.WebuiBatPath;

        if (string.IsNullOrEmpty(batPath))
        {
            MessageBox.Show(
                "Stable Diffusion WebUI の起動パスが設定されていません。\n\n" +
                "appsettings.json の stable_diffusion.webui_bat_path に\n" +
                "webui-user.bat のパスを設定してください。\n\n" +
                "例: \"C:\\\\stable-diffusion-webui\\\\webui-user.bat\"",
                "Stable Diffusion 起動",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        if (!File.Exists(batPath))
        {
            MessageBox.Show(
                $"指定されたファイルが見つかりません:\n{batPath}\n\n" +
                "appsettings.json の stable_diffusion.webui_bat_path を確認してください。",
                "Stable Diffusion 起動エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return;
        }

        try
        {
            var workingDir = Path.GetDirectoryName(batPath) ?? "";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    WorkingDirectory = workingDir,
                    UseShellExecute = true
                }
            };
            process.Start();

            StatusMessage = "Stable Diffusion を起動中...";
            StableDiffusionStatus = "起動中";

            // Start polling for connection after launch
            _ = PollStableDiffusionConnectionAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Stable Diffusion の起動に失敗しました:\n{ex.Message}",
                "起動エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private async Task PollStableDiffusionConnectionAsync()
    {
        // Poll for connection every 5 seconds for up to 2 minutes
        for (int i = 0; i < 24; i++)
        {
            await Task.Delay(5000);

            // Re-check connection via SimpleImage ViewModel
            await SimpleImage.CheckStatusCommand.ExecuteAsync(null);
            if (SimpleImage.SdConnected)
            {
                UpdateStableDiffusionStatus(true);
                await BatchImage.CheckStatusCommand.ExecuteAsync(null);
                return;
            }
        }

        // Timeout - still not connected
        StableDiffusionStatus = "起動タイムアウト";
        UpdateStatusMessage();
    }

    public void ShowLicenseDialog()
    {
        var planInfo = CurrentPlan switch
        {
            "FREE" => "フリープラン (機能制限あり)",
            "TRIAL" => $"トライアルプラン (有効期限: {LicenseExpiresAt?.ToString("yyyy/MM/dd") ?? "不明"})",
            "STD" => $"スタンダードプラン (有効期限: {LicenseExpiresAt?.ToString("yyyy/MM/dd") ?? "不明"})",
            "PRO" => $"プロプラン (有効期限: {LicenseExpiresAt?.ToString("yyyy/MM/dd") ?? "不明"})",
            "ENT" => "エンタープライズプラン",
            _ => "不明"
        };

        var message = $"InsightImageGen ライセンス情報\n\n" +
                      $"現在のプラン: {planInfo}\n\n" +
                      $"ライセンスキー: {(string.IsNullOrEmpty(LicenseKey) ? "未設定" : MaskLicenseKey(LicenseKey))}\n\n" +
                      "ライセンスの購入・更新については管理者にお問い合わせください。";

        var result = MessageBox.Show(
            message,
            "ライセンス管理",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information
        );

        if (result == MessageBoxResult.OK)
        {
            // Open license activation dialog
            ShowLicenseActivationDialog();
        }
    }

    private void ShowLicenseActivationDialog()
    {
        // TODO: Implement proper license activation dialog
        // For now, show a simple input dialog concept

        var message = "ライセンスキーを入力してください:\n\n" +
                      "形式: INIG-{プラン}-{YYMM}-{XXXX}-{XXXX}-{XXXX}\n" +
                      "例: INIG-PRO-2601-ABCD-EFGH-IJKL\n\n" +
                      "ライセンスキーの入力は今後のアップデートで\n" +
                      "専用ダイアログを実装予定です。\n\n" +
                      "現在は環境変数 INIG_LICENSE_KEY に設定してください。";

        MessageBox.Show(
            message,
            "ライセンス認証",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private static string MaskLicenseKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 10)
            return "****";

        // Show first 9 chars (INIG-XXX-) and mask the rest
        return key[..9] + new string('*', key.Length - 9);
    }

    /// <summary>
    /// Check if a feature is available for the current plan
    /// </summary>
    public bool CanUseFeature(string featureKey)
    {
        // Feature availability based on INIG product features from products.ts
        return featureKey switch
        {
            "generate_image" => true, // All plans
            "generate_audio" => true, // All plans
            "batch_image" => CurrentPlan is "TRIAL" or "STD" or "PRO" or "ENT",
            "hi_res" => CurrentPlan is "TRIAL" or "PRO" or "ENT",
            "cloud_sync" => CurrentPlan is "TRIAL" or "PRO" or "ENT",
            _ => false
        };
    }

    /// <summary>
    /// Get the limit for a feature (returns -1 for unlimited)
    /// </summary>
    public int GetFeatureLimit(string featureKey)
    {
        if (featureKey == "character_prompts")
        {
            return CurrentPlan switch
            {
                "FREE" => 3,
                "TRIAL" => -1,
                "STD" => 20,
                "PRO" => -1,
                "ENT" => -1,
                _ => 3
            };
        }

        return -1; // No limit by default
    }
}
