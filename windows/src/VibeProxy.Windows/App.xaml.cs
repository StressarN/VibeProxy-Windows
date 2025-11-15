using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VibeProxy.Windows.Services;
using VibeProxy.Windows.ViewModels;

namespace VibeProxy.Windows;

public partial class App : Application
{
    private TrayIconService? _trayIcon;
    private CliProxyService? _cliProxy;
    private ThinkingProxyServer? _thinkingProxy;
    private AuthStatusService? _authStatusService;
    private NotificationService? _notificationService;
    private LaunchAtLoginService? _launchAtLoginService;
    private SettingsViewModel? _viewModel;
    private MainWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _notificationService = new NotificationService();
        _launchAtLoginService = new LaunchAtLoginService();

        var resourceDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");
        var authDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cli-proxy-api");

        Directory.CreateDirectory(authDirectory);

        _cliProxy = new CliProxyService(resourceDirectory);
        _thinkingProxy = new ThinkingProxyServer();
        _authStatusService = new AuthStatusService(authDirectory);

        _viewModel = new SettingsViewModel(
            _cliProxy,
            _thinkingProxy,
            _authStatusService,
            _launchAtLoginService,
            _notificationService);

        _trayIcon = new TrayIconService(_viewModel, _notificationService);
        _trayIcon.OpenSettingsRequested += (_, _) => ShowSettingsWindow();
        _trayIcon.StartStopRequested += async (_, _) => await ToggleServerAsync();
        _trayIcon.CopyUrlRequested += (_, _) => _viewModel?.CopyServerUrlToClipboard();
        _trayIcon.ExitRequested += (_, _) => ShutdownApp();

        _ = _authStatusService.StartAsync();
        _ = _thinkingProxy.StartAsync();
        _ = _cliProxy.StartAsync();

        ShowSettingsWindow();
    }

    private void ShowSettingsWindow()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (_settingsWindow is null)
        {
            _settingsWindow = new MainWindow
            {
                DataContext = _viewModel
            };
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        if (!_settingsWindow.IsVisible)
        {
            _settingsWindow.Show();
        }

        _settingsWindow.Activate();
    }

    private async Task ToggleServerAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (_viewModel.IsServerRunning)
        {
            await _viewModel.StopServerAsync();
        }
        else
        {
            await _viewModel.StartServerAsync();
        }
    }

    private void ShutdownApp()
    {
        _ = ShutdownAsync();
    }

    private async Task ShutdownAsync()
    {
        _settingsWindow?.Close();

        if (_viewModel is not null)
        {
            await _viewModel.StopServerAsync();
        }

        _trayIcon?.Dispose();
        _thinkingProxy?.Dispose();
        _cliProxy?.Dispose();
        _authStatusService?.Dispose();

        Shutdown();
    }
}
