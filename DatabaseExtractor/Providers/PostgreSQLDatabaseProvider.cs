using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Npgsql;

namespace DatabaseExtractor.Providers
{
    /// <summary>
    /// PostgreSQL database provider implementation using Npgsql and pg_dump/pg_restore.
    /// </summary>
    public class PostgreSQLDatabaseProvider : IDatabaseProvider
    {
        public string ProviderType => "PostgreSQL";
        public string DefaultFileExtension => ".sql";

        public bool CanHandleConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            var lower = connectionString.ToLowerInvariant();
            return lower.Contains("host=") ||
                   lower.Contains("server=") ||
                   lower.Contains("database=") ||
                   lower.Contains("dbname=") ||
                   lower.Contains("port=") ||
                   lower.Contains("user id=") ||
                   lower.Contains("username=") ||
                   lower.Contains("password=") ||
                   lower.Contains("npgsql") ||
                   lower.Contains("postgresql") ||
                   lower.Contains("postgres://");
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
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                result.Server = builder.Host;
                result.Database = builder.Database;
                result.AuthenticationType = "PostgreSQL Authentication";
                result.UserId = builder.Username;

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    result.IsValid = true;
                    result.Message = "Connection successful";

                    using (var command = new NpgsqlCommand("SELECT version()", connection))
                    {
                        command.CommandTimeout = timeoutSeconds;
                        result.ServerVersion = command.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(result.Database))
                    {
                        using (var command = new NpgsqlCommand("SELECT current_database()", connection))
                        {
                            result.Database = command.ExecuteScalar()?.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(result.Database))
                    {
                        using (var command = new NpgsqlCommand(
                            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'", 
                            connection))
                        {
                            command.CommandTimeout = timeoutSeconds;
                            var tableCount = command.ExecuteScalar();
                            result.TableCount = tableCount != null ? Convert.ToInt32(tableCount) : 0;
                        }
                    }
                }
            }
            catch (PostgresException ex)
            {
                result.IsValid = false;
                result.Message = $"PostgreSQL error: {ex.Message}";
                result.ErrorCode = ex.SqlState != null ? int.TryParse(ex.SqlState, out var code) ? code : (int?)null : null;
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
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                result.Server = builder.Host;
                result.AuthenticationType = "PostgreSQL Authentication";
                result.UserId = builder.Username;

                // Connect to postgres database to query all databases
                var postgresConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = "postgres"
                };

