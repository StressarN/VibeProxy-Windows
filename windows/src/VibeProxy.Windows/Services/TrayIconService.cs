using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using VibeProxy.Windows.ViewModels;

namespace VibeProxy.Windows.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly SettingsViewModel _viewModel;
    private readonly NotificationService _notificationService;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _serverStatusItem;
    private readonly ToolStripMenuItem _startStopItem;
    private readonly ToolStripMenuItem _copyUrlItem;

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? StartStopRequested;
    public event EventHandler? CopyUrlRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService(SettingsViewModel viewModel, NotificationService notificationService)
    {
        _viewModel = viewModel;
        _notificationService = notificationService;

        _notifyIcon = new NotifyIcon
        {
            Text = "VibeProxy",
            Icon = SystemIcons.Application,
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();

        _serverStatusItem = new ToolStripMenuItem("Server: Stopped") { Enabled = false };
        contextMenu.Items.Add(_serverStatusItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        var openSettingsItem = new ToolStripMenuItem("Open Settings", null, (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(openSettingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        _startStopItem = new ToolStripMenuItem("Start Server", null, (_, _) => StartStopRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(_startStopItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        _copyUrlItem = new ToolStripMenuItem("Copy Server URL", null, (_, _) => CopyUrlRequested?.Invoke(this, EventArgs.Empty))
        {
            Enabled = false
        };
        contextMenu.Items.Add(_copyUrlItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Quit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        UpdateUi();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.IsServerRunning)
            or nameof(SettingsViewModel.ServerStatusText))
        {
            UpdateUi();
        }
    }

    private void UpdateUi()
    {
        var running = _viewModel.IsServerRunning;
        _serverStatusItem.Text = _viewModel.ServerStatusText;
        _startStopItem.Text = running ? "Stop Server" : "Start Server";
        _copyUrlItem.Enabled = running;
        _notifyIcon.Icon = running ? SystemIcons.Shield : SystemIcons.Application;
    }

    public void ShowNotification(string title, string message)
    {
        _notificationService.Show(title, message);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
