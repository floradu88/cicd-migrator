using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace DatabaseExtractor.Providers
{
    /// <summary>
    /// SQL Server database provider implementation using DacFx.
    /// </summary>
    public class SqlServerDatabaseProvider : IDatabaseProvider
    {
        public string ProviderType => "SqlServer";
        public string DefaultFileExtension => ".bacpac";

        public bool CanHandleConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // Check for SQL Server connection string indicators
            var lower = connectionString.ToLowerInvariant();
            return lower.Contains("server=") || 
                   lower.Contains("data source=") || 
                   lower.Contains("initial catalog=") ||
                   lower.Contains("database=") ||
                   lower.Contains("sqlconnection") ||
                   lower.Contains("(local)") ||
                   lower.Contains("localhost") ||
                   lower.Contains(".database.windows.net"); // Azure SQL
        }

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
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                result.Server = builder.DataSource;
                result.Database = builder.InitialCatalog;
                result.AuthenticationType = builder.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication";
                result.UserId = builder.UserID;

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    result.IsValid = true;
                    result.Message = "Connection successful";
                    
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        result.ServerVersion = command.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(result.Database))
                    {
                        using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                        {
                            result.Database = command.ExecuteScalar()?.ToString();
                        }
                    }

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
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                result.Server = builder.DataSource;
                result.AuthenticationType = builder.IntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication";
                result.UserId = builder.UserID;

                var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                using (var connection = new SqlConnection(masterConnectionString.ConnectionString))
                {
                    connection.Open();
                    result.IsValid = true;
                    result.Message = "Successfully retrieved database list";

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

        public bool ExtractSchema(string connectionString, string outputFilePath, ExtractOptions extractOptions = null, bool validateConnection = true)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                throw new ArgumentNullException(nameof(outputFilePath), "Output file path cannot be null or empty.");
            }

            string outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

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
                if (validateConnection)
                {
                    var connectionTest = TestConnection(connectionString);
                    if (!connectionTest.IsValid)
                    {
                        throw new Exception($"Database connection validation failed: {connectionTest.Message}");
                    }
                    Console.WriteLine($"Connection validated successfully. Server: {connectionTest.Server}, Database: {connectionTest.Database}");
                }

                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw new ArgumentException("Database name not found in connection string. Ensure 'Database' or 'Initial Catalog' is specified.");
                }

                var dacServices = new DacServices(connectionString);
                dacServices.Message += OnMessage;
                dacServices.ProgressChanged += OnProgressChanged;

                dacServices.ExportBacpac(outputFilePath, databaseName);

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
                if (validateConnection)
                {
                    var connectionTest = TestConnection(targetConnectionString);
                    if (!connectionTest.IsValid)
                    {
                        throw new Exception($"Target database connection validation failed: {connectionTest.Message}");
                    }
                    Console.WriteLine($"Target connection validated successfully. Server: {connectionTest.Server}");
                }

                string extension = Path.GetExtension(packageFilePath).ToLowerInvariant();
                bool isBacpac = extension == ".bacpac";

                var dacServices = new DacServices(targetConnectionString);
                dacServices.Message += OnMessage;
                dacServices.ProgressChanged += OnProgressChanged;

                if (isBacpac)
                {
                    var bacPackage = BacPackage.Load(packageFilePath);
                    dacServices.ImportBacpac(bacPackage, targetDatabaseName);
                }
                else
                {
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

        private void OnMessage(object sender, DacMessageEventArgs e)
        {
            Console.WriteLine($"[{e.Message.MessageType}] {e.Message.Message}");
        }

        private void OnProgressChanged(object sender, DacProgressEventArgs e)
        {
            Console.WriteLine($"Progress: {e.Status} - {e.Message}");
        }
    }
}

