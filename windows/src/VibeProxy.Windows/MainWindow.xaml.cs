using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VibeProxy.Windows.Models;
using VibeProxy.Windows.ViewModels;

namespace VibeProxy.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    private async void OnToggleServer(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsServerRunning)
        {
            await ViewModel.StopServerAsync();
        }
        else
        {
            await ViewModel.StartServerAsync();
        }
    }

    private async void OnLaunchAtLoginClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox box)
        {
            await ViewModel.UpdateLaunchAtLoginAsync(box.IsChecked == true);
        }
    }

    private void OnOpenAuthFolder(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenAuthFolder();
    }

    private async void OnConnectClaude(object sender, RoutedEventArgs e) => await ViewModel.ConnectClaudeAsync();
    private async void OnDisconnectClaude(object sender, RoutedEventArgs e) => await ViewModel.DisconnectClaudeAsync();
    private async void OnConnectCodex(object sender, RoutedEventArgs e) => await ViewModel.ConnectCodexAsync();
    private async void OnDisconnectCodex(object sender, RoutedEventArgs e) => await ViewModel.DisconnectCodexAsync();
    private async void OnConnectGemini(object sender, RoutedEventArgs e) => await ViewModel.ConnectGeminiAsync();
    private async void OnDisconnectGemini(object sender, RoutedEventArgs e) => await ViewModel.DisconnectGeminiAsync();

    private async void OnConnectQwen(object sender, RoutedEventArgs e)
    {
        var dialog = new QwenEmailDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await ViewModel.ConnectQwenAsync(dialog.Email);
        }
    }

    private async void OnDisconnectQwen(object sender, RoutedEventArgs e) => await ViewModel.DisconnectQwenAsync();
}
