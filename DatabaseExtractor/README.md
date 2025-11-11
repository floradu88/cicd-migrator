# DatabaseExtractor Class Library

A .NET Framework 4.6.2 class library for extracting database schema from Azure SQL Database to DACPAC files.

## Overview

This class library provides a simple, easy-to-use API for extracting database schemas from Azure SQL Database using the Microsoft SQL Server Data-Tier Application Framework (DacFx).

## Requirements

- .NET Framework 4.6.2 or higher
- Microsoft.SqlServer.DacFx NuGet package (version 162.0.0 or compatible)
- Access to Azure SQL Database with appropriate permissions

## Installation

### Via NuGet (if published)

```powershell
Install-Package DatabaseExtractor
```

### Manual Installation

1. **Add the project to your solution:**
   - In Visual Studio: Add → Existing Project → Select `DatabaseExtractor.csproj`

2. **Add a project reference:**
   - Right-click your project → Add → Reference → Projects → Select `DatabaseExtractor`

3. **Restore NuGet packages:**
   ```powershell
   nuget restore DatabaseExtractor\packages.config
   ```

## Quick Start

```csharp
using DatabaseExtractor;

// Create extractor instance
var extractor = new DatabaseSchemaExtractor();

// Your Azure SQL connection string
string connectionString = "Server=tcp:yourserver.database.windows.net,1433;Database=yourdb;User ID=user;Password=pass;Encrypt=True;";

// Extract schema to DACPAC
extractor.ExtractSchema(connectionString, @"C:\Output\MyDatabase.dacpac");
```

## API Reference

### `DatabaseSchemaExtractor` Class

Main class for extracting database schemas.

#### Methods

**`ExtractSchema(string connectionString, string outputDacpacPath, ExtractOptions extractOptions = null)`**

Extracts database schema to a DACPAC file.

- **Parameters:**
  - `connectionString` (string): Azure SQL Database connection string
  - `outputDacpacPath` (string): Full path where DACPAC will be created
  - `extractOptions` (ExtractOptions, optional): Extraction configuration options
- **Returns:** `bool` - true if successful
- **Throws:**
  - `ArgumentNullException` - if connectionString or outputDacpacPath is null/empty
  - `ArgumentException` - if database name not found in connection string
  - `DacServicesException` - if extraction fails
  - `Exception` - for unexpected errors

**`ExtractSchema(string connectionString, string outputDacpacPath, bool extractTableData, bool ignoreExtendedProperties, bool verifyExtraction)`**

Simplified overload with inline parameters.

- **Parameters:**
  - `connectionString` (string): Azure SQL Database connection string
  - `outputDacpacPath` (string): Full path where DACPAC will be created
  - `extractTableData` (bool): Extract table data along with schema (default: false)
  - `ignoreExtendedProperties` (bool): Ignore extended properties (default: false)
  - `verifyExtraction` (bool): Verify extraction after completion (default: true)
- **Returns:** `bool` - true if successful

### `ExtractOptions` Class

Configuration class for extraction options.

#### Properties

- **`ExtractAllTableData`** (bool): Extract table data along with schema (default: false)
- **`IgnoreExtendedProperties`** (bool): Ignore extended properties (default: false)
- **`VerifyExtraction`** (bool): Verify extraction after completion (default: true)

## Usage Examples

### Example 1: Basic Extraction

```csharp
var extractor = new DatabaseSchemaExtractor();
extractor.ExtractSchema(connectionString, @"C:\Output\MyDatabase.dacpac");
```

### Example 2: With Custom Options

```csharp
var extractor = new DatabaseSchemaExtractor();

var options = new ExtractOptions
{
    ExtractAllTableData = false,
    IgnoreExtendedProperties = false,
    VerifyExtraction = true
};

extractor.ExtractSchema(connectionString, outputPath, options);
```

### Example 3: Simplified Method

```csharp
var extractor = new DatabaseSchemaExtractor();
extractor.ExtractSchema(
    connectionString: connectionString,
    outputDacpacPath: outputPath,
    extractTableData: false,
    ignoreExtendedProperties: false,
    verifyExtraction: true
);
```

### Example 4: Error Handling

```csharp
try
{
    var extractor = new DatabaseSchemaExtractor();
    extractor.ExtractSchema(connectionString, outputPath);
    Console.WriteLine("Success!");
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Invalid parameter: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
catch (DacServicesException ex)
{
    Console.WriteLine($"Extraction failed: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Connection String Format

Azure SQL Database connection string format:

```
Server=tcp:servername.database.windows.net,1433;Database=dbname;User ID=username;Password=password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Features

- ✅ Simple, intuitive API
- ✅ Automatic output directory creation
- ✅ Progress and message event handlers
- ✅ Comprehensive error handling
- ✅ Configurable extraction options
- ✅ .NET Framework 4.6.2 compatible
- ✅ Works with Azure SQL Database

## Dependencies

- **Microsoft.SqlServer.DacFx** (162.0.0) - SQL Server Data-Tier Application Framework

## Notes

- The output directory will be created automatically if it doesn't exist
- Progress and messages are written to console by default (can be customized)
- Requires database user with at least `db_datareader` permissions
- Ensure your IP is allowed in Azure SQL firewall rules

## See Also

- See `ExampleConsoleApp` project for complete working examples
- [Microsoft DacFx Documentation](https://docs.microsoft.com/en-us/sql/tools/sqlpackage)
