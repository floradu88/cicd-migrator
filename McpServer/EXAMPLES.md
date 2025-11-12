# MCP Server Usage Examples

Complete examples for all MCP tools and methods.

## Prerequisites

1. Build the MCP server:
   ```powershell
   msbuild McpServer\McpServer.csproj /t:Build
   ```

2. Start the server (it will read from stdin):
   ```bash
   .\McpServer\bin\Debug\McpServer.exe
   ```

## Example 1: Initialize MCP Server

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "example-client",
      "version": "1.0.0"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {}
    },
    "serverInfo": {
      "name": "database-extractor-mcp-server",
      "version": "1.0.0"
    }
  }
}
```

## Example 2: List Available Tools

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "extract_schema",
        "description": "Extracts database schema from SQL Server (local or Azure) to a BACPAC file...",
        "inputSchema": { ... }
      },
      {
        "name": "restore_database",
        "description": "Restores a DACPAC or BACPAC file to a target database...",
        "inputSchema": { ... }
      },
      {
        "name": "list_connection_examples",
        "description": "Returns example connection strings...",
        "inputSchema": { ... }
      }
    ]
  }
}
```

## Example 3: Extract Schema from Local SQL Server

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "extract_schema",
    "arguments": {
      "connectionString": "Server=(local);Database=AdventureWorks;Integrated Security=True;",
      "outputPath": "C:\\Backups\\AdventureWorks.bacpac"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"success\": true,\n  \"message\": \"Schema extracted successfully\",\n  \"outputPath\": \"C:\\\\Backups\\\\AdventureWorks.bacpac\",\n  \"fileExists\": true,\n  \"fileSize\": 5242880\n}"
      }
    ]
  }
}
```

## Example 4: Extract Schema from LocalDB

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "extract_schema",
    "arguments": {
      "connectionString": "Server=(localdb)\\MSSQLLocalDB;Database=MyLocalDB;Integrated Security=True;",
      "outputPath": "C:\\Backups\\MyLocalDB.bacpac"
    }
  }
}
```

## Example 5: Extract Schema from Azure SQL Database

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "extract_schema",
    "arguments": {
      "connectionString": "Server=tcp:myserver.database.windows.net,1433;Database=mydb;User ID=myuser;Password=MySecurePassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
      "outputPath": "C:\\Backups\\AzureDB.bacpac"
    }
  }
}
```

## Example 6: Extract Schema with Default Output Path

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tools/call",
  "params": {
    "name": "extract_schema",
    "arguments": {
      "connectionString": "Server=(local);Database=MyDatabase;Integrated Security=True;"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"success\": true,\n  \"message\": \"Schema extracted successfully\",\n  \"outputPath\": \"C:\\\\Users\\\\YourName\\\\Documents\\\\DacpacFiles\\\\MyDatabase.bacpac\",\n  \"fileExists\": true,\n  \"fileSize\": 2097152\n}"
      }
    ]
  }
}
```

## Example 7: Restore BACPAC to New Database

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "restore_database",
    "arguments": {
      "packageFilePath": "C:\\Backups\\AdventureWorks.bacpac",
      "targetConnectionString": "Server=(local);Database=AdventureWorks_Restored;Integrated Security=True;",
      "targetDatabaseName": "AdventureWorks_Restored",
      "upgradeExisting": false
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"success\": true,\n  \"message\": \"Successfully restored AdventureWorks.bacpac to database 'AdventureWorks_Restored'\",\n  \"packageFilePath\": \"C:\\\\Backups\\\\AdventureWorks.bacpac\",\n  \"targetDatabaseName\": \"AdventureWorks_Restored\",\n  \"upgradeExisting\": false\n}"
      }
    ]
  }
}
```

## Example 8: Restore DACPAC and Upgrade Existing Database

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "tools/call",
  "params": {
    "name": "restore_database",
    "arguments": {
      "packageFilePath": "C:\\Backups\\MyDatabase.dacpac",
      "targetConnectionString": "Server=(local);Database=MyDatabase;Integrated Security=True;",
      "targetDatabaseName": "MyDatabase",
      "upgradeExisting": true
    }
  }
}
```

