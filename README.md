# Database Schema Extractor

A .NET Framework 4.6.2 solution for extracting database schema from Azure SQL Database to DACPAC files and serving them via a minimal API.

## Solution Structure

```
.
├── DatabaseExtractor.sln          # Visual Studio solution file
│
├── DatabaseExtractor/             # Class Library Project
│   ├── DatabaseSchemaExtractor.cs
│   ├── ExtractOptions.cs
│   └── DatabaseExtractor.csproj
│
├── ExampleConsoleApp/              # Example Console Application
│   ├── Program.cs
│   └── ExampleConsoleApp.csproj
│
└── FileDownloadApi/                # Minimal Web API for file downloads
    ├── Controllers/
    │   └── FilesController.cs
    ├── Web.config
    └── FileDownloadApi.csproj
```

## Quick Start

### 1. Build the Solution

```powershell
# Restore NuGet packages and build
msbuild DatabaseExtractor.sln /t:Restore,Build
```

Or in Visual Studio:
- Open `DatabaseExtractor.sln`
- Right-click solution → Restore NuGet Packages
- Build → Build Solution

### 2. Extract Database Schema

```csharp
using DatabaseExtractor;

var extractor = new DatabaseSchemaExtractor();
extractor.ExtractSchema(
    connectionString: "Server=tcp:yourserver.database.windows.net,1433;Database=yourdb;User ID=user;Password=pass;Encrypt=True;",
    outputDacpacPath: @"C:\DacpacFiles\MyDatabase.dacpac"
);
```

### 3. Serve Files via API

Start the `FileDownloadApi` project and access files:

```
GET http://localhost:8080/api/files/MyDatabase.dacpac          # Download file
GET http://localhost:8080/api/files/MyDatabase.dacpac/status    # Get file status
GET http://localhost:8080/api/files                             # List all files
```

## Projects

### DatabaseExtractor

Class library for extracting database schemas to DACPAC files.

**Target Framework:** .NET Framework 4.6.2  
**Output Type:** Class Library (DLL)

[See Documentation](DatabaseExtractor/README.md)

### ExampleConsoleApp

Console application demonstrating usage of the DatabaseExtractor class library.

**Target Framework:** .NET Framework 4.6.2  
**Output Type:** Console Application (EXE)

[See Documentation](ExampleConsoleApp/README.md)

### FileDownloadApi

Minimal ASP.NET Web API 2 for downloading DACPAC files and checking file status.

**Target Framework:** .NET Framework 4.6.2  
**Output Type:** Web Application

**API Endpoints:**
- `GET /api/files/{filename}` - Download DACPAC file
- `GET /api/files/{filename}/status` - Get file status
- `GET /api/files` - List all available files

[See Documentation](FileDownloadApi/README.md)

## Requirements

- .NET Framework 4.6.2
- Microsoft.SqlServer.DacFx (NuGet package)
- ASP.NET Web API 2 (for FileDownloadApi)
- Visual Studio 2017+ or MSBuild
- IIS or IIS Express (for FileDownloadApi)

## Building from Command Line

```powershell
# Restore packages
nuget restore

# Build all projects
msbuild DatabaseExtractor.sln /t:Build

# Build individual projects
msbuild DatabaseExtractor\DatabaseExtractor.csproj /t:Build
msbuild ExampleConsoleApp\ExampleConsoleApp.csproj /t:Build
msbuild FileDownloadApi\FileDownloadApi.csproj /t:Build
```

## Usage Workflow

1. **Extract schema** using `DatabaseExtractor` class library
2. **Store DACPAC files** in a configured directory (default: `App_Data/Files`)
3. **Serve files** via `FileDownloadApi` endpoints
4. **Check status** or **list files** using API endpoints

## Configuration

### FileDownloadApi - Files Directory

Edit `FileDownloadApi/Web.config`:

```xml
<appSettings>
  <add key="DacpacFilesPath" value="C:\DacpacFiles" />
</appSettings>
```

## License

This project is provided as-is for internal use.
