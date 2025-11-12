<#
.SYNOPSIS
    Builds a Visual Studio Database Project from a solution and generates DACPAC files.

.DESCRIPTION
    This script builds SQL Server Database Projects (.sqlproj) from a Visual Studio solution
    and generates DACPAC files. It supports VS 2019 and later versions.

.PARAMETER SolutionPath
    Path to the Visual Studio solution file (.sln)

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.PARAMETER OutputPath
    Optional output directory for DACPAC files. If not specified, uses project's bin folder.

.PARAMETER ProjectName
    Optional specific project name to build. If not specified, builds all database projects.

.EXAMPLE
    .\Build-DatabaseProject.ps1 -SolutionPath "C:\Projects\MyDatabase.sln"
    
.EXAMPLE
    .\Build-DatabaseProject.ps1 -SolutionPath "C:\Projects\MyDatabase.sln" -Configuration Debug -OutputPath "C:\Dacpacs"
    
.EXAMPLE
    .\Build-DatabaseProject.ps1 -SolutionPath "C:\Projects\MyDatabase.sln" -ProjectName "MyDatabase"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$SolutionPath,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectName = ""
)

# Function to find MSBuild
function Find-MSBuild {
    $msbuildPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    )
    
    foreach ($path in $msbuildPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    # Try to find via vswhere if available
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
        if ($msbuildPath -and (Test-Path $msbuildPath)) {
            return $msbuildPath
        }
    }
    
    throw "MSBuild not found. Please install Visual Studio 2019 or later."
}

# Function to find database projects in solution
function Get-DatabaseProjects {
    param([string]$SolutionFile)
    
    $projects = @()
    $solutionContent = Get-Content $SolutionFile -Raw
    
    # Parse solution file for project references
    $projectPattern = 'Project\("{[^}]+}"\)\s*=\s*"([^"]+)",\s*"([^"]+\.sqlproj)",\s*"\{[^}]+\}"'
    $matches = [regex]::Matches($solutionContent, $projectPattern)
    
    foreach ($match in $matches) {
        $projectName = $match.Groups[1].Value
        $projectPath = $match.Groups[2].Value
        
        $solutionDir = Split-Path $SolutionFile -Parent
        $fullProjectPath = Join-Path $solutionDir $projectPath
        
        if (Test-Path $fullProjectPath) {
            $projects += @{
                Name = $projectName
                Path = $fullProjectPath
                FullPath = $fullProjectPath
            }
        }
    }
    
    return $projects
}

# Main script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Project DACPAC Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate solution path
if (-not (Test-Path $SolutionPath)) {
    Write-Host "[ERROR] Solution file not found: $SolutionPath" -ForegroundColor Red
    exit 1
}

Write-Host "Solution: $SolutionPath" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Green
Write-Host ""

# Find MSBuild
Write-Host "Locating MSBuild..." -ForegroundColor Yellow
try {
    $msbuild = Find-MSBuild
    Write-Host "[OK] Found MSBuild: $msbuild" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Get database projects from solution
Write-Host ""
Write-Host "Scanning solution for database projects..." -ForegroundColor Yellow
$projects = Get-DatabaseProjects -SolutionFile $SolutionPath

if ($projects.Count -eq 0) {
    Write-Host "[ERROR] No database projects (.sqlproj) found in solution." -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Found $($projects.Count) database project(s)" -ForegroundColor Green
Write-Host ""

# Filter by project name if specified
if ($ProjectName) {
    $projects = $projects | Where-Object { $_.Name -eq $ProjectName }
    if ($projects.Count -eq 0) {
        Write-Host "[ERROR] Project '$ProjectName' not found in solution." -ForegroundColor Red
        exit 1
    }
}

# Build each project
$dacpacFiles = @()
foreach ($project in $projects) {
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    Write-Host "Building: $($project.Name)" -ForegroundColor Cyan
    Write-Host "Project: $($project.Path)" -ForegroundColor Gray
    Write-Host ""
    
    # Build the project
    $buildArgs = @(
        $project.FullPath,
        "/t:Build",
        "/p:Configuration=$Configuration",
        "/p:Platform=`"Any CPU`"",
        "/v:minimal",
        "/nologo"
    )
    
    Write-Host "Running MSBuild..." -ForegroundColor Yellow
    $buildResult = & $msbuild $buildArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed for project: $($project.Name)" -ForegroundColor Red
        $buildResult | Write-Host
        continue
    }
    
    Write-Host "[OK] Build succeeded" -ForegroundColor Green
    
    # Find generated DACPAC file
    $projectDir = Split-Path $project.FullPath -Parent
    $projectNameOnly = [System.IO.Path]::GetFileNameWithoutExtension($project.FullPath)
    
    # DACPAC is typically in bin\{Configuration}\{ProjectName}.dacpac
    $dacpacPath = Join-Path $projectDir "bin\$Configuration\$projectNameOnly.dacpac"
    
    if (-not (Test-Path $dacpacPath)) {
        # Try alternative location
        $dacpacPath = Join-Path $projectDir "bin\$projectNameOnly.dacpac"
    }
    
    if (Test-Path $dacpacPath) {
        Write-Host "[OK] DACPAC generated: $dacpacPath" -ForegroundColor Green
        
        $fileInfo = Get-Item $dacpacPath
        Write-Host "    Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
        
        # Copy to output path if specified
        if ($OutputPath) {
            if (-not (Test-Path $OutputPath)) {
                New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
            }
            
            $outputDacpac = Join-Path $OutputPath "$projectNameOnly.dacpac"
            Copy-Item $dacpacPath $outputDacpac -Force
            Write-Host "    Copied to: $outputDacpac" -ForegroundColor Green
            $dacpacFiles += $outputDacpac
        } else {
            $dacpacFiles += $dacpacPath
        }
    } else {
        Write-Host "[WARNING] DACPAC file not found at expected location: $dacpacPath" -ForegroundColor Yellow
        Write-Host "    Searching for DACPAC files..." -ForegroundColor Yellow
        
        $foundDacpacs = Get-ChildItem -Path $projectDir -Filter "*.dacpac" -Recurse | Where-Object { $_.DirectoryName -like "*$Configuration*" }
        if ($foundDacpacs) {
            foreach ($dacpac in $foundDacpacs) {
                Write-Host "    Found: $($dacpac.FullName)" -ForegroundColor Green
                $dacpacFiles += $dacpac.FullName
            }
        } else {
            Write-Host "[ERROR] No DACPAC files found for project: $($project.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($dacpacFiles.Count -gt 0) {
    Write-Host "[OK] Successfully generated $($dacpacFiles.Count) DACPAC file(s):" -ForegroundColor Green
    foreach ($dacpac in $dacpacFiles) {
        Write-Host "  - $dacpac" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "DACPAC files are ready for deployment!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] No DACPAC files were generated." -ForegroundColor Red
    exit 1
}

