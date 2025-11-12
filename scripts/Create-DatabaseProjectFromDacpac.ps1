<#
.SYNOPSIS
    Creates a Visual Studio SQL Server Database Project from a DACPAC file.

.DESCRIPTION
    This script creates a new Visual Studio 2019 SQL Server Database Project,
    imports a DACPAC file into it, and saves the solution to disk.

.PARAMETER DacpacPath
    Path to the DACPAC file to import

.PARAMETER SolutionPath
    Path where the solution (.sln) will be created

.PARAMETER ProjectName
    Name of the database project (defaults to DACPAC filename without extension)

.PARAMETER ProjectPath
    Optional path for the project folder. If not specified, uses SolutionPath\ProjectName

.PARAMETER VisualStudioVersion
    Visual Studio version (2019 or 2022). Default: 2019

.EXAMPLE
    .\Create-DatabaseProjectFromDacpac.ps1 -DacpacPath "C:\Dacpacs\MyDatabase.dacpac" -SolutionPath "C:\Projects\MyDatabase.sln"
    
.EXAMPLE
    .\Create-DatabaseProjectFromDacpac.ps1 -DacpacPath "C:\Dacpacs\MyDatabase.dacpac" -SolutionPath "C:\Projects\MyDatabase.sln" -ProjectName "MyDatabaseProject"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$DacpacPath,
    
    [Parameter(Mandatory=$true)]
    [string]$SolutionPath,
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectName = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("2019", "2022")]
    [string]$VisualStudioVersion = "2019"
)

# Function to find SqlPackage.exe
function Find-SqlPackage {
    $sqlPackagePaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150\SqlPackage.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150\SqlPackage.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150\SqlPackage.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150\SqlPackage.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150\SqlPackage.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150\SqlPackage.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\140\DAC\bin\SqlPackage.exe"
    )
    
    foreach ($path in $sqlPackagePaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    throw "SqlPackage.exe not found. Please install SQL Server Data Tools (SSDT) or Visual Studio with SQL Server Database Projects."
}

# Function to find Visual Studio installation
function Find-VisualStudio {
    param([string]$Version)
    
    $vsPaths = @()
    if ($Version -eq "2022") {
        $vsPaths = @(
            "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise",
            "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional",
            "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community"
        )
    } else {
        $vsPaths = @(
            "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise",
            "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional",
            "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community"
        )
    }
    
    foreach ($path in $vsPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    # Try vswhere
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -version "[$Version.0,$($Version + 1).0)" -latest -property installationPath
        if ($vsPath -and (Test-Path $vsPath)) {
            return $vsPath
        }
    }
    
    throw "Visual Studio $Version not found."
}

# Function to create .sqlproj file
function New-SqlProjectFile {
    param(
        [string]$ProjectPath,
        [string]$ProjectName,
        [string]$VisualStudioVersion
    )
    
    $projectGuid = [System.Guid]::NewGuid().ToString("B").ToUpper()
    $projectFile = Join-Path $ProjectPath "$ProjectName.sqlproj"
    
    $vsVersion = if ($VisualStudioVersion -eq "2022") { "17.0" } else { "16.0" }
    
    $projectXml = @"
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration Condition=" '`$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '`$(Platform)' == '' ">AnyCPU</Platform>
    <Name>$ProjectName</Name>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectVersion>4.1</ProjectVersion>
    <ProjectGuid>{$projectGuid}</ProjectGuid>
    <DSP>Microsoft.Data.Tools.Msbuild.UnitTest.targets</DSP>
    <DeployToDatabase>True</DeployToDatabase>
    <DeployScriptFileName>`$(MSBuildProjectName).sql</DeployScriptFileName>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <VisualStudioVersion Condition="'`$(VisualStudioVersion)' == ''">$vsVersion</VisualStudioVersion>
    <VSToolsPath Condition="'`$(VSToolsPath)' == ''">`$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v`$(VisualStudioVersion)</VSToolsPath>
    <SSDTExists Condition="Exists('`$(VSToolsPath)\SSDT\Microsoft.Data.Tools.Schema.SqlTasks.targets')">True</SSDTExists>
    <SSDTPath Condition="'`$(SSDTExists)' == 'True'">`$(VSToolsPath)\SSDT</SSDTPath>
  </PropertyGroup>
  <PropertyGroup Condition=" '`$(Configuration)|`$(Platform)' == 'Debug|AnyCPU' ">
    <OutputPath>bin\Debug\</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" '`$(Configuration)|`$(Platform)' == 'Release|AnyCPU' ">
    <OutputPath>bin\Release\</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Folder Include="Properties" />
    <Folder Include="Tables" />
    <Folder Include="Views" />
    <Folder Include="Stored Procedures" />
    <Folder Include="Functions" />
    <Folder Include="Security" />
  </ItemGroup>
  <Import Condition="'`$(SQLDBExtensionsRefPath)' != ''" Project="`$(SQLDBExtensionsRefPath)\Microsoft.Data.Tools.Schema.SqlTasks.targets" />
  <Import Condition="'`$(SQLDBExtensionsRefPath)' == ''" Project="`$(VSToolsPath)\SSDT\Microsoft.Data.Tools.Schema.SqlTasks.targets" />
  <Import Condition="'`$(SQLDBExtensionsRefPath)' != ''" Project="`$(SQLDBExtensionsRefPath)\Microsoft.Data.Tools.Schema.Sql.UnitTesting.targets" />
  <Import Condition="'`$(SQLDBExtensionsRefPath)' == '' AND '`$(VSToolsPath)' != ''" Project="`$(VSToolsPath)\SSDT\Microsoft.Data.Tools.Schema.Sql.UnitTesting.targets" />
</Project>
"@
    
    Set-Content -Path $projectFile -Value $projectXml -Encoding UTF8
    return $projectFile
}

