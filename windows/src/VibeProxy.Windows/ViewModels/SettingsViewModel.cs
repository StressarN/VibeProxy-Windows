using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using VibeProxy.Windows.Models;
using VibeProxy.Windows.Services;
using VibeProxy.Windows.Utilities;

namespace VibeProxy.Windows.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly CliProxyService _cliProxyService;
    private readonly ThinkingProxyServer _thinkingProxyServer;
    private readonly AuthStatusService _authStatusService;
    private readonly LaunchAtLoginService _launchAtLoginService;
    private readonly NotificationService _notificationService;
    private readonly Dictionary<AuthProviderType, bool> _authBusy = new();

    private bool _launchAtLoginEnabled;
    private bool _thinkingProxyRunning;
    private string _serverStatusText = "Server: Stopped";

    public SettingsViewModel(
        CliProxyService cliProxyService,
        ThinkingProxyServer thinkingProxyServer,
        AuthStatusService authStatusService,
        LaunchAtLoginService launchAtLoginService,
        NotificationService notificationService)
    {
        _cliProxyService = cliProxyService;
        _thinkingProxyServer = thinkingProxyServer;
        _authStatusService = authStatusService;
        _launchAtLoginService = launchAtLoginService;
        _notificationService = notificationService;

        LogLines = new ObservableCollection<string>(_cliProxyService.GetLogs());

        _cliProxyService.StatusChanged += (_, _) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                RaisePropertyChanged(nameof(IsServerRunning));
                UpdateServerStatusText();
            });
        };

        _cliProxyService.LogsUpdated += (_, logs) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogLines.Clear();
                foreach (var line in logs)
                {
                    LogLines.Add(line);
                }
            });
        };

        _thinkingProxyServer.StatusChanged += (_, running) =>
        {
            _thinkingProxyRunning = running;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                RaisePropertyChanged(nameof(IsThinkingProxyRunning));
                UpdateServerStatusText();
            });
        };

        _authStatusService.StatusesChanged += (_, statuses) => Application.Current?.Dispatcher.Invoke(() => UpdateStatuses(statuses));

        _ = InitializeAsync();
    }

    public ObservableCollection<string> LogLines { get; }

    public bool IsServerRunning => _cliProxyService.IsRunning;

    public bool IsThinkingProxyRunning => _thinkingProxyRunning;

    public bool LaunchAtLoginEnabled
    {
        get => _launchAtLoginEnabled;
        private set => SetProperty(ref _launchAtLoginEnabled, value);
    }

    public string ServerStatusText
    {
        get => _serverStatusText;
        private set => SetProperty(ref _serverStatusText, value);
    }

    public string VersionText => $"VibeProxy {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0"}";

    public AuthStatus ClaudeStatus { get; private set; } = new(AuthProviderType.Claude);
    public AuthStatus CodexStatus { get; private set; } = new(AuthProviderType.Codex);
    public AuthStatus GeminiStatus { get; private set; } = new(AuthProviderType.Gemini);
    public AuthStatus QwenStatus { get; private set; } = new(AuthProviderType.Qwen);

    public bool IsAuthenticatingClaude => GetBusy(AuthProviderType.Claude);
    public bool IsAuthenticatingCodex => GetBusy(AuthProviderType.Codex);
    public bool IsAuthenticatingGemini => GetBusy(AuthProviderType.Gemini);
    public bool IsAuthenticatingQwen => GetBusy(AuthProviderType.Qwen);

    public async Task StartServerAsync()
    {
        await _thinkingProxyServer.StartAsync();
        var started = await _cliProxyService.StartAsync();
        if (started)
        {
            _notificationService.Show("Server Started", "VibeProxy is now running on http://localhost:8317");
        }
        UpdateServerStatusText();
    }

    public async Task StopServerAsync()
    {
        await _cliProxyService.StopAsync();
        await _thinkingProxyServer.StopAsync();
        UpdateServerStatusText();
    }

    public void CopyServerUrlToClipboard()
    {
        var url = "http://localhost:8317";
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
            {
                Clipboard.SetText(url);
            }
            else if (dispatcher.CheckAccess())
            {
                Clipboard.SetText(url);
            }
            else
            {
                dispatcher.Invoke(() => Clipboard.SetText(url));
            }
            _notificationService.Show("Copied", "Server URL copied to clipboard");
        }
        catch
        {
            // ignored
        }
    }

    public void OpenAuthFolder()
    {
        try
        {
            var directory = _authStatusService.DirectoryPath;
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignored
        }
    }

    public Task ConnectClaudeAsync() => RunAuthFlowAsync(AuthProviderType.Claude, () => _cliProxyService.RunAuthCommandAsync(AuthCommand.Claude, null));

    public Task ConnectCodexAsync() => RunAuthFlowAsync(AuthProviderType.Codex, () => _cliProxyService.RunAuthCommandAsync(AuthCommand.Codex, null));

    public Task ConnectGeminiAsync() => RunAuthFlowAsync(AuthProviderType.Gemini, () => _cliProxyService.RunAuthCommandAsync(AuthCommand.Gemini, null));

    public Task ConnectQwenAsync(string email) => RunAuthFlowAsync(AuthProviderType.Qwen, () => _cliProxyService.RunAuthCommandAsync(AuthCommand.Qwen, email));

    public Task DisconnectClaudeAsync() => DisconnectAsync(AuthProviderType.Claude);
    public Task DisconnectCodexAsync() => DisconnectAsync(AuthProviderType.Codex);
    public Task DisconnectGeminiAsync() => DisconnectAsync(AuthProviderType.Gemini);
    public Task DisconnectQwenAsync() => DisconnectAsync(AuthProviderType.Qwen);

    private async Task InitializeAsync()
    {
        var launch = await _launchAtLoginService.IsEnabledAsync().ConfigureAwait(false);
        UpdateLaunchAtLoginFlag(launch);
        await _authStatusService.RefreshAsync().ConfigureAwait(false);
        var snapshot = _authStatusService.CurrentStatuses;
        Application.Current?.Dispatcher.Invoke(() => UpdateStatuses(snapshot));
        UpdateServerStatusText();
    }

    private async Task RunAuthFlowAsync(AuthProviderType provider, Func<Task<AuthCommandResult>> action)
    {
        if (GetBusy(provider))
        {
            return;
        }

        SetBusy(provider, true);
        try
        {
            AuthCommandResult result;
            try
            {
                result = await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result = new AuthCommandResult(false, ex.Message);
            }
            _notificationService.Show(
                result.Success ? "Authentication Started" : "Authentication Failed",
                result.Message);

            if (result.Success)
            {
                await _authStatusService.RefreshAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            SetBusy(provider, false);
        }
    }

    private async Task DisconnectAsync(AuthProviderType provider)
    {
        if (GetBusy(provider))
        {
            return;
        }

        SetBusy(provider, true);
        var wasRunning = IsServerRunning;

        try
        {
            if (wasRunning)
            {
                await StopServerAsync().ConfigureAwait(false);
            }

            var deleted = await DeleteCredentialFileAsync(provider).ConfigureAwait(false);
            await _authStatusService.RefreshAsync().ConfigureAwait(false);

            _notificationService.Show(
                deleted ? "Disconnected" : "No Credentials Found",
                deleted ? $"Removed stored credentials for {provider}." : $"No credentials found for {provider}.");
        }
        finally
        {
            if (wasRunning)
            {
                await StartServerAsync().ConfigureAwait(false);
            }

            SetBusy(provider, false);
        }
    }

    private async Task<bool> DeleteCredentialFileAsync(AuthProviderType provider)
    {
        var deleted = false;
        foreach (var file in Directory.EnumerateFiles(_authStatusService.DirectoryPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("type", out var typeProperty))
                {
                    continue;
                }

                if (!Enum.TryParse<AuthProviderType>(typeProperty.GetString(), true, out var fileProvider))
                {
                    continue;
                }

                if (fileProvider == provider)
                {
                    File.Delete(file);
                    deleted = true;
                }
            }
            catch
            {
                // ignore malformed files
            }
        }

        return deleted;
    }

    private void UpdateStatuses(IReadOnlyDictionary<AuthProviderType, AuthStatus> snapshot)
    {
        if (snapshot.TryGetValue(AuthProviderType.Claude, out var claude))
        {
            ClaudeStatus = claude;
            RaisePropertyChanged(nameof(ClaudeStatus));
        }

        if (snapshot.TryGetValue(AuthProviderType.Codex, out var codex))
        {
            CodexStatus = codex;
            RaisePropertyChanged(nameof(CodexStatus));
        }

        if (snapshot.TryGetValue(AuthProviderType.Gemini, out var gemini))
        {
            GeminiStatus = gemini;
            RaisePropertyChanged(nameof(GeminiStatus));
        }

        if (snapshot.TryGetValue(AuthProviderType.Qwen, out var qwen))
        {
            QwenStatus = qwen;
            RaisePropertyChanged(nameof(QwenStatus));
        }
    }

    private void UpdateServerStatusText()
    {
        var status = IsServerRunning
            ? $"Server: Running (port {_thinkingProxyServer.ListeningPort})"
            : "Server: Stopped";
        if (Application.Current is { Dispatcher: { } dispatcher })
        {
            if (dispatcher.CheckAccess())
            {
                ServerStatusText = status;
            }
            else
            {
                dispatcher.Invoke(() => ServerStatusText = status);
            }
        }
    }

    private bool GetBusy(AuthProviderType provider)
    {
        lock (_authBusy)
        {
            return _authBusy.TryGetValue(provider, out var busy) && busy;
        }
    }

    private void SetBusy(AuthProviderType provider, bool busy)
    {
        lock (_authBusy)
        {
            _authBusy[provider] = busy;
        }
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        void Raise()
        {
            switch (provider)
            {
                case AuthProviderType.Claude:
                    RaisePropertyChanged(nameof(IsAuthenticatingClaude));
                    break;
                case AuthProviderType.Codex:
                    RaisePropertyChanged(nameof(IsAuthenticatingCodex));
                    break;
                case AuthProviderType.Gemini:
                    RaisePropertyChanged(nameof(IsAuthenticatingGemini));
                    break;
                case AuthProviderType.Qwen:
                    RaisePropertyChanged(nameof(IsAuthenticatingQwen));
                    break;
            }
        }

        if (dispatcher.CheckAccess())
        {
            Raise();
        }
        else
        {
            dispatcher.Invoke(Raise);
        }
    }

    public async Task UpdateLaunchAtLoginAsync(bool enabled)
    {
        await _launchAtLoginService.SetEnabledAsync(enabled).ConfigureAwait(false);
        var launch = await _launchAtLoginService.IsEnabledAsync().ConfigureAwait(false);
        UpdateLaunchAtLoginFlag(launch);
    }

    private void UpdateLaunchAtLoginFlag(bool value)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            LaunchAtLoginEnabled = value;
            return;
        }

        if (dispatcher.CheckAccess())
        {
            LaunchAtLoginEnabled = value;
        }
        else
        {
            dispatcher.Invoke(() => LaunchAtLoginEnabled = value);
        }
    }
}
