# MCP Server Integration Guide

This guide shows how to integrate the Database Extractor MCP server with various editors and tools.

## VS Code Integration

### Option 1: Using VS Code Settings

1. Open VS Code settings (File → Preferences → Settings, or `Ctrl+,`)
2. Search for "MCP" or "Model Context Protocol"
3. Add the following configuration to your `settings.json`:

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

### Option 2: Using MCP Extension Configuration

If you're using a VS Code MCP extension, create or edit `.vscode/mcp.json`:

```json
{
  "mcpServers": {
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

### Option 3: Using Workspace Settings

Create `.vscode/settings.json` in your workspace:

```json
{
  "mcp.servers": {
    "database-extractor": {
      "command": "dotnet",
      "args": [
        "${workspaceFolder}\\McpServer\\bin\\Debug\\McpServer.exe"
      ],
      "env": {}
    }
  }
}
```

## Cursor Integration

### Using Cursor Settings

1. Open Cursor Settings
2. Navigate to MCP/Extensions section
3. Add the following configuration:

```json
{
  "mcpServers": {
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

Or add to Cursor's configuration file (typically in `%APPDATA%\Cursor\User\settings.json` or similar).

## Claude Desktop Integration

Add to `claude_desktop_config.json` (location varies by OS):

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
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

## Using Relative Paths

For portability, use relative paths from the workspace:

```json
{
  "mcpServers": {
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

## Using Full Executable Path (No dotnet)

If you want to run the executable directly:

```json
{
  "mcpServers": {
    "database-extractor": {
      "command": "D:\\code\\projects\\cicd-database\\McpServer\\bin\\Debug\\McpServer.exe",
      "args": [],
      "env": {}
    }
  }
}
```

## Environment Variables

You can set environment variables if needed:

```json
{
  "mcpServers": {
    "database-extractor": {
      "command": "dotnet",
      "args": [
        "D:\\code\\projects\\cicd-database\\McpServer\\bin\\Debug\\McpServer.exe"
      ],
      "env": {
        "DACPAC_OUTPUT_PATH": "C:\\DacpacFiles",
        "LOG_LEVEL": "INFO"
      }
    }
  }
}
```

## Verification

After configuration, you should be able to:

1. **List available tools:**
   - In VS Code/Cursor: Open MCP panel or command palette
   - Look for "database-extractor" server
   - See available tools: `extract_schema`, `restore_database`, `list_connection_examples`

2. **Test a tool call:**
   - Use the MCP client interface to call `extract_schema` or `restore_database`
   - Check that responses are received correctly

## Troubleshooting

### Server Not Starting

- Verify the path to `McpServer.exe` is correct
- Ensure the project has been built (`msbuild McpServer\McpServer.csproj`)
- Check that .NET Framework 4.8 is installed

### Tools Not Appearing

- Restart VS Code/Cursor after adding configuration
- Check the MCP server logs (usually in Output panel)
- Verify JSON syntax is valid

### Connection Errors

- Ensure the executable path uses correct path separators for your OS
- On Windows, use backslashes or forward slashes (both work)
- Check file permissions

## Example Usage in VS Code

Once configured, you can use the MCP tools in VS Code:

1. Open Command Palette (`Ctrl+Shift+P`)
2. Type "MCP" to see available MCP commands
3. Select a tool to invoke
4. Provide parameters when prompted

## Example Usage in Cursor

Similar to VS Code:

1. Use Cursor's MCP interface
2. Select the `database-extractor` server
3. Choose a tool (`extract_schema`, `restore_database`, etc.)
4. Fill in the required parameters

## Configuration File Locations

- **VS Code:** `.vscode/settings.json` (workspace) or User Settings
- **Cursor:** Similar to VS Code, check Cursor documentation
- **Claude Desktop:** `%APPDATA%\Claude\claude_desktop_config.json` (Windows)

## Quick Start

1. Build the MCP server:
   ```powershell
   msbuild McpServer\McpServer.csproj /t:Build
   ```

2. Copy the configuration to your editor's settings

3. Restart your editor

4. Verify the server appears in MCP tools list

