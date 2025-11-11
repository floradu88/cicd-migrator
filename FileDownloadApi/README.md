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

Downloads a DACPAC file.

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

Returns a list of all available DACPAC files.

**Example:**
```
GET http://localhost:8080/api/files
```

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

