# Example Console Application

This is an example console application that demonstrates how to use the `DatabaseExtractor` class library.

## Prerequisites

- .NET Framework 4.6.2
- The `DatabaseExtractor` class library project must be built first
- Microsoft.SqlServer.DacFx NuGet package (restored automatically via project reference)

## Building

1. **Build the DatabaseExtractor class library first:**
   ```powershell
   msbuild DatabaseExtractor\DatabaseExtractor.csproj /t:Restore,Build
   ```

2. **Build this example application:**
   ```powershell
   msbuild ExampleConsoleApp\ExampleConsoleApp.csproj /t:Restore,Build
   ```

   Or in Visual Studio:
   - Right-click solution → Restore NuGet Packages
   - Build → Build Solution

## Running

1. Update the `connectionString` variable in `Program.cs` with your Azure SQL Database connection string
2. Uncomment the example you want to run
3. Run the application:
   ```powershell
   .\ExampleConsoleApp\bin\Debug\ExampleConsoleApp.exe
   ```

## Examples Included

1. **Basic Usage** - Simple schema extraction
2. **Custom Options** - Extraction with custom configuration
3. **Simplified Method** - Using the overloaded method signature
4. **Error Handling** - Comprehensive exception handling

## Connection String Format

```
Server=tcp:servername.database.windows.net,1433;Database=dbname;User ID=username;Password=password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Notes

- Ensure your IP is allowed in Azure SQL firewall rules
- The database user needs appropriate permissions (at least `db_datareader`)
- Output DACPAC files will be created in the specified path

