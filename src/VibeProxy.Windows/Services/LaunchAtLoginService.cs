using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VibeProxy.Windows.Services;

public sealed class LaunchAtLoginService
{
    private const string RegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string RegistryValue = "VibeProxy";

    public Task<bool> IsEnabledAsync()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
        var exists = key?.GetValue(RegistryValue) is string;
        return Task.FromResult(exists);
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        if (enabled)
        {
            await RegisterAsync();
        }
        else
        {
            await UnregisterAsync();
        }
    }

    private Task RegisterAsync()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        var executablePath = Path.Combine(AppContext.BaseDirectory, "VibeProxy.Windows.exe");
        key.SetValue(RegistryValue, '"' + executablePath + '"');
        return Task.CompletedTask;
    }

    private Task UnregisterAsync()
    {
        RemoveRegistryEntry();
        return Task.CompletedTask;
    }

    private static void RemoveRegistryEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        key?.DeleteValue(RegistryValue, throwOnMissingValue: false);
    }
}
