# Plug-and-Play Multi-Database Architecture

## Overview

The project has been refactored to support a plug-and-play architecture that automatically detects and supports multiple database providers (SQL Server, PostgreSQL, and extensible for more).

## Architecture

### Core Components

1. **`IDatabaseProvider` Interface**
   - Defines the contract for all database providers
   - Methods: `TestConnection`, `ListDatabases`, `ExtractSchema`, `RestoreDatabase`, `CanHandleConnectionString`
   - Properties: `ProviderType`, `DefaultFileExtension`

2. **Provider Implementations**
   - **`SqlServerDatabaseProvider`**: Uses DacFx for SQL Server operations
   - **`PostgreSQLDatabaseProvider`**: Uses Npgsql and pg_dump/pg_restore for PostgreSQL operations

3. **`DatabaseProviderFactory`**
   - Auto-detects provider from connection string
   - Supports explicit provider selection by type
   - Allows registration of custom providers

4. **`DatabaseSchemaExtractorV2`**
   - High-level API that uses provider abstraction
   - Auto-detects provider from connection string
   - Maintains backward compatibility with existing code

## Supported Databases

### SQL Server
- **File Extensions**: `.dacpac`, `.bacpac`
- **Connection String Examples**:
  - Local: `Server=(local);Database=MyDB;Integrated Security=True;`
  - Azure SQL: `Server=tcp:myserver.database.windows.net,1433;Database=MyDB;User Id=user@myserver;Password=pass;Encrypt=True;`
  - Named Instance: `Server=localhost\SQLEXPRESS;Database=MyDB;Integrated Security=True;`

### PostgreSQL
- **File Extensions**: `.sql`, `.dump`, `.backup`
- **Connection String Examples**:
  - Standard: `Host=localhost;Port=5432;Database=MyDB;Username=postgres;Password=pass;`
  - URI Format: `postgresql://user:pass@localhost:5432/MyDB`
- **Requirements**: PostgreSQL client tools (pg_dump, pg_restore, psql) must be installed and in PATH

## Usage

### Auto-Detection (Recommended)

```csharp
// Automatically detects provider from connection string
var extractor = new DatabaseSchemaExtractorV2(connectionString);

// All operations work the same way
extractor.TestConnection(connectionString);
extractor.ExtractSchema(connectionString, outputPath);
extractor.RestoreDatabase(filePath, targetConnectionString, targetDatabaseName);
extractor.ListDatabases(connectionString);
```

### Explicit Provider Selection

```csharp
// Specify provider type explicitly
var extractor = new DatabaseSchemaExtractorV2("PostgreSQL", connectionString);
// or
var extractor = new DatabaseSchemaExtractorV2("SqlServer", connectionString);
```

### Direct Provider Usage

```csharp
// Get provider directly
var provider = DatabaseProviderFactory.GetProvider(connectionString);
// or
var provider = DatabaseProviderFactory.GetProviderByType("PostgreSQL");

// Use provider directly
provider.ExtractSchema(connectionString, outputPath);
```

## API Changes

The `FileDownloadApi` and `McpServer` have been updated to use `DatabaseSchemaExtractorV2`, which automatically detects the database provider from the connection string. No changes are required in API requests - the system automatically handles SQL Server and PostgreSQL.

## Adding New Providers

To add support for a new database:

1. Implement `IDatabaseProvider` interface
2. Register with `DatabaseProviderFactory.RegisterProvider()`

Example:

```csharp
public class MySQLDatabaseProvider : IDatabaseProvider
{
    public string ProviderType => "MySQL";
    public string DefaultFileExtension => ".sql";
    
    public bool CanHandleConnectionString(string connectionString) { ... }
    public ConnectionTestResult TestConnection(string connectionString, int timeoutSeconds = 30) { ... }
    public DatabaseListResult ListDatabases(string connectionString, int timeoutSeconds = 30) { ... }
    public bool ExtractSchema(string connectionString, string outputFilePath, ExtractOptions extractOptions = null, bool validateConnection = true) { ... }
    public bool RestoreDatabase(string packageFilePath, string targetConnectionString, string targetDatabaseName, bool upgradeExisting = false, bool validateConnection = true) { ... }
}

// Register
DatabaseProviderFactory.RegisterProvider(new MySQLDatabaseProvider());
```

## Backward Compatibility

The original `DatabaseSchemaExtractor` class remains unchanged for backward compatibility. New code should use `DatabaseSchemaExtractorV2` for multi-database support.

## PostgreSQL Requirements

For PostgreSQL operations, ensure:
1. PostgreSQL client tools are installed (pg_dump, pg_restore, psql)
2. Tools are in system PATH, or update the provider code to specify custom paths
3. Npgsql NuGet package is installed (already included)

## File Extensions

- **SQL Server**: `.dacpac` (schema only), `.bacpac` (schema + data)
- **PostgreSQL**: `.sql` (plain SQL), `.dump` or `.backup` (custom format)

The system automatically selects appropriate extensions based on the detected provider.

