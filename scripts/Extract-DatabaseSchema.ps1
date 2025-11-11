<#
.SYNOPSIS
    Extracts database schema from an existing Azure SQL database to a DACPAC file.
    
.DESCRIPTION
    Uses SqlPackage.exe to extract the complete database schema from an existing
    Azure SQL database and creates a DACPAC file that can be used to create a
    Visual Studio SQL Database Project.
    
.PARAMETER ConnectionString
    Full Azure SQL connection string (e.g., "Server=tcp:myserver.database.windows.net,1433;Database=mydb;User ID=myuser;Password=mypass;Encrypt=True;")
    
.PARAMETER ServerName
    Azure SQL server name (alternative to ConnectionString)
    
.PARAMETER DatabaseName
    Database name (required if using ServerName)
    
.PARAMETER UserName
    SQL authentication username (required if using ServerName)
    
.PARAMETER Password
    SQL authentication password (required if using ServerName)
    
.PARAMETER OutputPath
    Path where the DACPAC file will be created (default: ./output)
    
.PARAMETER DacpacFileName
    Name of the output DACPAC file (default: DatabaseName.dacpac)
    
.PARAMETER SqlPackagePath
    Path to SqlPackage.exe (if not in PATH)
    
.EXAMPLE
    .\Extract-DatabaseSchema.ps1 -ConnectionString "Server=tcp:myserver.database.windows.net,1433;Database=mydb;User ID=myuser;Password=mypass;Encrypt=True;"
    
.EXAMPLE
    .\Extract-DatabaseSchema.ps1 -ServerName "myserver.database.windows.net" -DatabaseName "mydb" -UserName "myuser" -Password "mypass"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ConnectionString,
    
    [Parameter(Mandatory = $false)]
    [string]$ServerName,
    
    [Parameter(Mandatory = $false)]
    [string]$DatabaseName,
    
    [Parameter(Mandatory = $false)]
    [string]$UserName,
    
    [Parameter(Mandatory = $false)]
    [string]$Password,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "./output",
    
    [Parameter(Mandatory = $false)]
    [string]$DacpacFileName,
    
    [Parameter(Mandatory = $false)]
    [string]$SqlPackagePath
)

# Error handling
$ErrorActionPreference = "Stop"

# Function to find SqlPackage.exe
function Find-SqlPackage {
    $possiblePaths = @(
        "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\140\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\140\DAC\bin\SqlPackage.exe"
    )
    
    # Check if provided path exists
    if ($SqlPackagePath -and (Test-Path $SqlPackagePath)) {
        return $SqlPackagePath
    }
    
    # Check common installation paths
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            Write-Host "Found SqlPackage.exe at: $path" -ForegroundColor Green
            return $path
        }
    }
    
    # Check PATH
    $sqlPackageInPath = Get-Command SqlPackage.exe -ErrorAction SilentlyContinue
    if ($sqlPackageInPath) {
        Write-Host "Found SqlPackage.exe in PATH: $($sqlPackageInPath.Source)" -ForegroundColor Green
        return $sqlPackageInPath.Source
    }
    
    throw "SqlPackage.exe not found. Please install SQL Server Data Tools (SSDT) or provide the path using -SqlPackagePath parameter."
}

# Validate parameters
if (-not $ConnectionString) {
    if (-not $ServerName -or -not $DatabaseName -or -not $UserName -or -not $Password) {
        throw "Either ConnectionString or all of (ServerName, DatabaseName, UserName, Password) must be provided."
    }
    
    # Build connection string from parameters
    $ConnectionString = "Server=tcp:$ServerName,1433;Database=$DatabaseName;User ID=$UserName;Password=$Password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

# Set default DACPAC filename if not provided
if (-not $DacpacFileName) {
    if ($DatabaseName) {
        $DacpacFileName = "$DatabaseName.dacpac"
    } else {
        # Try to extract database name from connection string
        if ($ConnectionString -match "Database=([^;]+)") {
            $DacpacFileName = "$($matches[1]).dacpac"
        } else {
            $DacpacFileName = "Database.dacpac"
        }
    }
}

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Green
}

$fullDacpacPath = Join-Path $OutputPath $DacpacFileName

# Find SqlPackage.exe
$sqlPackageExe = Find-SqlPackage

Write-Host "`n=== Database Schema Extraction ===" -ForegroundColor Cyan
Write-Host "Source: Azure SQL Database" -ForegroundColor Yellow
Write-Host "Output: $fullDacpacPath" -ForegroundColor Yellow
Write-Host "SqlPackage: $sqlPackageExe" -ForegroundColor Yellow
Write-Host ""

# Extract DACPAC
try {
    Write-Host "Extracting database schema..." -ForegroundColor Green
    
    $extractArgs = @(
        "/Action:Extract",
        "/SourceConnectionString:`"$ConnectionString`"",
        "/TargetFile:`"$fullDacpacPath`"",
        "/p:ExtractAllTableData=False",
        "/p:IgnoreExtendedProperties=False",
        "/p:VerifyExtraction=True"
    )
    
    & $sqlPackageExe $extractArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Successfully extracted database schema!" -ForegroundColor Green
        Write-Host "  DACPAC file: $fullDacpacPath" -ForegroundColor Cyan
        Write-Host "`nNext steps:" -ForegroundColor Yellow
        Write-Host "  1. Import this DACPAC into Visual Studio SQL Database Project" -ForegroundColor White
        Write-Host "  2. Or use: Import-DacpacToProject.ps1 to create project structure" -ForegroundColor White
    } else {
        throw "SqlPackage.exe exited with code $LASTEXITCODE"
    }
} catch {
    Write-Host "`n✗ Error extracting database schema: $_" -ForegroundColor Red
    throw
}

