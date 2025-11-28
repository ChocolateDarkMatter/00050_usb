# Uninstall USB-over-IP Backend Service
# Run this script as Administrator

param(
    [string]$ServiceName = "UsbOverIpBackendService"
)

$ErrorActionPreference = "Stop"

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

# Check if service exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $existingService) {
    Write-Warning "Service '$ServiceName' does not exist."
    exit 0
}

# Stop the service if running
Write-Host "Stopping service '$ServiceName'..."
Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Remove the service
Write-Host "Uninstalling service '$ServiceName'..."
& sc.exe delete $ServiceName

Write-Host "Service uninstalled successfully!"
