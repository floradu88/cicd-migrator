# File Download API

A minimal ASP.NET Web API 2 project for downloading DACPAC files and checking file status.

## Overview

This API provides simple GET endpoints to:
- Download DACPAC files
- Check file status (exists, size, last modified, etc.)
- List all available files

## Requirements

- .NET Framework 4.6.2
- ASP.NET Web API 2
- IIS or IIS Express for hosting

## Configuration

### Files Directory

Configure the path where DACPAC files are stored in `Web.config`:

```xml
<appSettings>
  <add key="DacpacFilesPath" value="C:\DacpacFiles" />
</appSettings>
```

If not specified, files will be stored in `App_Data/Files` relative to the application.

## API Endpoints

### 1. Download File

**GET** `/api/files/{filename}`

Downloads a DACPAC or BACPAC file.

**Example:**
```
GET http://localhost:8080/api/files/MyDatabase.dacpac
```

**Response:**
- `200 OK` - File download (binary content)
- `404 Not Found` - File doesn't exist
- `500 Internal Server Error` - Error reading file

### 2. Get File Status

**GET** `/api/files/{filename}/status`

Returns status information about a file.

**Example:**
```
GET http://localhost:8080/api/files/MyDatabase.dacpac/status
```

**Response:**
```json
{
  "filename": "MyDatabase.dacpac",
  "exists": true,
  "size": 1048576,
  "sizeFormatted": "1 MB",
  "lastModified": "2025-01-15T10:30:00",
  "created": "2025-01-15T10:25:00",
  "fullPath": "C:\\DacpacFiles\\MyDatabase.dacpac",
  "error": null
}
```

### 3. List All Files

**GET** `/api/files`

Returns a list of all available DACPAC and BACPAC files.

### 4. Restore Database

**POST** `/api/files/{filename}/restore`

Restores a DACPAC or BACPAC file to a target database.

**Request Body:**
```json
{
  "targetConnectionString": "Server=(local);Database=TargetDB;Integrated Security=True;",
  "targetDatabaseName": "TargetDB",
  "upgradeExisting": false
}
```

**Example:**
```
POST http://localhost:8080/api/files/MyDatabase.bacpac/restore
Content-Type: application/json

{
  "targetConnectionString": "Server=(local);Database=RestoredDB;Integrated Security=True;",
  "targetDatabaseName": "RestoredDB",
  "upgradeExisting": false
}
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully restored MyDatabase.bacpac to database 'RestoredDB'",
  "filename": "MyDatabase.bacpac",
  "targetDatabaseName": "RestoredDB"
}
```

**Notes:**
- Supports both `.dacpac` (schema only) and `.bacpac` (schema + data) files
- If `upgradeExisting` is `false`, creates a new database
- If `upgradeExisting` is `true`, upgrades an existing database
- The target database will be created if it doesn't exist (for DACPAC files)
- Connection validation is performed by default before restore (can be disabled by setting `validateConnection: false`)

### 4. Test Database Connection

**POST** `/api/files/test-connection`

Tests a database connection to ensure it's accessible. Validates connection string and returns detailed connection information.

**Request Body:**
```json
{
  "connectionString": "Server=(local);Database=MyDB;Integrated Security=True;",
  "timeoutSeconds": 30
}
```

**Example:**
```
POST http://localhost:8080/api/files/test-connection
Content-Type: application/json

{
  "connectionString": "Server=(local);Database=AdventureWorks;Integrated Security=True;",
  "timeoutSeconds": 30
}
```

**Response (Success):**
```json
{
  "connectionString": "Server=(local);Database=AdventureWorks;Integrated Security=True;",
  "isValid": true,
  "server": "(local)",
  "database": "AdventureWorks",
  "authenticationType": "Windows Authentication",
  "userId": null,
  "serverVersion": "Microsoft SQL Server 2019 (RTM) - 15.0.2000.5 (X64)...",
  "tableCount": 71,
  "message": "Connection successful",
  "errorCode": null,
  "errorDetails": null,
  "testedAt": "2025-01-15T10:30:00Z"
}
```

**Response (Failure):**
```json
{
  "connectionString": "Server=(local);Database=NonExistentDB;Integrated Security=True;",
  "isValid": false,
  "server": "(local)",
  "database": "NonExistentDB",
  "authenticationType": "Windows Authentication",
  "userId": null,
  "serverVersion": null,
  "tableCount": null,
  "message": "SQL Server error: Cannot open database 'NonExistentDB' requested by the login. The login failed.",
  "errorCode": 4060,
  "errorDetails": "System.Data.SqlClient.SqlException: Cannot open database...",
  "testedAt": "2025-01-15T10:30:00Z"
}
```

**Notes:**
- Validates connection string format and accessibility
- Returns server version, database name, authentication type, and table count
- Provides detailed error information if connection fails
- Default timeout is 30 seconds

### 5. List Databases (Security Auditing)

**POST** `/api/files/list-databases`

Lists all databases accessible with the given connection string for security auditing. Returns database information including permissions, ownership, and security-relevant details.

**Request Body:**
```json
{
  "connectionString": "Server=(local);Integrated Security=True;",
  "timeoutSeconds": 30
}
```

