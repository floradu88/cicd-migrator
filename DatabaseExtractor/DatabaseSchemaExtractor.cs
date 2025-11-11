using System;
using System.Data.SqlClient;
using System.IO;
using Microsoft.SqlServer.Dac;

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
                using (DacServices dacServices = new DacServices(connectionString))
                {
                    // Set progress event handler
                    dacServices.Message += OnMessage;
                    dacServices.ProgressChanged += OnProgressChanged;

                    // Convert ExtractOptions to DacExtractOptions
                    DacExtractOptions dacExtractOptions = new DacExtractOptions
                    {
                        ExtractAllTableData = extractOptions.ExtractAllTableData,
                        IgnoreExtendedProperties = extractOptions.IgnoreExtendedProperties,
                        VerifyExtraction = extractOptions.VerifyExtraction
                    };

                    // Extract the database schema to DACPAC
                    // Parameters: packageFilePath, databaseName, options
                    dacServices.Extract(outputDacpacPath, databaseName, dacExtractOptions);

                    return true;
                }
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
    }
}

