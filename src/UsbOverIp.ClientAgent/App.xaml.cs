using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UsbOverIp.ClientAgent.Services;
using UsbOverIp.ClientAgent.ViewModels;
using UsbOverIp.Shared.Contracts;

namespace UsbOverIp.ClientAgent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private AutoStartManager? _autoStartManager;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure services
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register HttpClient
        services.AddSingleton<HttpClient>();

        // Register services
        services.AddSingleton<IServerApiClient, ServerApiClient>();
        services.AddSingleton(sp => new ServerDiscoveryService(
            sp.GetRequiredService<ILogger<ServerDiscoveryService>>(),
            50052)); // Discovery port

        // Register ViewModels
        services.AddSingleton<MainViewModel>();

        // Register Windows
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Initialize AutoStartManager
        _autoStartManager = new AutoStartManager();

        // Initialize tray icon
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

        // Update auto-start menu item
        UpdateAutoStartMenuItem();

        // Show main window
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainWindow.Closing += MainWindow_Closing;
        _viewModel = _mainWindow.DataContext as MainViewModel;

        // Subscribe to status changes to update tray tooltip
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        _mainWindow.Show();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.StatusMessage) && _trayIcon != null && _viewModel != null)
        {
            _trayIcon.ToolTipText = $"USB-over-IP Client\n{_viewModel.StatusMessage}";
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        if (_mainWindow != null && _mainWindow.WindowState != WindowState.Minimized)
        {
            e.Cancel = true;
            _mainWindow.Hide();
            _trayIcon?.ShowBalloonTip("USB-over-IP Client", "Application minimized to system tray", BalloonIcon.Info);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    // Tray icon event handlers
    private void TrayIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowWindow_OnClick(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void RefreshDevices_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.RefreshCommand.Execute(null);
    }

    private void ToggleAutoStart_OnClick(object sender, RoutedEventArgs e)
    {
        if (_autoStartManager != null)
        {
            _autoStartManager.ToggleAutoStart();
            UpdateAutoStartMenuItem();
        }
    }

    private void UpdateAutoStartMenuItem()
    {
        if (_trayIcon?.ContextMenu != null && _autoStartManager != null)
        {
            var menuItem = _trayIcon.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => m.Name == "AutoStartMenuItem");

            if (menuItem != null)
            {
                menuItem.IsChecked = _autoStartManager.IsAutoStartEnabled();
            }
        }
    }

    private void Exit_OnClick(object sender, RoutedEventArgs e)
    {
        // Actually exit the application
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        _mainWindow?.Close();
        Shutdown();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }
}
