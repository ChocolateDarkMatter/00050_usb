# Start USB-over-IP Backend Service
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
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Error "Service '$ServiceName' does not exist. Please install it first."
    exit 1
}

Write-Host "Starting service '$ServiceName'..."
Start-Service -Name $ServiceName

Start-Sleep -Seconds 2

$service = Get-Service -Name $ServiceName
Write-Host "Service status: $($service.Status)"

if ($service.Status -eq "Running") {
    Write-Host "Service started successfully!" -ForegroundColor Green
} else {
    Write-Warning "Service is not running. Check logs for details."
}
