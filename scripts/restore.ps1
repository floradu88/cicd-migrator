<#
.SYNOPSIS
    Restores NuGet packages for all projects in the solution.

.DESCRIPTION
    Restores NuGet packages for all projects in the DatabaseExtractor solution.
    This script will:
    1. Find and use NuGet.exe or MSBuild to restore packages
    2. Restore packages for all projects (DatabaseExtractor, ExampleConsoleApp, FileDownloadApi, McpServer)
    3. Display progress and results

.PARAMETER SolutionPath
    Path to the solution file (default: ..\DatabaseExtractor.sln)

.EXAMPLE
    .\restore.ps1
    
.EXAMPLE
    .\restore.ps1 -SolutionPath "..\DatabaseExtractor.sln"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$SolutionPath = "..\DatabaseExtractor.sln"
)

$ErrorActionPreference = "Stop"

# Get script directory and resolve solution path
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Resolve-Path (Join-Path $scriptDir $SolutionPath) -ErrorAction SilentlyContinue

if (-not $solutionPath) {
    Write-Host "Solution file not found: $SolutionPath" -ForegroundColor Red
    Write-Host "Please ensure you're running from the scripts folder and the solution exists." -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "NuGet Package Restoration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Solution: $solutionPath" -ForegroundColor Gray
Write-Host ""

# Try to find MSBuild (preferred method for .NET Framework projects)
$msbuild = $null
$msbuildPaths = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
)

foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuild = $path
        break
    }
}

# Try to find NuGet.exe
$nuget = $null
$nugetPaths = @(
    "${env:LOCALAPPDATA}\Microsoft\Windows\PowerShell\Scripts\nuget.exe",
    "${env:ProgramFiles}\NuGet\nuget.exe",
    "${env:ProgramFiles(x86)}\NuGet\nuget.exe",
    ".\nuget.exe"
)

foreach ($path in $nugetPaths) {
    if (Test-Path $path) {
        $nuget = $path
        break
    }
}

# Method 1: Use MSBuild to restore (preferred)
if ($msbuild) {
    Write-Host "Using MSBuild to restore packages..." -ForegroundColor Green
    Write-Host "MSBuild: $msbuild" -ForegroundColor Gray
    Write-Host ""
    
    try {
        & $msbuild $solutionPath /t:Restore /p:Configuration=Debug /v:minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "[OK] Packages restored successfully!" -ForegroundColor Green
            exit 0
        } else {
            Write-Host ""
            Write-Host "[ERROR] Package restoration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    } catch {
        Write-Host "Error running MSBuild: $_" -ForegroundColor Red
        exit 1
    }
}
# Method 2: Use NuGet.exe
elseif ($nuget) {
    Write-Host "Using NuGet.exe to restore packages..." -ForegroundColor Green
    Write-Host "NuGet: $nuget" -ForegroundColor Gray
    Write-Host ""
    
    try {
        # Restore packages for each project
        $projects = @(
            "..\DatabaseExtractor\DatabaseExtractor.csproj",
            "..\ExampleConsoleApp\ExampleConsoleApp.csproj",
            "..\FileDownloadApi\FileDownloadApi.csproj",
            "..\McpServer\McpServer.csproj"
        )
        
        foreach ($project in $projects) {
            $projectPath = Join-Path $scriptDir $project
            if (Test-Path $projectPath) {
                Write-Host "Restoring packages for: $project" -ForegroundColor Yellow
                & $nuget restore $projectPath
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "Failed to restore packages for: $project" -ForegroundColor Red
                    exit $LASTEXITCODE
                }
            }
        }
        
        Write-Host ""
        Write-Host "[OK] Packages restored successfully!" -ForegroundColor Green
        exit 0
    } catch {
        Write-Host "Error running NuGet: $_" -ForegroundColor Red
        exit 1
    }
}
# Method 3: Try dotnet restore (if available)
elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "Using dotnet CLI to restore packages..." -ForegroundColor Green
    Write-Host ""
    
    try {
        Push-Location (Split-Path $solutionPath -Parent)
        dotnet restore $solutionPath
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "[OK] Packages restored successfully!" -ForegroundColor Green
            Pop-Location
            exit 0
        } else {
            Write-Host ""
            Write-Host "[ERROR] Package restoration failed" -ForegroundColor Red
            Pop-Location
            exit $LASTEXITCODE
        }
    } catch {
        Write-Host "Error running dotnet restore: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
}
else {
    Write-Host "[ERROR] Could not find MSBuild, NuGet.exe, or dotnet CLI" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install one of the following:" -ForegroundColor Yellow
    Write-Host "  - Visual Studio 2017+ (includes MSBuild)" -ForegroundColor Yellow
    Write-Host "  - NuGet.exe (download from https://www.nuget.org/downloads)" -ForegroundColor Yellow
    Write-Host "  - .NET SDK (includes dotnet CLI)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Or restore packages manually in Visual Studio:" -ForegroundColor Yellow
    Write-Host "  Right-click solution -> Restore NuGet Packages" -ForegroundColor Yellow
    exit 1
}

