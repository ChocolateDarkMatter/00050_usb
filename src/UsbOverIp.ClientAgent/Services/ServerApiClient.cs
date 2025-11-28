using System.Net.Http;
using System.Net.Http.Json;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;

namespace UsbOverIp.ClientAgent.Services;

/// <summary>
/// HTTP client for communicating with the USB-over-IP backend server API.
/// </summary>
public class ServerApiClient : IServerApiClient
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = string.Empty;

    public ServerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetServerUrl(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<HealthResponse> GetHealthAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HealthResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize health response");
    }

    public async Task<DeviceListResponse> GetDevicesAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/devices");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceListResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize device list response");
    }

    public async Task<UsbDevice> ShareDeviceAsync(string busId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/{busId}/share", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UsbDevice>()
            ?? throw new InvalidOperationException("Failed to deserialize device response");
    }

    public async Task<UsbDevice> UnshareDeviceAsync(string busId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/{busId}/unshare", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UsbDevice>()
            ?? throw new InvalidOperationException("Failed to deserialize device response");
    }

    public async Task<AttachResponse> AttachDeviceAsync(string busId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/{busId}/attach", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttachResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize attach response");
    }

    public async Task<AttachResponse> DetachDeviceAsync(string busId)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/{busId}/detach", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttachResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize detach response");
    }
}
