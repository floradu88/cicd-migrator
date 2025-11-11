# Database Schema Extraction Guide

This guide explains how to extract the database structure from your existing Azure SQL dev database.

## Prerequisites

1. **SqlPackage.exe** - Required tool for extracting DACPAC files
   - Comes with SQL Server Management Studio (SSMS)
   - Comes with SQL Server Data Tools (SSDT) for Visual Studio
   - Can be downloaded separately: [Download SqlPackage](https://aka.ms/sqlpackage)

2. **Access to Azure SQL Database**
   - Connection string or credentials for your dev database
   - Firewall rules allowing your IP address

## Quick Start

### Option 1: Using Connection String Directly

```powershell
.\scripts\Extract-DatabaseSchema.ps1 -ConnectionString "Server=tcp:yourserver.database.windows.net,1433;Database=yourdb;User ID=youruser;Password=yourpass;Encrypt=True;"
```

### Option 2: Using Individual Parameters

```powershell
.\scripts\Extract-DatabaseSchema.ps1 `
    -ServerName "yourserver.database.windows.net" `
    -DatabaseName "yourdb" `
    -UserName "youruser" `
    -Password "yourpass"
```

### Option 3: Using Configuration File (Recommended)

1. Copy the example config file:
   ```powershell
   Copy-Item scripts\config.example.ps1 scripts\config.ps1
   ```

2. Edit `scripts\config.ps1` with your actual connection strings

3. Run the extraction:
   ```powershell
   .\scripts\Extract-DatabaseSchema-FromConfig.ps1 -Environment Dev
   ```

## Output

The script will create a DACPAC file in the `./output` directory (or your specified path):
- `YourDatabaseName.dacpac` - Contains the complete database schema

## Next Steps

After extraction, you can:

1. **Import into Visual Studio SQL Database Project:**
   - Open Visual Studio
   - Create new SQL Server Database Project
   - Right-click project → Import → Data-tier Application (.dacpac)
   - Select your extracted DACPAC file

2. **Or use the import script** (if we create one):
   ```powershell
   .\scripts\Import-DacpacToProject.ps1 -DacpacPath .\output\YourDatabaseName.dacpac
   ```

## Troubleshooting

### SqlPackage.exe Not Found

If you get an error that SqlPackage.exe is not found:

1. **Install SSDT for Visual Studio:**
   - Download from: https://aka.ms/ssdt
   - Or install via Visual Studio Installer → Individual Components → SQL Server Data Tools

2. **Or provide the path manually:**
   ```powershell
   .\scripts\Extract-DatabaseSchema.ps1 `
       -ConnectionString "..." `
       -SqlPackagePath "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
   ```

### Connection Issues

- Verify your IP is allowed in Azure SQL firewall rules
- Check that the connection string is correct
- Ensure the database user has appropriate permissions (at least `db_datareader`)

### Firewall Access

If you need to add your IP to Azure SQL firewall:
```powershell
# Using Azure CLI
az sql server firewall-rule create `
    --resource-group YourResourceGroup `
    --server YourServerName `
    --name AllowMyIP `
    --start-ip-address YourPublicIP `
    --end-ip-address YourPublicIP
```

## Security Notes

- **Never commit `config.ps1` to source control** - it contains credentials
- Use Azure Key Vault for production environments
- Consider using Managed Identity or Service Principal authentication for CI/CD pipelines