## Example 9: Restore to Azure SQL Database

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "tools/call",
  "params": {
    "name": "restore_database",
    "arguments": {
      "packageFilePath": "C:\\Backups\\ProductionDB.bacpac",
      "targetConnectionString": "Server=tcp:targetserver.database.windows.net,1433;Database=StagingDB;User ID=admin;Password=SecurePass123!;Encrypt=True;",
      "targetDatabaseName": "StagingDB",
      "upgradeExisting": false
    }
  }
}
```

## Example 10: Get Connection String Examples

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "method": "tools/call",
  "params": {
    "name": "list_connection_examples",
    "arguments": {}
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"examples\": {\n    \"localWindowsAuth\": {\n      \"description\": \"Local SQL Server with Windows Authentication\",\n      \"connectionString\": \"Server=(local);Database=YourDatabase;Integrated Security=True;\",\n      \"usage\": \"Server=(local);Database=YourDatabase;Integrated Security=True;\"\n    },\n    \"localSqlAuth\": {\n      \"description\": \"Local SQL Server with SQL Server Authentication\",\n      \"connectionString\": \"Server=(local);Database=YourDatabase;User Id=sa;Password=YourPassword;\",\n      \"usage\": \"Server=(local);Database=YourDatabase;User Id=sa;Password=YourPassword;\"\n    },\n    \"localDbWindowsAuth\": {\n      \"description\": \"LocalDB with Windows Authentication\",\n      \"connectionString\": \"Server=(localdb)\\\\MSSQLLocalDB;Database=YourDatabase;Integrated Security=True;\",\n      \"usage\": \"Server=(localdb)\\\\MSSQLLocalDB;Database=YourDatabase;Integrated Security=True;\"\n    },\n    \"namedInstance\": {\n      \"description\": \"Named SQL Server instance (e.g., SQLEXPRESS)\",\n      \"connectionString\": \"Server=.\\\\SQLEXPRESS;Database=YourDatabase;Integrated Security=True;\",\n      \"usage\": \"Server=.\\\\SQLEXPRESS;Database=YourDatabase;Integrated Security=True;\"\n    },\n    \"azureSql\": {\n      \"description\": \"Azure SQL Database\",\n      \"connectionString\": \"Server=tcp:yourserver.database.windows.net,1433;Database=YourDatabase;User ID=YourUser;Password=YourPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;\",\n      \"usage\": \"Server=tcp:yourserver.database.windows.net,1433;Database=YourDatabase;User ID=YourUser;Password=YourPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;\"\n    }\n  }\n}"
      }
    ]
  }
}
```

## Example 11: Error Handling - Missing Required Parameter

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "method": "tools/call",
  "params": {
    "name": "extract_schema",
    "arguments": {}
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "error": {
    "code": -32603,
    "message": "Tool execution failed",
    "data": "connectionString is required"
  }
}
```

## Example 12: Error Handling - File Not Found

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "method": "tools/call",
  "params": {
    "name": "restore_database",
    "arguments": {
      "packageFilePath": "C:\\NonExistent\\File.bacpac",
      "targetConnectionString": "Server=(local);Database=TestDB;Integrated Security=True;",
      "targetDatabaseName": "TestDB"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "error": {
    "code": -32603,
    "message": "Tool execution failed",
    "data": "Package file not found: C:\\NonExistent\\File.bacpac"
  }
}
```

## Testing with PowerShell

You can test the MCP server using PowerShell:

```powershell
# Start the server in background
$server = Start-Process -FilePath ".\McpServer\bin\Debug\McpServer.exe" `
    -RedirectStandardInput -RedirectStandardOutput -RedirectStandardError `
    -NoNewWindow -PassThru

# Send initialize request
$initRequest = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{}
        clientInfo = @{
            name = "test-client"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

# Send request and read response
$initRequest | Out-File -FilePath "request.json" -Encoding utf8
# (You would pipe this to the server's stdin in a real scenario)
```

## Integration with MCP Clients

### Claude Desktop

Add to `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "database-extractor": {
      "command": "dotnet",
      "args": [
        "D:\\code\\projects\\cicd-database\\McpServer\\bin\\Debug\\McpServer.exe"
      ]
    }
  }
}
```

### Cursor

Similar configuration in Cursor's MCP settings.

## Notes

- All requests must be valid JSON-RPC 2.0 format
- The server reads from stdin line by line
- Responses are written to stdout
- Error messages go to stderr
- Each request must have a unique `id` field

