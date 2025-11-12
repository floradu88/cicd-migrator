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

## Example 11: Test Database Connection

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "method": "tools/call",
  "params": {
    "name": "test_connection",
    "arguments": {
      "connectionString": "Server=(local);Database=AdventureWorks;Integrated Security=True;",
      "timeoutSeconds": 30
    }
  }
}
```

**Response (Success):**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"isValid\": true,\n  \"message\": \"Connection successful\",\n  \"server\": \"(local)\",\n  \"database\": \"AdventureWorks\",\n  \"authenticationType\": \"Windows Authentication\",\n  \"userId\": null,\n  \"serverVersion\": \"Microsoft SQL Server 2019 (RTM) - 15.0.2000.5 (X64)...\",\n  \"tableCount\": 71,\n  \"errorCode\": null,\n  \"errorDetails\": null,\n  \"testedAt\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

**Response (Failure):**
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"isValid\": false,\n  \"message\": \"SQL Server error: Cannot open database 'AdventureWorks' requested by the login. The login failed.\",\n  \"server\": \"(local)\",\n  \"database\": \"AdventureWorks\",\n  \"authenticationType\": \"Windows Authentication\",\n  \"userId\": null,\n  \"serverVersion\": null,\n  \"tableCount\": null,\n  \"errorCode\": 4060,\n  \"errorDetails\": \"System.Data.SqlClient.SqlException: Cannot open database...\",\n  \"testedAt\": \"2024-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

## Example 12: Test Azure SQL Database Connection

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "method": "tools/call",
  "params": {
    "name": "test_connection",
    "arguments": {
      "connectionString": "Server=tcp:myserver.database.windows.net,1433;Database=MyDB;User ID=myuser;Password=mypass;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
      "timeoutSeconds": 30
    }
  }
}
```

## Example 13: List All Databases (Security Auditing)

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 13,
  "method": "tools/call",
  "params": {
    "name": "list_databases",
    "arguments": {
      "connectionString": "Server=(local);Integrated Security=True;",
      "timeoutSeconds": 30
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 13,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"isValid\": true,\n  \"message\": \"Successfully retrieved database list\",\n  \"server\": \"(local)\",\n  \"authenticationType\": \"Windows Authentication\",\n  \"userId\": null,\n  \"databaseCount\": 5,\n  \"databases\": [\n    {\n      \"name\": \"master\",\n      \"databaseId\": 1,\n      \"state\": \"ONLINE\",\n      \"recoveryModel\": \"SIMPLE\",\n      \"collation\": \"SQL_Latin1_General_CP1_CI_AS\",\n      \"createDate\": \"2003-04-08T09:10:00\",\n      \"compatibilityLevel\": 150,\n      \"owner\": \"sa\",\n      \"canViewDefinition\": true,\n      \"canConnect\": true,\n      \"canCreateTable\": false\n    },\n    {\n      \"name\": \"MyDatabase\",\n      \"databaseId\": 5,\n      \"state\": \"ONLINE\",\n      \"recoveryModel\": \"FULL\",\n      \"collation\": \"SQL_Latin1_General_CP1_CI_AS\",\n      \"createDate\": \"2024-01-15T10:00:00\",\n      \"compatibilityLevel\": 150,\n      \"owner\": \"DOMAIN\\\\User\",\n      \"canViewDefinition\": true,\n      \"canConnect\": true,\n      \"canCreateTable\": true\n    }\n  ],\n  \"errorCode\": null,\n  \"errorDetails\": null,\n  \"listedAt\": \"2025-01-15T10:30:00Z\"\n}"
      }
    ]
  }
}
```

**Use Cases:**
- Security auditing: Identify all databases accessible with given credentials
- Permission verification: Check what permissions the account has on each database
- Compliance: Verify principle of least privilege
- Access review: Identify over-privileged accounts

## Example 14: Error Handling - Missing Required Parameter

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

