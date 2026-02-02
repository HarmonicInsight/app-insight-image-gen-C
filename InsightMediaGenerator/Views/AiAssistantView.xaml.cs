using System.Windows;
using System.Windows.Controls;
using InsightMediaGenerator.ViewModels;

namespace InsightMediaGenerator.Views;

public partial class AiAssistantView : UserControl
{
    public AiAssistantView()
    {
        InitializeComponent();
        ApiKeyBox.PasswordChanged += ApiKeyBox_PasswordChanged;
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AiAssistantViewModel vm)
        {
            vm.ApiKey = ApiKeyBox.Password;
        }
    }
}
