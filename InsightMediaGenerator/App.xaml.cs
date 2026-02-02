using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services;
using InsightMediaGenerator.Services.Interfaces;
using InsightMediaGenerator.ViewModels;

namespace InsightMediaGenerator;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // Load configuration
        var config = LoadConfiguration();
        services.AddSingleton(config);

        // Register services
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAudioPlayerService, AudioPlayerService>();

        // Register HTTP clients for API services
        services.AddHttpClient<IStableDiffusionService, StableDiffusionService>();
        services.AddHttpClient<IVoicevoxService, VoicevoxService>();

        // Register HTTP client for AI Assistant
        services.AddHttpClient<AiAssistantViewModel>();

        // Register ViewModels
        services.AddTransient<SimpleImageViewModel>();
        services.AddTransient<BatchImageViewModel>();
        services.AddTransient<AudioViewModel>();
        services.AddTransient<PromptBuilderViewModel>();
        services.AddTransient<AiAssistantViewModel>();
        services.AddTransient<MainViewModel>();

        // Register MainWindow
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        // Initialize database
        var dbService = Services.GetRequiredService<IDatabaseService>();
        await dbService.InitializeAsync();

        // Show main window
        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Loaded += async (s, args) => await mainViewModel.InitializeAsync();
        mainWindow.Show();
    }

    private static AppConfig LoadConfiguration()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    return config;
                }
            }
            catch
            {
                // Fall through to default config
            }
        }

        return new AppConfig();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
