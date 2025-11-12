<#
.SYNOPSIS
    Runs the FileDownloadApi project.

.DESCRIPTION
    Starts the FileDownloadApi Web API using IIS Express.
    This script will:
    1. Verify the project is built
    2. Find IIS Express
    3. Start the API on the specified port
    4. Open the API in the default browser (optional)

.PARAMETER Port
    Port number for the API (default: 8080)

.PARAMETER OpenBrowser
    Open the API in the default browser after starting

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)

.EXAMPLE
    .\run.ps1
    
.EXAMPLE
    .\run.ps1 -Port 9000 -OpenBrowser
    
.EXAMPLE
    .\run.ps1 -Configuration Release
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [int]$Port = 8080,
    
    [Parameter(Mandatory = $false)]
    [switch]$OpenBrowser,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$apiPath = Join-Path $projectRoot "FileDownloadApi"
$apiDll = Join-Path $apiPath "bin\FileDownloadApi.dll"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FileDownloadApi Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify the API is built
if (-not (Test-Path $apiDll)) {
    Write-Host "[ERROR] FileDownloadApi.dll not found!" -ForegroundColor Red
    Write-Host "  Path: $apiDll" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Please build the solution first:" -ForegroundColor Yellow
    Write-Host "  .\scripts\build.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or in Visual Studio:" -ForegroundColor Yellow
    Write-Host "  Build -> Build Solution" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] FileDownloadApi.dll found" -ForegroundColor Green
Write-Host ""

# Find IIS Express
$iisExpress = $null
$iisExpressPaths = @(
    "${env:ProgramFiles}\IIS Express\iisexpress.exe",
    "${env:ProgramFiles(x86)}\IIS Express\iisexpress.exe"
)

foreach ($path in $iisExpressPaths) {
    if (Test-Path $path) {
        $iisExpress = $path
        break
    }
}

if (-not $iisExpress) {
    Write-Host "[ERROR] IIS Express not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install IIS Express:" -ForegroundColor Yellow
    Write-Host "  - Install Visual Studio (includes IIS Express)" -ForegroundColor Yellow
    Write-Host "  - Or download from: https://www.microsoft.com/en-us/download/details.aspx?id=48264" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternatively, you can run the API in Visual Studio:" -ForegroundColor Yellow
    Write-Host "  Set FileDownloadApi as startup project -> Press F5" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using IIS Express: $iisExpress" -ForegroundColor Green
Write-Host "API Path: $apiPath" -ForegroundColor Gray
Write-Host "Port: $Port" -ForegroundColor Gray
Write-Host ""

# Check if port is in use
$portInUse = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
if ($portInUse) {
    Write-Host "[WARN] Port $Port is already in use!" -ForegroundColor Yellow
    Write-Host "  You may need to stop the existing process or use a different port." -ForegroundColor Yellow
    Write-Host ""
    $response = Read-Host "Continue anyway? (y/N)"
    if ($response -ne "y" -and $response -ne "Y") {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Start IIS Express
Write-Host "Starting FileDownloadApi..." -ForegroundColor Yellow
Write-Host ""

$apiUrl = "http://localhost:$Port"

try {
    # Start IIS Express in background
    $process = Start-Process -FilePath $iisExpress -ArgumentList "/path:`"$apiPath`" /port:$Port" -PassThru -NoNewWindow
    
    # Wait a moment for IIS Express to start
    Start-Sleep -Seconds 2
    
    if ($process.HasExited) {
        Write-Host "[ERROR] IIS Express failed to start" -ForegroundColor Red
        Write-Host "  Exit code: $($process.ExitCode)" -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "[OK] FileDownloadApi is running!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "API URL: $apiUrl" -ForegroundColor Cyan
    Write-Host "Process ID: $($process.Id)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Available endpoints:" -ForegroundColor Cyan
    Write-Host "  GET  $apiUrl/api/files" -ForegroundColor Gray
    Write-Host "  GET  $apiUrl/api/files/{filename}" -ForegroundColor Gray
    Write-Host "  GET  $apiUrl/api/files/{filename}/status" -ForegroundColor Gray
    Write-Host "  POST $apiUrl/api/files/{filename}/restore" -ForegroundColor Gray
    Write-Host "  POST $apiUrl/api/files/test-connection" -ForegroundColor Gray
    Write-Host "  POST $apiUrl/api/files/list-databases" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Press Ctrl+C to stop the API" -ForegroundColor Yellow
    Write-Host ""
    
    # Open browser if requested
    if ($OpenBrowser) {
        Start-Sleep -Seconds 1
        Start-Process $apiUrl
        Write-Host "Opened API in browser" -ForegroundColor Green
        Write-Host ""
    }
    
    # Test the API
    Write-Host "Testing API connection..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    
    try {
        $response = Invoke-RestMethod -Uri "$apiUrl/api/files" -Method Get -ErrorAction Stop
        Write-Host "[OK] API is responding!" -ForegroundColor Green
        Write-Host "  Files found: $($response.count)" -ForegroundColor Gray
    } catch {
        Write-Host "[WARN] API may still be starting up..." -ForegroundColor Yellow
        Write-Host "  Try accessing $apiUrl/api/files in your browser" -ForegroundColor Gray
    }
    
    Write-Host ""
    
    # Wait for user to stop
    Write-Host "API is running. Press any key to stop..." -ForegroundColor Cyan
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    # Stop IIS Express
    Write-Host ""
    Write-Host "Stopping API..." -ForegroundColor Yellow
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] API stopped" -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "Error starting API: $_" -ForegroundColor Red
    exit 1
}

