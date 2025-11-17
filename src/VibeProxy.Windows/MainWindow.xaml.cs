using System.Threading.Tasks;
using System.Windows;
using ButtonControl = System.Windows.Controls.Button;
using CheckBoxControl = System.Windows.Controls.CheckBox;
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
        if (sender is CheckBoxControl box)
        {
            await ViewModel.UpdateLaunchAtLoginAsync(box.IsChecked == true);
        }
    }

    private void OnOpenAuthFolder(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenAuthFolder();
    }

    private void OnCopyServerUrl(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyServerUrlToClipboard();
    }

    private async void OnConnectService(object sender, RoutedEventArgs e)
    {
        if (sender is not ButtonControl { Tag: AuthProviderType provider })
        {
            return;
        }

        if (provider == AuthProviderType.Qwen)
        {
            var dialog = new QwenEmailDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await ViewModel.ConnectQwenAsync(dialog.Email);
            }
            return;
        }

        await ConnectServiceAsync(provider);
    }

    private async void OnDisconnectService(object sender, RoutedEventArgs e)
    {
        if (sender is ButtonControl { Tag: AuthProviderType provider })
        {
            await DisconnectServiceAsync(provider);
        }
    }

    private Task ConnectServiceAsync(AuthProviderType provider) => provider switch
    {
        AuthProviderType.Claude => ViewModel.ConnectClaudeAsync(),
        AuthProviderType.Codex => ViewModel.ConnectCodexAsync(),
        AuthProviderType.Gemini => ViewModel.ConnectGeminiAsync(),
        _ => Task.CompletedTask
    };

    private Task DisconnectServiceAsync(AuthProviderType provider) => provider switch
    {
        AuthProviderType.Claude => ViewModel.DisconnectClaudeAsync(),
        AuthProviderType.Codex => ViewModel.DisconnectCodexAsync(),
        AuthProviderType.Gemini => ViewModel.DisconnectGeminiAsync(),
        AuthProviderType.Qwen => ViewModel.DisconnectQwenAsync(),
        _ => Task.CompletedTask
    };
}
