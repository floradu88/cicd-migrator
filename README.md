# Database Schema Extractor

A .NET Framework 4.8 solution for extracting database schema from SQL Server (local or Azure SQL Database) to BACPAC/DACPAC files, restoring databases, and serving them via a Web API. Includes MCP (Model Context Protocol) server integration for AI assistant support.

## Solution Structure

```
.
├── DatabaseExtractor.sln          # Visual Studio solution file
│
├── DatabaseExtractor/             # Class Library Project
│   ├── DatabaseSchemaExtractor.cs
│   ├── ExtractOptions.cs
│   ├── ConnectionStringExamples.cs
│   └── DatabaseExtractor.csproj
│
├── ExampleConsoleApp/              # Example Console Application
│   ├── Program.cs
│   └── ExampleConsoleApp.csproj
│
├── FileDownloadApi/                # Web API for file downloads and database operations
│   ├── Controllers/
│   │   └── FilesController.cs
│   ├── Web.config
│   └── FileDownloadApi.csproj
│
└── McpServer/                      # MCP Server for AI assistant integration
    ├── Program.cs
    └── McpServer.csproj
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
    outputDacpacPath: @"C:\DacpacFiles\MyDatabase.bacpac",
    extractTableData: false  // Set to true to include data in BACPAC
);
```

### 3. Serve Files and Operations via API

Start the `FileDownloadApi` project and access files:

```
# File Operations
GET  http://localhost:8080/api/files/MyDatabase.bacpac          # Download file
GET  http://localhost:8080/api/files/MyDatabase.bacpac/status    # Get file status
GET  http://localhost:8080/api/files                              # List all files

# Database Operations
POST http://localhost:8080/api/files/MyDatabase.bacpac/restore   # Restore database
POST http://localhost:8080/api/files/test-connection             # Test connection
POST http://localhost:8080/api/files/list-databases               # List databases (security audit)
```

## Projects

### DatabaseExtractor

Class library for extracting database schemas to BACPAC/DACPAC files, restoring databases, testing connections, and listing databases for security auditing.

**Target Framework:** .NET Framework 4.8  
**Output Type:** Class Library (DLL)  
**NuGet Packages:**
- Microsoft.SqlServer.DacFx (160.5400.1)
- Microsoft.SqlServer.DacFx.x64 (150.5282.3)

**Features:**
- Extract database schema to BACPAC (schema + data) or DACPAC (schema only)
- Restore BACPAC/DACPAC files to databases
- Test database connections
- List all accessible databases with security information

[See Documentation](DatabaseExtractor/README.md)

### ExampleConsoleApp

Console application demonstrating usage of the DatabaseExtractor class library.

**Target Framework:** .NET Framework 4.8  
**Output Type:** Console Application (EXE)

[See Documentation](ExampleConsoleApp/README.md)

### FileDownloadApi

ASP.NET Web API 2 for downloading DACPAC/BACPAC files, checking file status, restoring databases, testing connections, and security auditing.

**Target Framework:** .NET Framework 4.8  
**Output Type:** Web Application  
**NuGet Packages:**
- Microsoft.AspNet.WebApi (5.3.0)
- Microsoft.AspNet.WebApi.Client (6.0.0)
- Microsoft.AspNet.WebApi.Core (5.3.0)
- Microsoft.AspNet.WebApi.WebHost (5.3.0)
- Newtonsoft.Json (13.0.4)

**API Endpoints:**
- `GET /api/files/{filename}` - Download DACPAC/BACPAC file
- `GET /api/files/{filename}/status` - Get file status
- `GET /api/files` - List all available files
- `POST /api/files/{filename}/restore` - Restore database from DACPAC/BACPAC
- `POST /api/files/test-connection` - Test database connection
- `POST /api/files/list-databases` - List databases (security auditing)

[See Documentation](FileDownloadApi/README.md)

### McpServer

Model Context Protocol (MCP) server for AI assistant integration. Exposes database tools via JSON-RPC 2.0 protocol.

**Target Framework:** .NET Framework 4.8  
**Output Type:** Console Application (EXE)  
**NuGet Packages:**
- Newtonsoft.Json (13.0.4)

**MCP Tools:**
- `extract_schema` - Extract database schema to BACPAC
- `restore_database` - Restore DACPAC/BACPAC to database
- `test_connection` - Test database connection
- `list_databases` - List accessible databases (security audit)
- `list_connection_examples` - Get connection string examples

[See Documentation](McpServer/README.md)

## Requirements

- **.NET Framework 4.8** (all projects)
- **Microsoft.SqlServer.DacFx** (160.5400.1) - SQL Server Data-Tier Application Framework
- **ASP.NET Web API 2** (5.3.0) - For FileDownloadApi
- **Visual Studio 2017+** or **MSBuild** with .NET Framework 4.8 targeting pack
- **IIS or IIS Express** (for FileDownloadApi)
- **SQL Server** (local, LocalDB, or Azure SQL Database) with appropriate permissions

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

1. **Extract schema** using `DatabaseExtractor` class library (creates BACPAC/DACPAC files)
2. **Store BACPAC/DACPAC files** in a configured directory (default: `App_Data/Files`)
3. **Serve files** via `FileDownloadApi` endpoints
4. **Restore databases** from BACPAC/DACPAC files via API
5. **Test connections** and **audit database access** for security compliance
6. **Use MCP server** for AI assistant integration (VS Code, Cursor, etc.)

## Configuration

### FileDownloadApi - Files Directory

Edit `FileDownloadApi/Web.config`:

```xml
<appSettings>
  <add key="DacpacFilesPath" value="C:\DacpacFiles" />
</appSettings>
```

### MCP Server Integration

See [McpServer/README.md](McpServer/README.md) and [McpServer/INTEGRATION.md](McpServer/INTEGRATION.md) for detailed integration instructions with VS Code, Cursor, and other IDEs.

## Key Features

- ✅ **Database Schema Extraction** - Extract to BACPAC (schema + data) or DACPAC (schema only)
- ✅ **Database Restore** - Restore BACPAC/DACPAC files to target databases
- ✅ **Connection Testing** - Validate database connections before operations
- ✅ **Security Auditing** - List all accessible databases with permission details
- ✅ **Web API** - RESTful API for all operations
- ✅ **MCP Integration** - AI assistant support via Model Context Protocol
- ✅ **Local & Azure Support** - Works with local SQL Server, LocalDB, and Azure SQL Database
- ✅ **Comprehensive Error Handling** - Detailed error messages and validation

## Version Information

- **.NET Framework:** 4.8 (all projects)
- **Microsoft.SqlServer.DacFx:** 160.5400.1
- **ASP.NET Web API:** 5.3.0
- **Newtonsoft.Json:** 13.0.4

## License

This project is provided as-is for internal use.
