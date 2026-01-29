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
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://harmonic-insight.com/docs/insight-image-gen",
            UseShellExecute = true
        });
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
