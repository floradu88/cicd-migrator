# Database Schema Extractor

A .NET Framework 4.6.2 solution for extracting database schema from Azure SQL Database to DACPAC files.

## Solution Structure

```
.
├── DatabaseExtractor/          # Class library project
│   ├── DatabaseSchemaExtractor.cs
│   ├── ExtractOptions.cs
│   └── DatabaseExtractor.csproj
│
├── ExampleConsoleApp/           # Example console application
│   ├── Program.cs               # Usage examples
│   └── ExampleConsoleApp.csproj
│
└── README.md                    # This file
```

## Quick Start

### 1. Build the Class Library

```powershell
# Restore NuGet packages and build
msbuild DatabaseExtractor\DatabaseExtractor.csproj /t:Restore,Build
```

Or in Visual Studio:
- Open the solution
- Right-click solution → Restore NuGet Packages
- Build → Build Solution

### 2. Use in Your Code

```csharp
using DatabaseExtractor;

var extractor = new DatabaseSchemaExtractor();
extractor.ExtractSchema(
    connectionString: "Server=tcp:yourserver.database.windows.net,1433;Database=yourdb;User ID=user;Password=pass;Encrypt=True;",
    outputDacpacPath: @"C:\Output\MyDatabase.dacpac"
);
```

### 3. Run the Example

See `ExampleConsoleApp` for complete working examples.

## Projects

### DatabaseExtractor

Class library containing:
- `DatabaseSchemaExtractor` - Main extraction class
- `ExtractOptions` - Configuration options

**Target Framework:** .NET Framework 4.6.2  
**Output Type:** Class Library (DLL)

### ExampleConsoleApp

Console application demonstrating:
- Basic usage
- Custom options
- Error handling
- Multiple extraction scenarios

**Target Framework:** .NET Framework 4.6.2  
**Output Type:** Console Application (EXE)

## Requirements

- .NET Framework 4.6.2
- Microsoft.SqlServer.DacFx (NuGet package, version 162.0.0)
- Access to Azure SQL Database
- Visual Studio 2017+ or MSBuild

## Documentation

- [DatabaseExtractor README](DatabaseExtractor/README.md) - Class library documentation
- [ExampleConsoleApp README](ExampleConsoleApp/README.md) - Example application guide

## Building from Command Line

```powershell
# Restore packages
nuget restore

# Build class library
msbuild DatabaseExtractor\DatabaseExtractor.csproj /t:Build

# Build example app
msbuild ExampleConsoleApp\ExampleConsoleApp.csproj /t:Build
```

## License

This project is provided as-is for internal use.

