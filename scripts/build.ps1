<#
.SYNOPSIS
    Builds all projects in the solution.

.DESCRIPTION
    Builds all projects in the DatabaseExtractor solution.
    This script will:
    1. Find and use MSBuild to build the solution
    2. Build all projects in the correct order (respecting dependencies)
    3. Display build progress and results

.PARAMETER SolutionPath
    Path to the solution file (default: ..\DatabaseExtractor.sln)

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)

.PARAMETER Clean
    Clean the solution before building

.EXAMPLE
    .\build.ps1
    
.EXAMPLE
    .\build.ps1 -Configuration Release
    
.EXAMPLE
    .\build.ps1 -Clean
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$SolutionPath = "..\DatabaseExtractor.sln",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(Mandatory = $false)]
    [switch]$Clean
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
Write-Host "Solution Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Solution: $solutionPath" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Find MSBuild
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

if (-not $msbuild) {
    Write-Host "[ERROR] MSBuild not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Visual Studio 2017+ or add MSBuild to your PATH." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "You can also build manually in Visual Studio:" -ForegroundColor Yellow
    Write-Host "  Build -> Build Solution (or press Ctrl+Shift+B)" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Green
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    try {
        & $msbuild $solutionPath /t:Clean /p:Configuration=$Configuration /v:minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Warning: Clean failed, but continuing with build..." -ForegroundColor Yellow
        } else {
            Write-Host "[OK] Clean completed" -ForegroundColor Green
        }
        Write-Host ""
    } catch {
        Write-Host "Warning: Clean error: $_" -ForegroundColor Yellow
        Write-Host ""
    }
}

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
Write-Host ""

try {
    & $msbuild $solutionPath /t:Build /p:Configuration=$Configuration /v:minimal /m
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "[OK] Build completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        
        # Display output locations
        Write-Host "Build outputs:" -ForegroundColor Cyan
        $projects = @(
            @{Name="DatabaseExtractor"; Path="..\DatabaseExtractor\bin\$Configuration\DatabaseExtractor.dll"},
            @{Name="ExampleConsoleApp"; Path="..\ExampleConsoleApp\bin\$Configuration\ExampleConsoleApp.exe"},
            @{Name="FileDownloadApi"; Path="..\FileDownloadApi\bin\FileDownloadApi.dll"},
            @{Name="McpServer"; Path="..\McpServer\bin\$Configuration\McpServer.exe"}
        )
        
        foreach ($project in $projects) {
            $outputPath = Join-Path $scriptDir $project.Path
            if (Test-Path $outputPath) {
                $fileInfo = Get-Item $outputPath
                Write-Host "  [OK] $($project.Name): $($fileInfo.FullName)" -ForegroundColor Gray
            } else {
                Write-Host "  [WARN] $($project.Name): Not found" -ForegroundColor Yellow
            }
        }
        
        exit 0
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "[ERROR] Build failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "Error running MSBuild: $_" -ForegroundColor Red
    exit 1
}

