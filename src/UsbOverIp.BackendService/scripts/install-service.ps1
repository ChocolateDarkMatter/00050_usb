# Install USB-over-IP Backend Service
# Run this script as Administrator

param(
    [string]$ServiceName = "UsbOverIpBackendService",
    [string]$DisplayName = "USB-over-IP Backend Service",
    [string]$Description = "Backend service for sharing USB devices over network using usbipd-win"
)

$ErrorActionPreference = "Stop"

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

# Get the executable path
$exePath = Join-Path $PSScriptRoot "..\bin\Release\net8.0\UsbOverIp.BackendService.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "..\bin\Debug\net8.0\UsbOverIp.BackendService.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Error "Service executable not found. Please build the project first."
    exit 1
}

$exePath = Resolve-Path $exePath

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service '$ServiceName' already exists. Stopping and removing..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    & sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Create the service
Write-Host "Installing service '$ServiceName'..."
New-Service -Name $ServiceName `
    -BinaryPathName $exePath `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType Automatic

Write-Host "Service installed successfully!"
Write-Host ""
Write-Host "To start the service, run:"
Write-Host "  Start-Service -Name $ServiceName"
Write-Host ""
Write-Host "To check service status:"
Write-Host "  Get-Service -Name $ServiceName"
