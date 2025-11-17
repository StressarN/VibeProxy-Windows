using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VibeProxy.Windows.Models;

namespace VibeProxy.Windows.Services;

public sealed class AuthStatusService : IDisposable
{
    private readonly string _directory;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyDictionary<AuthProviderType, AuthStatus> _current = CreateEmptySnapshot();
    private bool _disposed;

    public AuthStatusService(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);

        _watcher = new FileSystemWatcher(_directory, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            IncludeSubdirectories = false,
            EnableRaisingEvents = false
        };

        _watcher.Changed += (_, _) => _ = RefreshAsync();
        _watcher.Created += (_, _) => _ = RefreshAsync();
        _watcher.Deleted += (_, _) => _ = RefreshAsync();
        _watcher.Renamed += (_, _) => _ = RefreshAsync();
    }

    public event EventHandler<IReadOnlyDictionary<AuthProviderType, AuthStatus>>? StatusesChanged;

    public Task StartAsync()
    {
        _watcher.EnableRaisingEvents = true;
        return RefreshAsync();
    }

    public IReadOnlyDictionary<AuthProviderType, AuthStatus> CurrentStatuses => _current;

    public string DirectoryPath => _directory;

    public async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var snapshot = new Dictionary<AuthProviderType, AuthStatus>
            {
                [AuthProviderType.Claude] = new(AuthProviderType.Claude),
                [AuthProviderType.Codex] = new(AuthProviderType.Codex),
                [AuthProviderType.Gemini] = new(AuthProviderType.Gemini),
                [AuthProviderType.Qwen] = new(AuthProviderType.Qwen)
            };

            foreach (var file in Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                    if (!document.RootElement.TryGetProperty("type", out var typeProperty))
                    {
                        continue;
                    }

                    if (!Enum.TryParse<AuthProviderType>(typeProperty.GetString(), ignoreCase: true, out var provider))
                    {
                        continue;
                    }

                    var email = document.RootElement.TryGetProperty("email", out var emailProperty)
                        ? emailProperty.GetString()
                        : null;

                    DateTimeOffset? expires = null;
                    if (document.RootElement.TryGetProperty("expired", out var expiresProperty))
                    {
                        var raw = expiresProperty.GetString();
                        if (!string.IsNullOrWhiteSpace(raw)
                            && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                        {
                            expires = parsed;
                        }
                    }

                    snapshot[provider] = new AuthStatus(provider, true, email, expires);
                }
                catch (Exception ex) when (ex is IOException or JsonException)
                {
                    // Skip malformed entries but continue processing others
                }
            }

            _current = snapshot;
            StatusesChanged?.Invoke(this, _current);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watcher.Dispose();
        _refreshLock.Dispose();
    }

    private static IReadOnlyDictionary<AuthProviderType, AuthStatus> CreateEmptySnapshot()
    {
        return new Dictionary<AuthProviderType, AuthStatus>
        {
            [AuthProviderType.Claude] = new(AuthProviderType.Claude),
            [AuthProviderType.Codex] = new(AuthProviderType.Codex),
            [AuthProviderType.Gemini] = new(AuthProviderType.Gemini),
            [AuthProviderType.Qwen] = new(AuthProviderType.Qwen)
        };
    }
}
