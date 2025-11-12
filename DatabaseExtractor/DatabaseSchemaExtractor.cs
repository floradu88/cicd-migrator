using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace DatabaseExtractor
{
    /// <summary>
    /// Extracts database schema from SQL Server (local or Azure SQL Database) to a DACPAC file.
    /// </summary>
    public class DatabaseSchemaExtractor
    {
        /// <summary>
        /// Tests the database connection to ensure it's accessible.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string to test</param>
        /// <param name="timeoutSeconds">Connection timeout in seconds (default: 30)</param>
        /// <returns>Connection test result with details</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty</exception>
        public ConnectionTestResult TestConnection(string connectionString, int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            var result = new ConnectionTestResult
            {
                ConnectionString = connectionString,
                IsValid = false,
                TestedAt = DateTime.UtcNow
            };

            try
            {
                // Parse connection string to extract details
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                result.Server = builder.DataSource;
                result.Database = builder.InitialCatalog;
                result.AuthenticationType = builder.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication";
                result.UserId = builder.UserID;

                // Test connection
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    result.IsValid = true;
                    result.Message = "Connection successful";
                    
                    // Get server version
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        result.ServerVersion = command.ExecuteScalar()?.ToString();
                    }

                    // Get database name if not specified
                    if (string.IsNullOrWhiteSpace(result.Database))
                    {
                        using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                        {
                            result.Database = command.ExecuteScalar()?.ToString();
                        }
                    }

                    // Test database access
                    if (!string.IsNullOrWhiteSpace(result.Database))
                    {
                        using (var command = new SqlCommand($"SELECT COUNT(*) FROM sys.tables", connection))
                        {
                            command.CommandTimeout = timeoutSeconds;
                            var tableCount = command.ExecuteScalar();
                            result.TableCount = tableCount != null ? Convert.ToInt32(tableCount) : 0;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                result.IsValid = false;
                result.Message = $"SQL Server error: {ex.Message}";
                result.ErrorCode = ex.Number;
                result.ErrorDetails = ex.ToString();
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Connection failed: {ex.Message}";
                result.ErrorDetails = ex.ToString();
            }

            return result;
        }

        /// <summary>
        /// Lists all databases accessible with the given connection string for security auditing.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string (will connect to master database or server-level)</param>
        /// <param name="timeoutSeconds">Query timeout in seconds (default: 30)</param>
        /// <returns>List of accessible databases with security-relevant information</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty</exception>
        public DatabaseListResult ListDatabases(string connectionString, int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            var result = new DatabaseListResult
            {
                ConnectionString = connectionString,
                ListedAt = DateTime.UtcNow,
                Databases = new List<DatabaseInfo>()
            };

            try
            {
                // Parse connection string to extract details
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                result.Server = builder.DataSource;
                result.AuthenticationType = builder.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication";
                result.UserId = builder.UserID;

                // Create a connection string that connects to master database (or remove database specification)
                // This allows us to query sys.databases
                var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master" // Connect to master to query all databases
                };

                using (var connection = new SqlConnection(masterConnectionString.ConnectionString))
                {
                    connection.Open();
                    result.IsValid = true;
                    result.Message = "Successfully retrieved database list";

                    // Query to get all accessible databases with security information
                    string query = @"
                        SELECT 
                            d.name AS DatabaseName,
                            d.database_id AS DatabaseId,
                            d.state_desc AS State,
                            d.recovery_model_desc AS RecoveryModel,
                            d.collation_name AS Collation,
                            d.create_date AS CreateDate,
                            d.compatibility_level AS CompatibilityLevel,
                            SUSER_SNAME(d.owner_sid) AS Owner,
                            CASE 
                                WHEN HAS_PERMS_BY_NAME(d.name, 'DATABASE', 'VIEW DEFINITION') = 1 THEN 1 
                                ELSE 0 
                            END AS CanViewDefinition,
                            CASE 
                                WHEN HAS_PERMS_BY_NAME(d.name, 'DATABASE', 'CONNECT') = 1 THEN 1 
                                ELSE 0 
                            END AS CanConnect,
                            CASE 
                                WHEN HAS_PERMS_BY_NAME(d.name, 'DATABASE', 'CREATE TABLE') = 1 THEN 1 
                                ELSE 0 
                            END AS CanCreateTable
                        FROM sys.databases d
                        WHERE d.state_desc = 'ONLINE'
                        ORDER BY d.name";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = timeoutSeconds;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dbInfo = new DatabaseInfo
                                {
                                    Name = reader["DatabaseName"]?.ToString(),
                                    DatabaseId = reader["DatabaseId"] != DBNull.Value ? Convert.ToInt32(reader["DatabaseId"]) : (int?)null,
                                    State = reader["State"]?.ToString(),
                                    RecoveryModel = reader["RecoveryModel"]?.ToString(),
                                    Collation = reader["Collation"]?.ToString(),
                                    CreateDate = reader["CreateDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreateDate"]) : (DateTime?)null,
                                    CompatibilityLevel = reader["CompatibilityLevel"] != DBNull.Value ? Convert.ToInt32(reader["CompatibilityLevel"]) : (int?)null,
                                    Owner = reader["Owner"]?.ToString(),
                                    CanViewDefinition = reader["CanViewDefinition"] != DBNull.Value && Convert.ToBoolean(reader["CanViewDefinition"]),
                                    CanConnect = reader["CanConnect"] != DBNull.Value && Convert.ToBoolean(reader["CanConnect"]),
                                    CanCreateTable = reader["CanCreateTable"] != DBNull.Value && Convert.ToBoolean(reader["CanCreateTable"])
                                };

                                result.Databases.Add(dbInfo);
                            }
                        }
                    }

                    result.DatabaseCount = result.Databases.Count;
                }
            }
            catch (SqlException ex)
            {
                result.IsValid = false;
                result.Message = $"SQL Server error while listing databases: {ex.Message}";
                result.ErrorCode = ex.Number;
                result.ErrorDetails = ex.ToString();
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Error listing databases: {ex.Message}";
                result.ErrorDetails = ex.ToString();
            }

            return result;
        }

        /// <summary>
        /// Extracts the database schema from the specified connection string to a DACPAC file.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string (local or Azure SQL Database)</param>
        /// <param name="outputDacpacPath">Full path where the DACPAC file will be created</param>
        /// <param name="extractOptions">Optional extraction options. If null, default options will be used.</param>
        /// <param name="validateConnection">Whether to validate connection before extraction (default: true)</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString or outputDacpacPath is null or empty</exception>
        /// <exception cref="DacServicesException">Thrown when extraction fails</exception>
        public bool ExtractSchema(string connectionString, string outputDacpacPath, ExtractOptions extractOptions = null, bool validateConnection = true)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(outputDacpacPath))
            {
                throw new ArgumentNullException(nameof(outputDacpacPath), "Output DACPAC path cannot be null or empty.");
            }

            // Ensure output directory exists
            string outputDirectory = Path.GetDirectoryName(outputDacpacPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Use default options if not provided
            if (extractOptions == null)
            {
                extractOptions = new ExtractOptions
                {
                    ExtractAllTableData = false,
                    IgnoreExtendedProperties = false,
                    VerifyExtraction = true
                };
            }

            try
            {
                // Validate connection before proceeding
                if (validateConnection)
                {
                    var connectionTest = TestConnection(connectionString);
                    if (!connectionTest.IsValid)
                    {
                        throw new Exception($"Database connection validation failed: {connectionTest.Message}");
                    }
                    Console.WriteLine($"Connection validated successfully. Server: {connectionTest.Server}, Database: {connectionTest.Database}");
                }

                // Extract database name from connection string
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw new ArgumentException("Database name not found in connection string. Ensure 'Database' or 'Initial Catalog' is specified.");
                }

                // Create DacServices instance with connection string
                var dacServices = new DacServices(connectionString);
                
                // Set progress event handler
                dacServices.Message += OnMessage;
                dacServices.ProgressChanged += OnProgressChanged;

                // Export the database to BACPAC (includes schema and data)
                // Parameters: packageFilePath, databaseName
                dacServices.ExportBacpac(outputDacpacPath, databaseName);

                return true;
            }
            catch (DacServicesException ex)
            {
                throw new DacServicesException($"Failed to extract database schema: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error during schema extraction: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts the database schema with custom extraction settings.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string (local or Azure SQL Database)</param>
        /// <param name="outputDacpacPath">Full path where the DACPAC file will be created</param>
        /// <param name="extractTableData">Whether to extract table data along with schema</param>
        /// <param name="ignoreExtendedProperties">Whether to ignore extended properties</param>
        /// <param name="verifyExtraction">Whether to verify the extraction</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractSchema(
            string connectionString,
            string outputDacpacPath,
            bool extractTableData = false,
            bool ignoreExtendedProperties = false,
            bool verifyExtraction = true)
        {
            var extractOptions = new ExtractOptions
            {
                ExtractAllTableData = extractTableData,
                IgnoreExtendedProperties = ignoreExtendedProperties,
                VerifyExtraction = verifyExtraction
            };

            return ExtractSchema(connectionString, outputDacpacPath, extractOptions);
        }

        /// <summary>
        /// Event handler for extraction messages.
        /// </summary>
        private void OnMessage(object sender, DacMessageEventArgs e)
        {
            // Log or handle messages as needed
            // You can customize this to write to a log file or console
            Console.WriteLine($"[{e.Message.MessageType}] {e.Message.Message}");
        }

        /// <summary>
        /// Event handler for extraction progress updates.
        /// </summary>
        private void OnProgressChanged(object sender, DacProgressEventArgs e)
        {
            // Log or handle progress as needed
            // You can customize this to update a progress bar or log
            Console.WriteLine($"Progress: {e.Status} - {e.Message}");
        }

        /// <summary>
        /// Restores a DACPAC or BACPAC file to a database.
        /// </summary>
        /// <param name="packageFilePath">Full path to the DACPAC or BACPAC file</param>
        /// <param name="targetConnectionString">Connection string to the target database</param>
        /// <param name="targetDatabaseName">Name of the target database (will be created if it doesn't exist)</param>
        /// <param name="upgradeExisting">Whether to upgrade existing database (true) or create new (false)</param>
        /// <param name="validateConnection">Whether to validate connection before restore (default: true)</param>
        /// <returns>True if restore succeeded, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null or empty</exception>
        /// <exception cref="DacServicesException">Thrown when restore fails</exception>
        public bool RestoreDatabase(string packageFilePath, string targetConnectionString, string targetDatabaseName, bool upgradeExisting = false, bool validateConnection = true)
        {
            if (string.IsNullOrWhiteSpace(packageFilePath))
            {
                throw new ArgumentNullException(nameof(packageFilePath), "Package file path cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(targetConnectionString))
            {
                throw new ArgumentNullException(nameof(targetConnectionString), "Target connection string cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(targetDatabaseName))
            {
                throw new ArgumentNullException(nameof(targetDatabaseName), "Target database name cannot be null or empty.");
            }

            if (!File.Exists(packageFilePath))
            {
                throw new FileNotFoundException($"Package file not found: {packageFilePath}");
            }

            try
            {
                // Validate connection before proceeding
                if (validateConnection)
                {
                    var connectionTest = TestConnection(targetConnectionString);
                    if (!connectionTest.IsValid)
                    {
                        throw new Exception($"Target database connection validation failed: {connectionTest.Message}");
                    }
                    Console.WriteLine($"Target connection validated successfully. Server: {connectionTest.Server}");
                }

                // Determine file type by extension
                string extension = Path.GetExtension(packageFilePath).ToLowerInvariant();
                bool isBacpac = extension == ".bacpac";

                // Create DacServices instance with target connection string
                var dacServices = new DacServices(targetConnectionString);
                
                // Set progress event handler
                dacServices.Message += OnMessage;
                dacServices.ProgressChanged += OnProgressChanged;

                if (isBacpac)
                {
                    // Import BACPAC file
                    var bacPackage = BacPackage.Load(packageFilePath);
                    dacServices.ImportBacpac(bacPackage, targetDatabaseName);
                }
                else
                {
                    // Deploy DACPAC file
                    var dacPackage = DacPackage.Load(packageFilePath);
                    var deployOptions = new DacDeployOptions
                    {
                        CreateNewDatabase = !upgradeExisting
                    };
                    
                    dacServices.Deploy(dacPackage, targetDatabaseName, upgradeExisting, deployOptions);
                }

                return true;
            }
            catch (DacServicesException ex)
            {
                throw new DacServicesException($"Failed to restore database: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error during database restore: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Result of a database connection test.
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// The connection string that was tested.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Whether the connection was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Server name from connection string.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Database name from connection string or current database.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Authentication type used (Windows Authentication or SQL Server Authentication).
        /// </summary>
        public string AuthenticationType { get; set; }

        /// <summary>
        /// User ID from connection string (if SQL Server Authentication).
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// SQL Server version information.
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// Number of tables in the database (if accessible).
        /// </summary>
        public int? TableCount { get; set; }

        /// <summary>
        /// Test result message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// SQL Server error code (if connection failed).
        /// </summary>
        public int? ErrorCode { get; set; }

        /// <summary>
        /// Detailed error information (if connection failed).
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Timestamp when the connection test was performed.
        /// </summary>
        public DateTime TestedAt { get; set; }
    }

    /// <summary>
    /// Result of listing databases accessible with a connection string.
    /// </summary>
    public class DatabaseListResult
    {
        /// <summary>
        /// The connection string that was used.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Server name from connection string.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Authentication type used (Windows Authentication or SQL Server Authentication).
        /// </summary>
        public string AuthenticationType { get; set; }

        /// <summary>
        /// User ID from connection string (if SQL Server Authentication).
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// List of accessible databases.
        /// </summary>
        public List<DatabaseInfo> Databases { get; set; }

        /// <summary>
        /// Total number of databases found.
        /// </summary>
        public int DatabaseCount { get; set; }

        /// <summary>
        /// Operation result message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// SQL Server error code (if operation failed).
        /// </summary>
        public int? ErrorCode { get; set; }

        /// <summary>
        /// Detailed error information (if operation failed).
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Timestamp when the database list was retrieved.
        /// </summary>
        public DateTime ListedAt { get; set; }
    }

    /// <summary>
    /// Information about a single database.
    /// </summary>
    public class DatabaseInfo
    {
        /// <summary>
        /// Database name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Database ID.
        /// </summary>
        public int? DatabaseId { get; set; }

        /// <summary>
        /// Database state (e.g., ONLINE, OFFLINE).
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Recovery model (FULL, SIMPLE, BULK_LOGGED).
        /// </summary>
        public string RecoveryModel { get; set; }

        /// <summary>
        /// Database collation.
        /// </summary>
        public string Collation { get; set; }

        /// <summary>
        /// Database creation date.
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// SQL Server compatibility level.
        /// </summary>
        public int? CompatibilityLevel { get; set; }

        /// <summary>
        /// Database owner.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Whether the current user can view database definition.
        /// </summary>
        public bool CanViewDefinition { get; set; }

        /// <summary>
        /// Whether the current user can connect to the database.
        /// </summary>
        public bool CanConnect { get; set; }

        /// <summary>
        /// Whether the current user can create tables in the database.
        /// </summary>
        public bool CanCreateTable { get; set; }
    }
}

