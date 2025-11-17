using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibeProxy.Windows.Services;

public sealed class NotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public NotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            Visible = false,
            Icon = System.Drawing.SystemIcons.Information
        };

        _notifyIcon.BalloonTipClosed += (_, _) => _notifyIcon.Visible = false;
        _notifyIcon.BalloonTipClicked += (_, _) => _notifyIcon.Visible = false;

        EnsureShortcut();
    }

    public void Show(string title, string message)
    {
        try
        {
            _notifyIcon.Visible = true;
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(4000);
        }
        catch
        {
            // Ignore notification failures
        }
    }

    private static void EnsureShortcut()
    {
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var shortcutPath = Path.Combine(startMenu, "Microsoft", "Windows", "Start Menu", "Programs", "VibeProxy.lnk");

        if (File.Exists(shortcutPath))
        {
            return;
        }

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return;
            }

            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            var exePath = Path.Combine(AppContext.BaseDirectory, "VibeProxy.Windows.exe");
            shortcut.TargetPath = exePath;
            var workingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.WindowStyle = 1;
            shortcut.Description = "VibeProxy";
            shortcut.Save();

            Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }
        catch
        {
            // best effort only
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
