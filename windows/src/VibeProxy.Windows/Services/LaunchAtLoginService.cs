using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace VibeProxy.Windows.Services;

public sealed class LaunchAtLoginService
{
    private const string TaskName = "VibeProxyAutoStart";
    private const string RegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string RegistryValue = "VibeProxy";

    public Task<bool> IsEnabledAsync()
    {
        try
        {
            using var taskService = new TaskService();
            var task = taskService.GetTask(TaskName);
            if (task is not null)
            {
                return Task.FromResult(true);
            }
        }
        catch
        {
            // Fall back to registry check
        }

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
        try
        {
            using var taskService = new TaskService();
            var definition = taskService.NewTask();
            definition.RegistrationInfo.Description = "Launches VibeProxy when the user signs in.";
            definition.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromSeconds(5) });

            var executablePath = Path.Combine(AppContext.BaseDirectory, "VibeProxy.Windows.exe");
            var workingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
            definition.Actions.Add(new ExecAction(executablePath, null, workingDirectory));
            definition.Principal.RunLevel = TaskRunLevel.Highest;

            taskService.RootFolder.RegisterTaskDefinition(TaskName, definition, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);
            RemoveRegistryEntry();
            return Task.CompletedTask;
        }
        catch
        {
            // Scheduler unavailable - registry fallback
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            var executablePath = Path.Combine(AppContext.BaseDirectory, "VibeProxy.Windows.exe");
            key.SetValue(RegistryValue, '"' + executablePath + '"');
            return Task.CompletedTask;
        }
    }

    private Task UnregisterAsync()
    {
        try
        {
            using var taskService = new TaskService();
            taskService.RootFolder.DeleteTask(TaskName, exceptionOnNotExists: false);
        }
        catch
        {
            // ignore
        }

        RemoveRegistryEntry();
        return Task.CompletedTask;
    }

    private static void RemoveRegistryEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        key?.DeleteValue(RegistryValue, throwOnMissingValue: false);
    }
}
