using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VibeProxy.Windows.Utilities;

namespace VibeProxy.Windows.Services;

public sealed class CliProxyService : IDisposable
{
    private const int Port = 8318;
    private readonly string _resourceDirectory;
    private readonly string _binaryPath;
    private readonly string _configPath;
    private readonly RingBuffer<string> _logBuffer = new(1000);
    private readonly object _syncRoot = new();
    private Process? _process;
    private bool _disposed;

    public CliProxyService(string resourceDirectory)
    {
        _resourceDirectory = resourceDirectory;
        _binaryPath = Path.Combine(resourceDirectory, "cli-proxy-api.exe");
        _configPath = Path.Combine(resourceDirectory, "config.yaml");
    }

    public event EventHandler<bool>? StatusChanged;

    public event EventHandler<IReadOnlyList<string>>? LogsUpdated;

    public bool IsRunning { get; private set; }

    public int BackendPort => Port;

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return true;
        }

        lock (_syncRoot)
        {
            if (IsRunning)
            {
                return true;
            }

            EnsureResources();
            KillOrphanedProcesses();

            var psi = new ProcessStartInfo
            {
                FileName = _binaryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("--config");
            psi.ArgumentList.Add(_configPath);

            _process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            _process.OutputDataReceived += OnProcessOutput;
            _process.ErrorDataReceived += OnProcessError;
            _process.Exited += (_, _) => HandleProcessExit();

            if (!_process.Start())
            {
                AppendLog("Failed to start cli-proxy-api process");
                return false;
            }

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            IsRunning = true;
            StatusChanged?.Invoke(this, true);
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            return _process is { HasExited: false };
        }
        catch (TaskCanceledException)
        {
            return _process is { HasExited: false };
        }
    }

    public Task StopAsync()
    {
        lock (_syncRoot)
        {
            if (_process is null)
            {
                IsRunning = false;
                StatusChanged?.Invoke(this, false);
                return Task.CompletedTask;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.CloseMainWindow();
                    if (!_process.WaitForExit(500))
                    {
                        _process.Kill(true);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
            finally
            {
                _process.Dispose();
                _process = null;
                IsRunning = false;
                StatusChanged?.Invoke(this, false);
            }
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetLogs() => _logBuffer.Snapshot();

    public async Task<AuthCommandResult> RunAuthCommandAsync(AuthCommand command, string? qwenEmail, CancellationToken cancellationToken = default)
    {
        EnsureResources();

        var psi = new ProcessStartInfo
        {
            FileName = _binaryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("--config");
        psi.ArgumentList.Add(_configPath);

        switch (command)
        {
            case AuthCommand.Claude:
                psi.ArgumentList.Add("-claude-login");
                break;
            case AuthCommand.Codex:
                psi.ArgumentList.Add("-codex-login");
                break;
            case AuthCommand.Gemini:
                psi.ArgumentList.Add("-login");
                break;
            case AuthCommand.Qwen:
                psi.ArgumentList.Add("-qwen-login");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is { Length: > 0 })
            {
                lock (outputBuilder)
                {
                    outputBuilder.AppendLine(args.Data);
                }
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is { Length: > 0 })
            {
                lock (errorBuilder)
                {
                    errorBuilder.AppendLine(args.Data);
                }
            }
        };

        if (!process.Start())
        {
            return new AuthCommandResult(false, "Failed to launch authentication helper");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _ = Task.Run(async () =>
        {
            if (command == AuthCommand.Gemini)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
                if (!process.HasExited)
                {
                    await process.StandardInput.WriteLineAsync(string.Empty);
                }
            }
            else if (command == AuthCommand.Qwen && !string.IsNullOrWhiteSpace(qwenEmail))
            {
                await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);
                if (!process.HasExited)
                {
                    await process.StandardInput.WriteLineAsync(qwenEmail);
                }
            }
        });

        await Task.Run(() => process.WaitForExit(), cancellationToken);

        var output = outputBuilder.ToString().Trim();
        var errors = errorBuilder.ToString().Trim();

        var success = process.ExitCode == 0;
        var message = success
            ? (output.Length > 0 ? output : "Authentication completed. Please finish the flow in your browser.")
            : (errors.Length > 0 ? errors : "Authentication failed.");

        if (success)
        {
            AppendLog($"Auth flow for {command} completed successfully");
        }
        else
        {
            AppendLog($"Auth flow for {command} failed: {message}");
        }

        return new AuthCommandResult(success, message);
    }

    private void EnsureResources()
    {
        if (!File.Exists(_binaryPath))
        {
            throw new FileNotFoundException("cli-proxy-api.exe is missing. Run scripts/fetch-cliproxy.ps1 before building.", _binaryPath);
        }

        if (!File.Exists(_configPath))
        {
            throw new FileNotFoundException("config.yaml is missing from the Resources folder.", _configPath);
        }
    }

    private void KillOrphanedProcesses()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("cli-proxy-api"))
            {
                if (process.Id == _process?.Id)
                {
                    continue;
                }

                process.Kill(true);
                process.Dispose();
            }
        }
        catch
        {
            // best effort cleanup
        }
    }

    private void HandleProcessExit()
    {
        lock (_syncRoot)
        {
            IsRunning = false;
            StatusChanged?.Invoke(this, false);

            _process?.Dispose();
            _process = null;
        }

        AppendLog("cli-proxy-api stopped");
    }

    private void OnProcessOutput(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            AppendLog(e.Data);
        }
    }

    private void OnProcessError(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            AppendLog($"⚠️ {e.Data}");
        }
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss");
        _logBuffer.Add($"[{timestamp}] {message}");
        LogsUpdated?.Invoke(this, _logBuffer.Snapshot());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _process?.Kill(true);
        _process?.Dispose();
    }
}