                using (var connection = new NpgsqlConnection(postgresConnectionString.ConnectionString))
                {
                    connection.Open();
                    result.IsValid = true;
                    result.Message = "Successfully retrieved database list";

                    string query = @"
                        SELECT 
                            d.datname AS DatabaseName,
                            d.oid AS DatabaseId,
                            CASE 
                                WHEN d.datallowconn = true THEN 'ONLINE'
                                ELSE 'OFFLINE'
                            END AS State,
                            pg_encoding_to_char(d.encoding) AS Encoding,
                            d.datcollate AS Collation,
                            d.datctype AS CType,
                            (SELECT usename FROM pg_user WHERE usesysid = d.datdba) AS Owner,
                            CASE 
                                WHEN has_database_privilege(d.datname, 'CONNECT') THEN true
                                ELSE false
                            END AS CanConnect,
                            CASE 
                                WHEN has_database_privilege(d.datname, 'CREATE') THEN true
                                ELSE false
                            END AS CanCreateTable,
                            CASE 
                                WHEN has_database_privilege(d.datname, 'TEMP') THEN true
                                ELSE false
                            END AS CanCreateTemp
                        FROM pg_database d
                        WHERE d.datallowconn = true
                        ORDER BY d.datname";

                    using (var command = new NpgsqlCommand(query, connection))
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
                                    Collation = reader["Collation"]?.ToString(),
                                    Owner = reader["Owner"]?.ToString(),
                                    CanConnect = reader["CanConnect"] != DBNull.Value && Convert.ToBoolean(reader["CanConnect"]),
                                    CanCreateTable = reader["CanCreateTable"] != DBNull.Value && Convert.ToBoolean(reader["CanCreateTable"]),
                                    // Store additional PostgreSQL-specific info in RecoveryModel field
                                    RecoveryModel = reader["Encoding"]?.ToString()
                                };
                                result.Databases.Add(dbInfo);
                            }
                        }
                    }
                    result.DatabaseCount = result.Databases.Count;
                }
            }
            catch (PostgresException ex)
            {
                result.IsValid = false;
                result.Message = $"PostgreSQL error while listing databases: {ex.Message}";
                result.ErrorCode = ex.SqlState != null ? int.TryParse(ex.SqlState, out var code) ? code : (int?)null : null;
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

                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                string databaseName = builder.Database;

                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw new ArgumentException("Database name not found in connection string. Ensure 'Database' or 'DBName' is specified.");
                }

                // Use pg_dump for extraction
                string pgDumpPath = FindPgDumpPath();
                if (string.IsNullOrEmpty(pgDumpPath))
                {
                    throw new Exception("pg_dump executable not found. Please ensure PostgreSQL client tools are installed and in PATH.");
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = pgDumpPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Build pg_dump arguments
                var arguments = new StringBuilder();
                
                // Connection parameters
                if (!string.IsNullOrEmpty(builder.Host))
                    arguments.Append($" --host={builder.Host}");
                if (builder.Port > 0)
                    arguments.Append($" --port={builder.Port}");
                if (!string.IsNullOrEmpty(builder.Username))
                    arguments.Append($" --username={builder.Username}");
                arguments.Append($" --dbname={databaseName}");

                // Format: plain SQL (default) or custom format
                string extension = Path.GetExtension(outputFilePath).ToLowerInvariant();
                if (extension == ".dump" || extension == ".backup")
                {
                    arguments.Append(" --format=custom");
                }
                else
                {
                    arguments.Append(" --format=plain");
                }

                // Options
                if (extractOptions.ExtractAllTableData)
                {
                    arguments.Append(" --data-only");
                }
                else
                {
                    arguments.Append(" --schema-only");
                }

                if (extractOptions.IgnoreExtendedProperties)
                {
                    arguments.Append(" --no-owner --no-privileges");
                }

                arguments.Append(" --verbose");
                arguments.Append($" --file=\"{outputFilePath}\"");

                processStartInfo.Arguments = arguments.ToString();

                // Set PGPASSWORD environment variable if password is provided
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    processStartInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;
                }

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        throw new Exception("Failed to start pg_dump process.");
                    }

                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"pg_dump failed with exit code {process.ExitCode}: {errorOutput}");
                    }

                    if (extractOptions.VerifyExtraction && !File.Exists(outputFilePath))
                    {
                        throw new Exception("Extraction completed but output file was not created.");
                    }
                }

                return true;
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

                var builder = new NpgsqlConnectionStringBuilder(targetConnectionString);

                // Check if database exists
                bool databaseExists = false;
                using (var connection = new NpgsqlConnection(targetConnectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT 1 FROM pg_database WHERE datname = @dbname",
                        connection))
                    {
                        command.Parameters.AddWithValue("dbname", targetDatabaseName);
                        databaseExists = command.ExecuteScalar() != null;
                    }
                }

                // Create database if it doesn't exist
                if (!databaseExists)
                {
                    var postgresConnectionString = new NpgsqlConnectionStringBuilder(targetConnectionString)
                    {
                        Database = "postgres"
                    };

                    using (var connection = new NpgsqlConnection(postgresConnectionString.ConnectionString))
                    {
                        connection.Open();
                        using (var command = new NpgsqlCommand($"CREATE DATABASE \"{targetDatabaseName}\"", connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // Use pg_restore or psql depending on file format
                string extension = Path.GetExtension(packageFilePath).ToLowerInvariant();
                bool isCustomFormat = extension == ".dump" || extension == ".backup";

                if (isCustomFormat)
                {
                    // Use pg_restore for custom format
                    string pgRestorePath = FindPgRestorePath();
                    if (string.IsNullOrEmpty(pgRestorePath))
                    {
                        throw new Exception("pg_restore executable not found. Please ensure PostgreSQL client tools are installed and in PATH.");
                    }

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = pgRestorePath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var arguments = new StringBuilder();
                    if (!string.IsNullOrEmpty(builder.Host))
                        arguments.Append($" --host={builder.Host}");
                    if (builder.Port > 0)
                        arguments.Append($" --port={builder.Port}");
                    if (!string.IsNullOrEmpty(builder.Username))
                        arguments.Append($" --username={builder.Username}");
                    arguments.Append($" --dbname={targetDatabaseName}");
                    arguments.Append(" --verbose");
                    if (upgradeExisting)
                    {
                        arguments.Append(" --clean --if-exists");
                    }
                    arguments.Append($" \"{packageFilePath}\"");

                    processStartInfo.Arguments = arguments.ToString();

                    if (!string.IsNullOrEmpty(builder.Password))
                    {
                        processStartInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;
                    }

                    using (var process = Process.Start(processStartInfo))
                    {
                        if (process == null)
                        {
                            throw new Exception("Failed to start pg_restore process.");
                        }

                        string errorOutput = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"pg_restore failed with exit code {process.ExitCode}: {errorOutput}");
                        }
                    }
                }
                else
                {
                    // Use psql for plain SQL files
                    string psqlPath = FindPsqlPath();
                    if (string.IsNullOrEmpty(psqlPath))
                    {
                        throw new Exception("psql executable not found. Please ensure PostgreSQL client tools are installed and in PATH.");
                    }

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = psqlPath,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var arguments = new StringBuilder();
                    if (!string.IsNullOrEmpty(builder.Host))
                        arguments.Append($" --host={builder.Host}");
                    if (builder.Port > 0)
                        arguments.Append($" --port={builder.Port}");
                    if (!string.IsNullOrEmpty(builder.Username))
                        arguments.Append($" --username={builder.Username}");
                    arguments.Append($" --dbname={targetDatabaseName}");
                    arguments.Append(" --quiet");

                    processStartInfo.Arguments = arguments.ToString();

                    if (!string.IsNullOrEmpty(builder.Password))
                    {
                        processStartInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;
                    }

                    using (var process = Process.Start(processStartInfo))
                    {
                        if (process == null)
                        {
                            throw new Exception("Failed to start psql process.");
                        }

                        // Read SQL file and pipe to psql
                        string sqlContent = File.ReadAllText(packageFilePath);
                        process.StandardInput.Write(sqlContent);
                        process.StandardInput.Close();

                        string errorOutput = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"psql failed with exit code {process.ExitCode}: {errorOutput}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error during database restore: {ex.Message}", ex);
            }
        }

        private string FindPgDumpPath()
        {
            // Common paths for pg_dump
            var possiblePaths = new[]
            {
                "pg_dump",
                @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\13\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\12\bin\pg_dump.exe",
                @"/usr/bin/pg_dump",
                @"/usr/local/bin/pg_dump",
                @"/opt/homebrew/bin/pg_dump"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path) || IsInPath(path))
                {
                    return path;
                }
            }

            return null;
        }

        private string FindPgRestorePath()
        {
            var possiblePaths = new[]
            {
                "pg_restore",
                @"C:\Program Files\PostgreSQL\15\bin\pg_restore.exe",
                @"C:\Program Files\PostgreSQL\14\bin\pg_restore.exe",
                @"C:\Program Files\PostgreSQL\13\bin\pg_restore.exe",
                @"C:\Program Files\PostgreSQL\12\bin\pg_restore.exe",
                @"/usr/bin/pg_restore",
                @"/usr/local/bin/pg_restore",
                @"/opt/homebrew/bin/pg_restore"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path) || IsInPath(path))
                {
                    return path;
                }
            }

            return null;
        }

        private string FindPsqlPath()
        {
            var possiblePaths = new[]
            {
                "psql",
                @"C:\Program Files\PostgreSQL\15\bin\psql.exe",
                @"C:\Program Files\PostgreSQL\14\bin\psql.exe",
                @"C:\Program Files\PostgreSQL\13\bin\psql.exe",
                @"C:\Program Files\PostgreSQL\12\bin\psql.exe",
                @"/usr/bin/psql",
                @"/usr/local/bin/psql",
                @"/opt/homebrew/bin/psql"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path) || IsInPath(path))
                {
                    return path;
                }
            }

            return null;
        }

        private bool IsInPath(string executable)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    return process != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

