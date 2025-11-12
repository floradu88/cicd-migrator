# MCP Server for Database Schema Extraction

A Model Context Protocol (MCP) server that exposes database schema extraction and restore functionality as MCP tools.

## Overview

This MCP server provides tools for:
- Extracting database schemas to BACPAC files
- Restoring DACPAC/BACPAC files to databases
- Getting connection string examples

## Installation

1. Build the project:
   ```powershell
   msbuild McpServer\McpServer.csproj /t:Build
   ```

2. The server communicates via stdio (standard input/output) following the MCP protocol.

## Available Tools

### 1. extract_schema

Extracts database schema from SQL Server (local or Azure) to a BACPAC file.

**Parameters:**
- `connectionString` (required): SQL Server connection string
- `outputPath` (optional): Full path where BACPAC will be created (default: Documents/DacpacFiles)
- `extractTableData` (optional): Whether to extract data (default: false)

**Example Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "extract_schema",
    "arguments": {
      "connectionString": "Server=(local);Database=MyDatabase;Integrated Security=True;",
      "outputPath": "C:\\Output\\MyDatabase.bacpac"
    }
  }
}
```

**Example Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"success\": true,\n  \"message\": \"Schema extracted successfully\",\n  \"outputPath\": \"C:\\\\Output\\\\MyDatabase.bacpac\",\n  \"fileExists\": true,\n  \"fileSize\": 1048576\n}"
      }
    ]
  }
}
```

### 2. restore_database

Restores a DACPAC or BACPAC file to a target database.

**Parameters:**
- `packageFilePath` (required): Full path to DACPAC or BACPAC file
- `targetConnectionString` (required): Connection string to target server
- `targetDatabaseName` (required): Name of target database
- `upgradeExisting` (optional): Upgrade existing database (default: false)

**Example Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "restore_database",
    "arguments": {
      "packageFilePath": "C:\\Files\\MyDatabase.bacpac",
      "targetConnectionString": "Server=(local);Database=RestoredDB;Integrated Security=True;",
      "targetDatabaseName": "RestoredDB",
      "upgradeExisting": false
    }
  }
}
```

**Example Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"success\": true,\n  \"message\": \"Successfully restored MyDatabase.bacpac to database 'RestoredDB'\",\n  \"packageFilePath\": \"C:\\\\Files\\\\MyDatabase.bacpac\",\n  \"targetDatabaseName\": \"RestoredDB\",\n  \"upgradeExisting\": false\n}"
      }
    ]
  }
}
```

### 3. list_connection_examples

Returns example connection strings for various SQL Server configurations.

**Parameters:** None

**Example Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "list_connection_examples",
    "arguments": {}
  }
}
```

**Example Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\n  \"examples\": {\n    \"localWindowsAuth\": {\n      \"description\": \"Local SQL Server with Windows Authentication\",\n      \"connectionString\": \"Server=(local);Database=YourDatabase;Integrated Security=True;\",\n      \"usage\": \"Server=(local);Database=YourDatabase;Integrated Security=True;\"\n    },\n    \"localSqlAuth\": { ... },\n    \"localDbWindowsAuth\": { ... },\n    \"namedInstance\": { ... },\n    \"azureSql\": { ... }\n  }\n}"
      }
    ]
  }
}
```

## VS Code Integration

### Quick Setup

1. **Copy the configuration file:**
   - The workspace already includes `.vscode/settings.json` with MCP configuration
   - Or copy from `McpServer/vscode-mcp-config.json`

2. **Update the path** (if needed):
   ```json
   {
     "mcp.servers": {
       "database-extractor": {
         "command": "dotnet",
         "args": [
           "${workspaceFolder}/McpServer/bin/Debug/McpServer.exe"
         ],
         "env": {}
       }
     }
   }
   ```

3. **Restart VS Code**

4. **Verify:** Open Command Palette (`Ctrl+Shift+P`) and type "MCP"

### Configuration Files

- `.vscode/settings.json` - Workspace settings (ready to use)
- `McpServer/vscode-mcp-config.json` - VS Code MCP config example
- `McpServer/mcp-config.json` - General MCP config
- `McpServer/mcp-server-config.json` - Detailed config with tool descriptions

See [INTEGRATION.md](INTEGRATION.md) for detailed integration instructions for VS Code, Cursor, and Claude Desktop.

## Usage

### Starting the Server

The server reads from stdin and writes to stdout:

```bash
.\McpServer\bin\Debug\McpServer.exe
```

### MCP Client Configuration

Configure your MCP client (e.g., Claude Desktop, Cursor) to use this server:

**Claude Desktop** (`claude_desktop_config.json`):
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

**Cursor** (similar configuration):
```json
{
  "mcpServers": {
    "database-extractor": {
      "command": "D:\\code\\projects\\cicd-database\\McpServer\\bin\\Debug\\McpServer.exe"
    }
  }
}
```

## Complete Examples

See [EXAMPLES.md](EXAMPLES.md) for comprehensive examples of all methods.

## Protocol Flow

1. **Initialize**: Client sends `initialize` request
2. **List Tools**: Client calls `tools/list` to get available tools
3. **Call Tool**: Client calls `tools/call` with tool name and arguments
4. **Get Result**: Server returns result in JSON-RPC 2.0 format

## Error Handling

The server returns standard JSON-RPC 2.0 error responses:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": "Connection string cannot be null or empty."
  }
}
```

## Dependencies

- .NET Framework 4.8
- DatabaseExtractor class library
- Newtonsoft.Json (13.0.4)

## Notes

- The server uses stdio for communication (standard input/output)
- All communication is JSON-RPC 2.0 format
- Progress messages are written to stderr (won't interfere with JSON-RPC)
- Default output directory: `Documents/DacpacFiles`

## Quick Start

See [QUICK_START.md](QUICK_START.md) for a step-by-step setup guide.