**Example:**
```
POST http://localhost:8080/api/files/list-databases
Content-Type: application/json

{
  "connectionString": "Server=(local);Integrated Security=True;",
  "timeoutSeconds": 30
}
```

**Response (Success):**
```json
{
  "connectionString": "Server=(local);Integrated Security=True;",
  "isValid": true,
  "server": "(local)",
  "authenticationType": "Windows Authentication",
  "userId": null,
  "databaseCount": 5,
  "message": "Successfully retrieved database list",
  "databases": [
    {
      "name": "master",
      "databaseId": 1,
      "state": "ONLINE",
      "recoveryModel": "SIMPLE",
      "collation": "SQL_Latin1_General_CP1_CI_AS",
      "createDate": "2003-04-08T09:10:00",
      "compatibilityLevel": 150,
      "owner": "sa",
      "canViewDefinition": true,
      "canConnect": true,
      "canCreateTable": false
    },
    {
      "name": "MyDatabase",
      "databaseId": 5,
      "state": "ONLINE",
      "recoveryModel": "FULL",
      "collation": "SQL_Latin1_General_CP1_CI_AS",
      "createDate": "2024-01-15T10:00:00",
      "compatibilityLevel": 150,
      "owner": "DOMAIN\\User",
      "canViewDefinition": true,
      "canConnect": true,
      "canCreateTable": true
    }
  ],
  "errorCode": null,
  "errorDetails": null,
  "listedAt": "2025-01-15T10:30:00Z"
}
```

**Notes:**
- Connects to master database to query all accessible databases
- Returns security-relevant information (permissions, ownership)
- Useful for security auditing and identifying over-privileged accounts
- Only lists ONLINE databases
- Default timeout is 30 seconds

### 6. List All Files

**GET** `/api/files`

**Response:**
```json
{
  "files": [
    {
      "filename": "MyDatabase.dacpac",
      "size": 1048576,
      "sizeFormatted": "1 MB",
      "lastModified": "2025-01-15T10:30:00",
      "downloadUrl": "http://localhost:8080/api/files/MyDatabase.dacpac"
    }
  ],
  "count": 1,
  "directory": "C:\\DacpacFiles"
}
```

## Usage Examples

### Using cURL

```bash
# Download a file
curl -O http://localhost:8080/api/files/MyDatabase.dacpac

# Check file status
curl http://localhost:8080/api/files/MyDatabase.dacpac/status

# List all files
curl http://localhost:8080/api/files
```

### Using PowerShell

```powershell
# Download a file
Invoke-WebRequest -Uri "http://localhost:8080/api/files/MyDatabase.dacpac" -OutFile "MyDatabase.dacpac"

# Check file status
Invoke-RestMethod -Uri "http://localhost:8080/api/files/MyDatabase.dacpac/status"

# List all files
Invoke-RestMethod -Uri "http://localhost:8080/api/files"

# Restore database
$restoreRequest = @{
    targetConnectionString = "Server=(local);Database=RestoredDB;Integrated Security=True;"
    targetDatabaseName = "RestoredDB"
    upgradeExisting = $false
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/files/MyDatabase.bacpac/restore" `
    -Method Post `
    -ContentType "application/json" `
    -Body $restoreRequest

# Test database connection
$testRequest = @{
    connectionString = "Server=(local);Database=MyDB;Integrated Security=True;"
    timeoutSeconds = 30
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/files/test-connection" `
    -Method Post `
    -ContentType "application/json" `
    -Body $testRequest

# List all accessible databases (security audit)
$listRequest = @{
    connectionString = "Server=(local);Integrated Security=True;"
    timeoutSeconds = 30
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/files/list-databases" `
    -Method Post `
    -ContentType "application/json" `
    -Body $listRequest
```

### Using C#

```csharp
using System.Net.Http;

var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:8080/");

// Download file
var fileBytes = await client.GetByteArrayAsync("api/files/MyDatabase.dacpac");
File.WriteAllBytes("MyDatabase.dacpac", fileBytes);

// Get status
var status = await client.GetStringAsync("api/files/MyDatabase.dacpac/status");

// List files
var files = await client.GetStringAsync("api/files");
```

## Security Notes

- The API sanitizes filenames to prevent directory traversal attacks
- Consider adding authentication/authorization for production use
- Ensure proper firewall rules are in place
- Use HTTPS in production environments

## Building and Running

1. **Restore NuGet packages:**
   ```powershell
   nuget restore FileDownloadApi\packages.config
   ```

2. **Build the project:**
   ```powershell
   msbuild FileDownloadApi\FileDownloadApi.csproj /t:Build
   ```

3. **Run in Visual Studio:**
   - Set `FileDownloadApi` as startup project
   - Press F5 to run (will start IIS Express)

4. **Deploy to IIS:**
   - Publish the project to IIS
   - Configure application pool to use .NET Framework 4.6.2
   - Set appropriate permissions on the files directory

## Integration with DatabaseExtractor

You can integrate this API with the `DatabaseExtractor` class library:

```csharp
using DatabaseExtractor;
using System.IO;

// Extract schema
var extractor = new DatabaseSchemaExtractor();
string outputPath = @"C:\DacpacFiles\MyDatabase.dacpac";
extractor.ExtractSchema(connectionString, outputPath);

// File is now available via API at:
// GET http://localhost:8080/api/files/MyDatabase.dacpac
```

