using Microsoft.Win32;
using System.Reflection;

namespace UsbOverIp.ClientAgent.Services;

/// <summary>
/// Manages Windows startup registry entries for auto-start functionality.
/// </summary>
public class AutoStartManager
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "UsbOverIpClient";

    /// <summary>
    /// Checks if the application is configured to start with Windows.
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enables auto-start by adding registry entry.
    /// </summary>
    public bool EnableAutoStart()
    {
        try
        {
            var exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Disables auto-start by removing registry entry.
    /// </summary>
    public bool DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Toggles auto-start on/off.
    /// </summary>
    public bool ToggleAutoStart()
    {
        if (IsAutoStartEnabled())
        {
            return DisableAutoStart();
        }
        else
        {
            return EnableAutoStart();
        }
    }
}
