using Microsoft.SqlServer.Dac;
using System;
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
        /// Extracts the database schema from the specified connection string to a DACPAC file.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string (local or Azure SQL Database)</param>
        /// <param name="outputDacpacPath">Full path where the DACPAC file will be created</param>
        /// <param name="extractOptions">Optional extraction options. If null, default options will be used.</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString or outputDacpacPath is null or empty</exception>
        /// <exception cref="DacServicesException">Thrown when extraction fails</exception>
        public bool ExtractSchema(string connectionString, string outputDacpacPath, ExtractOptions extractOptions = null)
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
        /// <returns>True if restore succeeded, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null or empty</exception>
        /// <exception cref="DacServicesException">Thrown when restore fails</exception>
        public bool RestoreDatabase(string packageFilePath, string targetConnectionString, string targetDatabaseName, bool upgradeExisting = false)
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
}

