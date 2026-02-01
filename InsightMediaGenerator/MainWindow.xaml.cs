using System.Diagnostics;
using System.IO;
using System.Windows;
using InsightMediaGenerator.ViewModels;

namespace InsightMediaGenerator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.InitializeAsync();
    }

    // ========================================
    // Menu: ファイル
    // ========================================

    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InsightImageGen"
        );

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        Process.Start("explorer.exe", appDataPath);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // ========================================
    // Menu: 表示
    // ========================================

    private void ViewSimpleImage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedTabIndex = 0;
        }
    }

    private void ViewBatchImage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedTabIndex = 1;
        }
    }

    private void ViewAudio_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedTabIndex = 2;
        }
    }

    // ========================================
    // Menu: ヘルプ
    // ========================================

    private void OpenDocs_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "InsightImageGen ドキュメント\n\n" +
            "【基本操作】\n" +
            "1. Stable Diffusion WebUI を起動してください\n" +
            "   (ファイル → Stable Diffusion を起動)\n" +
            "2. Simple Image タブでプロンプトを入力し Generate をクリック\n" +
            "3. Batch Image タブでは JSON ファイルから一括生成が可能です\n\n" +
            "【設定】\n" +
            "appsettings.json で以下を設定できます:\n" +
            "  - stable_diffusion.api_url: SD WebUI の API URL\n" +
            "  - stable_diffusion.webui_bat_path: webui-user.bat のパス\n" +
            "  - stable_diffusion.models_path: モデルフォルダのパス\n" +
            "  - stable_diffusion.lora_path: LoRA フォルダのパス\n" +
            "  - stable_diffusion.output_path: 出力先フォルダのパス\n" +
            "  - voicevox.api_url: VOICEVOX の API URL\n\n" +
            "【音声生成】\n" +
            "VOICEVOX を起動した状態で Audio タブからテキスト読み上げが可能です\n\n" +
            "【ライセンス】\n" +
            "環境変数 INIG_LICENSE_KEY にライセンスキーを設定してください",
            "ドキュメント",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "InsightImageGen v1.0.0\n\n" +
            "Stable Diffusion・VOICEVOXを活用したAI画像・音声生成ツール\n\n" +
            "Copyright (c) Harmonic Insight. All rights reserved.",
            "バージョン情報",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    // ========================================
    // Header Actions
    // ========================================

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "設定画面は今後実装予定です。\n\n" +
            "現在は appsettings.json を直接編集してください。",
            "設定",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private void License_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowLicenseDialog();
        }
    }
}
