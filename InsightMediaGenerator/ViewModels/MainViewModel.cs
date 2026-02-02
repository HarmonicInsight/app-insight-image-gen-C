using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.License;
using InsightMediaGenerator.Models;

namespace InsightMediaGenerator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppConfig _config;
    private readonly InsightLicenseManager _licenseManager;

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
        AppConfig config,
        InsightLicenseManager licenseManager)
    {
        SimpleImage = simpleImage;
        BatchImage = batchImage;
        Audio = audio;
        PromptBuilder = promptBuilder;
        AiAssistant = aiAssistant;
        _config = config;
        _licenseManager = licenseManager;
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
        await _licenseManager.LoadAsync();

        CurrentPlan = _licenseManager.CurrentPlan;
        LicenseKey = _licenseManager.LicenseKey;
        LicenseExpiresAt = _licenseManager.ExpiresAt;
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
        var planName = CurrentPlan switch
        {
            "FREE" => "FREE (フリー)",
            "TRIAL" => "TRIAL (トライアル)",
            "STD" => "STD (スタンダード)",
            "PRO" => "PRO (プロフェッショナル)",
            "ENT" => "ENT (エンタープライズ)",
            _ => CurrentPlan
        };

        var expiryInfo = CurrentPlan switch
        {
            "FREE" => "無期限 (機能制限あり)",
            "ENT" when !LicenseExpiresAt.HasValue => "永久ライセンス",
            _ when LicenseExpiresAt.HasValue => $"{LicenseExpiresAt.Value:yyyy/MM/dd}",
            _ => "不明"
        };

        var maskedKey = InsightLicenseManager.MaskKey(LicenseKey);

        var message = $"製品: InsightImageGen ({_licenseManager.ProductCodeDisplay})\n" +
                      $"プラン: {planName}\n" +
                      $"有効期限: {expiryInfo}\n" +
                      $"ライセンスキー: {maskedKey}\n\n" +
                      "ライセンスの購入・更新については管理者にお問い合わせください。\n" +
                      "キー形式: INIG-{PLAN}-{YYMM}-{HASH}-{SIG1}-{SIG2}";

        var result = MessageBox.Show(
            message,
            "ライセンス管理 - InsightImageGen",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information
        );

        if (result == MessageBoxResult.OK)
        {
            ShowLicenseActivationDialog();
        }
    }

    private void ShowLicenseActivationDialog()
    {
        var message = "ライセンスキーを入力してください:\n\n" +
                      "形式: INIG-{PLAN}-{YYMM}-{HASH}-{SIG1}-{SIG2}\n" +
                      "例: INIG-PRO-2701-ABCD-EFGH-IJKL\n\n" +
                      "PLAN: TRIAL / STD / PRO / ENT\n" +
                      "YYMM: 有効期限 (年月, 例: 2701=2027年1月)\n\n" +
                      "現在は環境変数 INIG_LICENSE_KEY に設定してください。";

        MessageBox.Show(
            message,
            "ライセンス認証 - InsightImageGen",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    /// <summary>
    /// Check if a feature is available for the current plan
    /// </summary>
    public bool CanUseFeature(string featureKey) => _licenseManager.CheckFeature(featureKey);

    /// <summary>
    /// Get the limit for a feature (returns -1 for unlimited)
    /// </summary>
    public int GetFeatureLimit(string featureKey) => _licenseManager.GetFeatureLimit(featureKey);
}
