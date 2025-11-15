using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CommunityToolkit.WinUI.Notifications;

namespace VibeProxy.Windows.Services;

public sealed class NotificationService : IDisposable
{
    private const string AppId = "Automaze.VibeProxy";
    private readonly NotifyIcon _fallbackIcon;

    public NotificationService()
    {
        _fallbackIcon = new NotifyIcon
        {
            Visible = false,
            Icon = System.Drawing.SystemIcons.Information
        };

        EnsureShortcut();
    }

    public void Show(string title, string message)
    {
        try
        {
            EnsureShortcut();
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show(toast => toast.ExpirationTime = DateTime.Now.AddSeconds(10));
        }
        catch
        {
            _fallbackIcon.Visible = true;
            _fallbackIcon.BalloonTipTitle = title;
            _fallbackIcon.BalloonTipText = message;
            _fallbackIcon.ShowBalloonTip(3000);
        }
    }

    private static void EnsureShortcut()
    {
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var shortcutPath = Path.Combine(startMenu, "Microsoft", "Windows", "Start Menu", "Programs", "VibeProxy.lnk");

        if (!File.Exists(shortcutPath))
        {
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
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
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

        ToastNotificationManagerCompat.OnActivated += _ => { };
        ToastNotificationManagerCompat.RegisterAumidAndComServer<AppNotificationActivator>(AppId);
        ToastNotificationManagerCompat.RegisterActivator<AppNotificationActivator>();
    }

    public void Dispose()
    {
        _fallbackIcon.Dispose();
    }
}

[ComVisible(true)]
[Guid("A6A6E8A3-C4E4-4EA9-9A39-966B1127CFC8")]
public sealed class AppNotificationActivator : ToastNotificationActivator
{
    public override void OnActivated(string arguments, NotificationUserInput userInput, string appUserModelId)
    {
        // No-op; the app only needs to acknowledge activation for now.
    }
}
