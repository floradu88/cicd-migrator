# Build and Run Scripts

PowerShell scripts for managing the DatabaseExtractor solution.

## Scripts

### restore.ps1
Restores NuGet packages for all projects in the solution.

**Usage:**
```powershell
.\restore.ps1
```

**Features:**
- Automatically finds MSBuild, NuGet.exe, or dotnet CLI
- Restores packages for all projects
- Shows progress and results

### build.ps1
Builds all projects in the solution.

**Usage:**
```powershell
# Build in Debug configuration (default)
.\build.ps1

# Build in Release configuration
.\build.ps1 -Configuration Release

# Clean and build
.\build.ps1 -Clean
```

**Features:**
- Builds all projects in correct dependency order
- Supports Debug and Release configurations
- Optional clean before build
- Shows build outputs location

### run.ps1
Runs the FileDownloadApi using IIS Express.

**Usage:**
```powershell
# Run on default port (8080)
.\run.ps1

# Run on custom port
.\run.ps1 -Port 9000

# Run and open browser automatically
.\run.ps1 -OpenBrowser

# Run Release build
.\run.ps1 -Configuration Release
```

**Features:**
- Verifies API is built before running
- Automatically finds IIS Express
- Checks if port is available
- Tests API connection on startup
- Shows all available endpoints
- Press any key to stop

## Quick Start Workflow

1. **Restore packages:**
   ```powershell
   .\scripts\restore.ps1
   ```

2. **Build solution:**
   ```powershell
   .\scripts\build.ps1
   ```

3. **Run API:**
   ```powershell
   .\scripts\run.ps1 -OpenBrowser
   ```

## Prerequisites

- **MSBuild** - Included with Visual Studio 2017+
- **IIS Express** - Included with Visual Studio (for run.ps1)
- **PowerShell** - Windows PowerShell 5.1+ or PowerShell Core

## Notes

- All scripts should be run from the `scripts` folder
- Scripts automatically resolve paths relative to the scripts folder
- If MSBuild is not found, scripts provide helpful error messages
- The `run.ps1` script will verify the API is built before attempting to run

## Troubleshooting

### MSBuild not found
- Install Visual Studio 2017+ (includes MSBuild)
- Or add MSBuild to your PATH

### IIS Express not found
- Install Visual Studio (includes IIS Express)
- Or download IIS Express separately

### Port already in use
- Use a different port: `.\run.ps1 -Port 9000`
- Or stop the process using the port

### Build fails
- Ensure packages are restored: `.\restore.ps1`
- Check that .NET Framework 4.8 is installed
- Review build output for specific errors

