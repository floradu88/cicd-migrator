namespace DatabaseExtractor
{
    /// <summary>
    /// Connection string examples for various SQL Server configurations.
    /// </summary>
    public static class ConnectionStringExamples
    {
        /// <summary>
        /// Local SQL Server with Windows Authentication (Integrated Security)
        /// </summary>
        public const string LocalWindowsAuth = 
            "Server=(local);Database=YourDatabase;Integrated Security=True;";

        /// <summary>
        /// Local SQL Server with SQL Server Authentication
        /// </summary>
        public const string LocalSqlAuth = 
            "Server=(local);Database=YourDatabase;User Id=sa;Password=YourPassword;";

        /// <summary>
        /// LocalDB (SQL Server Express LocalDB) with Windows Authentication
        /// </summary>
        public const string LocalDbWindowsAuth = 
            "Server=(localdb)\\MSSQLLocalDB;Database=YourDatabase;Integrated Security=True;";

        /// <summary>
        /// LocalDB with SQL Server Authentication
        /// </summary>
        public const string LocalDbSqlAuth = 
            "Server=(localdb)\\MSSQLLocalDB;Database=YourDatabase;User Id=sa;Password=YourPassword;";

        /// <summary>
        /// Named SQL Server instance (e.g., SQLEXPRESS) with Windows Authentication
        /// </summary>
        public const string NamedInstanceWindowsAuth = 
            "Server=.\\SQLEXPRESS;Database=YourDatabase;Integrated Security=True;";

        /// <summary>
        /// Named SQL Server instance with SQL Server Authentication
        /// </summary>
        public const string NamedInstanceSqlAuth = 
            "Server=.\\SQLEXPRESS;Database=YourDatabase;User Id=sa;Password=YourPassword;";

        /// <summary>
        /// SQL Server on specific port with Windows Authentication
        /// </summary>
        public const string SpecificPortWindowsAuth = 
            "Server=localhost,1433;Database=YourDatabase;Integrated Security=True;";

        /// <summary>
        /// SQL Server on specific port with SQL Server Authentication
        /// </summary>
        public const string SpecificPortSqlAuth = 
            "Server=localhost,1433;Database=YourDatabase;User Id=sa;Password=YourPassword;";

        /// <summary>
        /// Azure SQL Database connection string
        /// </summary>
        public const string AzureSql = 
            "Server=tcp:yourserver.database.windows.net,1433;Database=YourDatabase;User ID=YourUser;Password=YourPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        /// <summary>
        /// Azure SQL Database with connection timeout
        /// </summary>
        public const string AzureSqlWithTimeout = 
            "Server=tcp:yourserver.database.windows.net,1433;Database=YourDatabase;User ID=YourUser;Password=YourPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;";

        /// <summary>
        /// Get example connection string for local database with Windows Authentication
        /// </summary>
        public static string GetLocalExample(string databaseName = "YourDatabase")
        {
            return $"Server=(local);Database={databaseName};Integrated Security=True;";
        }

        /// <summary>
        /// Get example connection string for LocalDB with Windows Authentication
        /// </summary>
        public static string GetLocalDbExample(string databaseName = "YourDatabase")
        {
            return $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;";
        }

        /// <summary>
        /// Get example connection string for Azure SQL Database
        /// </summary>
        public static string GetAzureExample(string serverName, string databaseName, string userId, string password)
        {
            return $"Server=tcp:{serverName}.database.windows.net,1433;Database={databaseName};User ID={userId};Password={password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }
    }
}

