# MCP Server Quick Start Guide

## 1. Build the MCP Server

```powershell
msbuild McpServer\McpServer.csproj /t:Build
```

Or build the entire solution:
```powershell
msbuild DatabaseExtractor.sln /t:Build
```

## 2. VS Code Integration

### Option A: Workspace Settings (Recommended)

Copy `.vscode/settings.json` from the root directory, or create it with:

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

### Option B: User Settings

Add to your VS Code user settings (`File → Preferences → Settings → Open Settings (JSON)`):

```json
{
  "mcp.servers": {
    "database-extractor": {
      "command": "dotnet",
      "args": [
        "D:\\code\\projects\\cicd-database\\McpServer\\bin\\Debug\\McpServer.exe"
      ],
      "env": {}
    }
  }
}
```

**Note:** Update the path to match your actual project location.

## 3. Restart VS Code

After adding the configuration, restart VS Code to load the MCP server.

## 4. Verify Integration

1. Open Command Palette (`Ctrl+Shift+P`)
2. Type "MCP" to see available MCP commands
3. You should see `database-extractor` server listed
4. Available tools:
   - `extract_schema` - Extract database schema to BACPAC
   - `restore_database` - Restore DACPAC/BACPAC to database
   - `list_connection_examples` - Get connection string examples

## 5. Test the Tools

### Extract Schema Example

Use the MCP interface to call:
- Tool: `extract_schema`
- Parameters:
  ```json
  {
    "connectionString": "Server=(local);Database=MyDatabase;Integrated Security=True;",
    "outputPath": "C:\\Backups\\MyDatabase.bacpac"
  }
  ```

### Restore Database Example

Use the MCP interface to call:
- Tool: `restore_database`
- Parameters:
  ```json
  {
    "packageFilePath": "C:\\Backups\\MyDatabase.bacpac",
    "targetConnectionString": "Server=(local);Database=RestoredDB;Integrated Security=True;",
    "targetDatabaseName": "RestoredDB",
    "upgradeExisting": false
  }
  ```

## Configuration Files Provided

- `.vscode/settings.json` - VS Code workspace settings (ready to use)
- `McpServer/vscode-mcp-config.json` - VS Code MCP config example
- `McpServer/mcp-config.json` - General MCP config
- `McpServer/mcp-server-config.json` - Detailed MCP server config with tool descriptions

## Troubleshooting

- **Server not appearing:** Check that the path in settings.json is correct
- **Build errors:** Ensure .NET Framework 4.8 is installed
- **Tools not working:** Verify the MCP server executable exists at the specified path