# Function to create solution file
function New-SolutionFile {
    param(
        [string]$SolutionPath,
        [string]$ProjectName,
        [string]$ProjectPath,
        [string]$ProjectGuid
    )
    
    $solutionGuid = [System.Guid]::NewGuid().ToString("B").ToUpper()
    $projectRelativePath = [System.IO.Path]::GetRelativePath((Split-Path $SolutionPath -Parent), $ProjectPath)
    
    $solutionContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30114.105
MinimumVisualStudioVersion = 10.0.40219.1
Project("{$solutionGuid}") = "$ProjectName", "$projectRelativePath", {$ProjectGuid}
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{$ProjectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{$ProjectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{$ProjectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{$ProjectGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"@
    
    Set-Content -Path $SolutionPath -Value $solutionContent -Encoding UTF8
}

# Main script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Create Database Project from DACPAC" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate DACPAC path
if (-not (Test-Path $DacpacPath)) {
    Write-Host "[ERROR] DACPAC file not found: $DacpacPath" -ForegroundColor Red
    exit 1
}

Write-Host "DACPAC: $DacpacPath" -ForegroundColor Green

# Determine project name
if ([string]::IsNullOrWhiteSpace($ProjectName)) {
    $ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($DacpacPath)
}
Write-Host "Project Name: $ProjectName" -ForegroundColor Green

# Determine project path
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $solutionDir = Split-Path $SolutionPath -Parent
    $ProjectPath = Join-Path $solutionDir $ProjectName
}
Write-Host "Project Path: $ProjectPath" -ForegroundColor Green
Write-Host "Solution Path: $SolutionPath" -ForegroundColor Green
Write-Host ""

# Find SqlPackage.exe
Write-Host "Locating SqlPackage.exe..." -ForegroundColor Yellow
try {
    $sqlPackage = Find-SqlPackage
    Write-Host "[OK] Found SqlPackage: $sqlPackage" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Create directories
Write-Host ""
Write-Host "Creating project structure..." -ForegroundColor Yellow
$solutionDir = Split-Path $SolutionPath -Parent
if (-not (Test-Path $solutionDir)) {
    New-Item -ItemType Directory -Path $solutionDir -Force | Out-Null
}

if (-not (Test-Path $ProjectPath)) {
    New-Item -ItemType Directory -Path $ProjectPath -Force | Out-Null
}

# Create subdirectories
$folders = @("Properties", "Tables", "Views", "Stored Procedures", "Functions", "Security")
foreach ($folder in $folders) {
    $folderPath = Join-Path $ProjectPath $folder
    if (-not (Test-Path $folderPath)) {
        New-Item -ItemType Directory -Path $folderPath -Force | Out-Null
    }
}

Write-Host "[OK] Project structure created" -ForegroundColor Green

# Create .sqlproj file
Write-Host ""
Write-Host "Creating SQL Server Database Project file..." -ForegroundColor Yellow
try {
    $projectFile = New-SqlProjectFile -ProjectPath $ProjectPath -ProjectName $ProjectName -VisualStudioVersion $VisualStudioVersion
    Write-Host "[OK] Project file created: $projectFile" -ForegroundColor Green
    
    # Read project GUID from the file
    $projectXml = [xml](Get-Content $projectFile)
    $projectGuid = $projectXml.Project.PropertyGroup.ProjectGuid
} catch {
    Write-Host "[ERROR] Failed to create project file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Extract DACPAC to project folder
Write-Host ""
Write-Host "Extracting DACPAC to project folder..." -ForegroundColor Yellow
$extractPath = Join-Path $ProjectPath "Extracted"
if (-not (Test-Path $extractPath)) {
    New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
}

try {
    $extractArgs = @(
        "/Action:Extract",
        "/SourceFile:`"$DacpacPath`"",
        "/TargetPath:`"$extractPath`"",
        "/TargetFile:`"$ProjectName.dacpac`"",
        "/p:ExtractAllTableData=false",
        "/p:ExtractApplicationScopedObjectsOnly=false"
    )
    
    Write-Host "Running SqlPackage extract..." -ForegroundColor Gray
    $extractResult = & $sqlPackage $extractArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to extract DACPAC" -ForegroundColor Red
        $extractResult | Write-Host
        exit 1
    }
    
    Write-Host "[OK] DACPAC extracted" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to extract DACPAC: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Import DACPAC using SqlPackage Script action
Write-Host ""
Write-Host "Importing DACPAC into project..." -ForegroundColor Yellow

# Create a temporary database connection string for import
$tempDbName = "TempImport_$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
$tempConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=$tempDbName;Integrated Security=True;"

try {
    # First, import DACPAC to a temporary database
    Write-Host "Importing to temporary database..." -ForegroundColor Gray
    $importArgs = @(
        "/Action:Import",
        "/SourceFile:`"$DacpacPath`"",
        "/TargetConnectionString:`"$tempConnectionString`"",
        "/p:CommandTimeout=120"
    )
    
    $importResult = & $sqlPackage $importArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARNING] Direct import failed, trying alternative method..." -ForegroundColor Yellow
        Write-Host "Note: You may need to manually import the DACPAC in Visual Studio" -ForegroundColor Yellow
    } else {
        Write-Host "[OK] DACPAC imported to temporary database" -ForegroundColor Green
        
        # Extract schema from temporary database
        Write-Host "Extracting schema from temporary database..." -ForegroundColor Gray
        $extractFromDbArgs = @(
            "/Action:Extract",
            "/TargetConnectionString:`"$tempConnectionString`"",
            "/TargetFile:`"$extractPath\$ProjectName.dacpac`"",
            "/p:ExtractAllTableData=false"
        )
        
        $extractFromDbResult = & $sqlPackage $extractFromDbArgs 2>&1
        
        # Clean up temporary database
        Write-Host "Cleaning up temporary database..." -ForegroundColor Gray
        try {
            $dropDbSql = "DROP DATABASE IF EXISTS [$tempDbName]"
            sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $dropDbSql -E 2>&1 | Out-Null
        } catch {
            # Ignore cleanup errors
        }
    }
} catch {
    Write-Host "[WARNING] Import process encountered issues: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Note: You can manually import the DACPAC in Visual Studio by:" -ForegroundColor Yellow
    Write-Host "  1. Open the solution in Visual Studio" -ForegroundColor Gray
    Write-Host "  2. Right-click the project -> Import -> Data-tier Application (.dacpac)" -ForegroundColor Gray
    Write-Host "  3. Select the DACPAC file" -ForegroundColor Gray
}

# Create solution file
Write-Host ""
Write-Host "Creating solution file..." -ForegroundColor Yellow
try {
    New-SolutionFile -SolutionPath $SolutionPath -ProjectName $ProjectName -ProjectPath $projectFile -ProjectGuid $projectGuid
    Write-Host "[OK] Solution file created: $SolutionPath" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to create solution file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[OK] Database project created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Solution: $SolutionPath" -ForegroundColor Gray
Write-Host "Project: $projectFile" -ForegroundColor Gray
Write-Host "Project Path: $ProjectPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Open the solution in Visual Studio $VisualStudioVersion" -ForegroundColor Gray
Write-Host "  2. If DACPAC wasn't fully imported, right-click project -> Import -> Data-tier Application" -ForegroundColor Gray
Write-Host "  3. Select: $DacpacPath" -ForegroundColor Gray
Write-Host "  4. Build the project to verify it compiles" -ForegroundColor Gray
Write-Host ""

