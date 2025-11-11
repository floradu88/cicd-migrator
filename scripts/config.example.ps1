<#
.SYNOPSIS
    Example configuration file for database connection strings.
    
.DESCRIPTION
    Copy this file to config.ps1 and update with your actual connection strings.
    DO NOT commit config.ps1 to source control - it should be in .gitignore.
#>

# Azure SQL Database Connection Strings
# Format: "Server=tcp:servername.database.windows.net,1433;Database=dbname;User ID=username;Password=password;Encrypt=True;"

$Script:DatabaseConnections = @{
    Dev = "Server=tcp:your-dev-server.database.windows.net,1433;Database=YourDevDatabase;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    
    CI = "Server=tcp:your-ci-server.database.windows.net,1433;Database=YourCIDatabase;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    
    Staging = "Server=tcp:your-staging-server.database.windows.net,1433;Database=YourStagingDatabase;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    
    Production = "Server=tcp:your-prod-server.database.windows.net,1433;Database=YourProdDatabase;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

# Alternative: Use individual parameters
$Script:DatabaseConfig = @{
    Dev = @{
        ServerName = "your-dev-server.database.windows.net"
        DatabaseName = "YourDevDatabase"
        UserName = "your-user"
        Password = "your-password"
    }
    CI = @{
        ServerName = "your-ci-server.database.windows.net"
        DatabaseName = "YourCIDatabase"
        UserName = "your-user"
        Password = "your-password"
    }
    # Add more environments as needed
}

