using System.Collections.ObjectModel;
using System.Windows;
using UsbOverIp.ClientAgent.Services;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;

namespace UsbOverIp.ClientAgent.ViewModels;

/// <summary>
/// Main view model for the USB-over-IP client application.
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IServerApiClient _apiClient;
    private readonly ServerDiscoveryService _discoveryService;
    private readonly LocalUsbipClient _localUsbipClient;
    private string _serverUrl = "http://localhost:50051";
    private bool _isConnected;
    private bool _isLoading;
    private string _statusMessage = "Searching for servers...";
    private ServerInfo? _serverInfo;
    private ServerInfo? _selectedServer;

    public MainViewModel(IServerApiClient apiClient, ServerDiscoveryService discoveryService, LocalUsbipClient localUsbipClient)
    {
        _apiClient = apiClient;
        _discoveryService = discoveryService;
        _localUsbipClient = localUsbipClient;

        // Initialize collections
        Devices = new ObservableCollection<UsbDevice>();
        DiscoveredServers = new ObservableCollection<ServerInfo>();

        // Subscribe to discovery events
        _discoveryService.ServerDiscovered += OnServerDiscovered;
        _discoveryService.ServerOffline += OnServerOffline;

        // Start discovery
        _ = _discoveryService.StartAsync(CancellationToken.None);

        // Initialize commands
        ConnectCommand = new AsyncRelayCommand(async _ => await ConnectAsync(), _ => !IsLoading);
        RefreshCommand = new AsyncRelayCommand(async _ => await RefreshDevicesAsync(), _ => IsConnected && !IsLoading);
        ShareDeviceCommand = new AsyncRelayCommand(async device => await ShareDeviceAsync((UsbDevice)device!), CanExecuteDeviceCommand);
        UnshareDeviceCommand = new AsyncRelayCommand(async device => await UnshareDeviceAsync((UsbDevice)device!), CanExecuteDeviceCommand);
        AttachDeviceCommand = new AsyncRelayCommand(async device => await AttachDeviceAsync((UsbDevice)device!), CanExecuteAttachCommand);
        DetachDeviceCommand = new AsyncRelayCommand(async device => await DetachDeviceAsync((UsbDevice)device!), CanExecuteDetachCommand);
        SelectServerCommand = new AsyncRelayCommand(async server => await SelectServerAsync((ServerInfo)server!), CanExecuteSelectServer);
    }

    #region Properties

    public string ServerUrl
    {
        get => _serverUrl;
        set => SetProperty(ref _serverUrl, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ((AsyncRelayCommand)RefreshCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ((AsyncRelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)RefreshCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ServerInfo? ServerInfo
    {
        get => _serverInfo;
        set => SetProperty(ref _serverInfo, value);
    }

    public ObservableCollection<UsbDevice> Devices { get; }

    public ObservableCollection<ServerInfo> DiscoveredServers { get; }

    public ServerInfo? SelectedServer
    {
        get => _selectedServer;
        set => SetProperty(ref _selectedServer, value);
    }

    #endregion

    #region Commands

    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand ShareDeviceCommand { get; }
    public AsyncRelayCommand UnshareDeviceCommand { get; }
    public AsyncRelayCommand AttachDeviceCommand { get; }
    public AsyncRelayCommand DetachDeviceCommand { get; }
    public AsyncRelayCommand SelectServerCommand { get; }

    #endregion

    #region Command Handlers

    private async Task ConnectAsync()
    {
        IsLoading = true;
        StatusMessage = "Connecting...";

        try
        {
            _apiClient.SetServerUrl(ServerUrl);
            var health = await _apiClient.GetHealthAsync();

            IsConnected = true;
            StatusMessage = $"Connected to {health.Hostname} (v{health.Version})";

            await RefreshDevicesAsync();
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Connection failed: {ex.Message}";
            MessageBox.Show($"Failed to connect to server:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshDevicesAsync()
    {
        if (!IsConnected)
            return;

        IsLoading = true;
        StatusMessage = "Refreshing devices...";

        try
        {
            var response = await _apiClient.GetDevicesAsync();

            Devices.Clear();
            foreach (var device in response.Devices)
            {
                Devices.Add(device);
            }

            ServerInfo = response.ServerInfo;
            StatusMessage = $"Found {Devices.Count} device(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refresh failed: {ex.Message}";
            MessageBox.Show($"Failed to refresh devices:\n{ex.Message}", "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShareDeviceAsync(UsbDevice device)
    {
        IsLoading = true;
        StatusMessage = $"Sharing device {device.BusId}...";

        try
        {
            var updatedDevice = await _apiClient.ShareDeviceAsync(device.BusId);
            UpdateDeviceInList(updatedDevice);
            StatusMessage = $"Device {device.BusId} shared successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Share failed: {ex.Message}";
            MessageBox.Show($"Failed to share device:\n{ex.Message}", "Share Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UnshareDeviceAsync(UsbDevice device)
    {
        IsLoading = true;
        StatusMessage = $"Unsharing device {device.BusId}...";

        try
        {
            var updatedDevice = await _apiClient.UnshareDeviceAsync(device.BusId);
            UpdateDeviceInList(updatedDevice);
            StatusMessage = $"Device {device.BusId} unshared successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unshare failed: {ex.Message}";
            MessageBox.Show($"Failed to unshare device:\n{ex.Message}", "Unshare Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AttachDeviceAsync(UsbDevice device)
    {
        IsLoading = true;
        StatusMessage = $"Attaching device {device.BusId}...";

        try
        {
            // First, validate with the server that the device can be attached
            var response = await _apiClient.AttachDeviceAsync(device.BusId);
            if (!response.Success)
            {
                StatusMessage = $"Attach validation failed: {response.Message}";
                MessageBox.Show(response.Message, "Attach Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get server IP from the current server URL
            var serverIp = ExtractServerIp(ServerUrl);
            if (string.IsNullOrEmpty(serverIp))
            {
                throw new InvalidOperationException("Unable to determine server IP address");
            }

            // Perform the actual attachment locally using usbip client
            await _localUsbipClient.AttachDeviceAsync(serverIp, device.BusId);

            // Refresh device list to show updated state
            await RefreshDevicesAsync();

            StatusMessage = $"Device {device.BusId} attached successfully";
            MessageBox.Show($"Device {device.BusId} attached successfully", "Attach Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Attach failed: {ex.Message}";
            MessageBox.Show($"Failed to attach device:\n{ex.Message}", "Attach Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DetachDeviceAsync(UsbDevice device)
    {
        IsLoading = true;
        StatusMessage = $"Detaching device {device.BusId}...";

        try
        {
            // Perform the actual detachment locally using usbip client
            await _localUsbipClient.DetachDeviceAsync(device.BusId);

            // Refresh device list to show updated state
            await RefreshDevicesAsync();

            StatusMessage = $"Device {device.BusId} detached successfully";
            MessageBox.Show($"Device {device.BusId} detached successfully", "Detach Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Detach failed: {ex.Message}";
            MessageBox.Show($"Failed to detach device:\n{ex.Message}", "Detach Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Command CanExecute

    private bool CanExecuteDeviceCommand(object? parameter)
    {
        return !IsLoading && parameter is UsbDevice;
    }

    private bool CanExecuteAttachCommand(object? parameter)
    {
        if (!CanExecuteDeviceCommand(parameter))
            return false;

        var device = (UsbDevice)parameter!;
        return device.IsShared && !device.IsAttached;
    }

    private bool CanExecuteDetachCommand(object? parameter)
    {
        if (!CanExecuteDeviceCommand(parameter))
            return false;

        var device = (UsbDevice)parameter!;
        return device.IsAttached;
    }

    #endregion

    #region Discovery Event Handlers

    private void OnServerDiscovered(object? sender, ServerInfo server)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!DiscoveredServers.Any(s => s.Id == server.Id))
            {
                DiscoveredServers.Add(server);
                StatusMessage = $"Discovered server: {server.Hostname} ({server.IpAddress})";
            }
        });
    }

    private void OnServerOffline(object? sender, ServerInfo server)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var existingServer = DiscoveredServers.FirstOrDefault(s => s.Id == server.Id);
            if (existingServer != null)
            {
                existingServer.IsOnline = false;
                StatusMessage = $"Server offline: {server.Hostname}";

                if (_selectedServer?.Id == server.Id)
                {
                    IsConnected = false;
                }
            }
        });
    }

    private async Task SelectServerAsync(ServerInfo server)
    {
        if (!server.IsOnline)
        {
            MessageBox.Show($"Server {server.Hostname} is offline.", "Server Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedServer = server;
        ServerUrl = $"http://{server.IpAddress}:{server.ApiPort}";
        await ConnectAsync();
    }

    private bool CanExecuteSelectServer(object? parameter)
    {
        return !IsLoading && parameter is ServerInfo;
    }

    #endregion

    #region Helper Methods

    private void UpdateDeviceInList(UsbDevice updatedDevice)
    {
        var existingDevice = Devices.FirstOrDefault(d => d.BusId == updatedDevice.BusId);
        if (existingDevice != null)
        {
            var index = Devices.IndexOf(existingDevice);
            Devices[index] = updatedDevice;
        }
    }

    private string? ExtractServerIp(string serverUrl)
    {
        try
        {
            var uri = new Uri(serverUrl);
            return uri.Host;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        _discoveryService.ServerDiscovered -= OnServerDiscovered;
        _discoveryService.ServerOffline -= OnServerOffline;
        _ = _discoveryService.StopAsync(CancellationToken.None);
        _discoveryService.Dispose();
    }

    #endregion
}
