<#
.SYNOPSIS
    Extracts database schema using configuration from config.ps1
    
.DESCRIPTION
    Convenience script that loads connection strings from config.ps1 and extracts
    the database schema for the specified environment.
    
.PARAMETER Environment
    Environment name (Dev, CI, Staging, Production)
    
.PARAMETER ConfigPath
    Path to config.ps1 file (default: ./scripts/config.ps1)
    
.PARAMETER OutputPath
    Path where the DACPAC file will be created (default: ./output)
    
.EXAMPLE
    .\Extract-DatabaseSchema-FromConfig.ps1 -Environment Dev
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Dev", "CI", "Staging", "Production", "Sandbox")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = "./scripts/config.ps1",
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "./output"
)

$ErrorActionPreference = "Stop"

# Load configuration
if (-not (Test-Path $ConfigPath)) {
    Write-Host "Config file not found: $ConfigPath" -ForegroundColor Red
    Write-Host "Please copy scripts/config.example.ps1 to scripts/config.ps1 and update with your connection strings." -ForegroundColor Yellow
    exit 1
}

. $ConfigPath

# Get connection string for environment
$connectionString = $null
if ($Script:DatabaseConnections -and $Script:DatabaseConnections.ContainsKey($Environment)) {
    $connectionString = $Script:DatabaseConnections[$Environment]
} elseif ($Script:DatabaseConfig -and $Script:DatabaseConfig.ContainsKey($Environment)) {
    $config = $Script:DatabaseConfig[$Environment]
    $connectionString = "Server=tcp:$($config.ServerName),1433;Database=$($config.DatabaseName);User ID=$($config.UserName);Password=$($config.Password);Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
} else {
    Write-Host "Environment '$Environment' not found in configuration." -ForegroundColor Red
    exit 1
}

# Call the main extraction script
$scriptPath = Join-Path $PSScriptRoot "Extract-DatabaseSchema.ps1"
& $scriptPath -ConnectionString $connectionString -OutputPath $OutputPath -DacpacFileName "$Environment.dacpac"

