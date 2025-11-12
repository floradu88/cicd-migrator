using System;
using System.Collections.Generic;

namespace DatabaseExtractor
{
    /// <summary>
    /// Multi-database schema extractor that supports SQL Server, PostgreSQL, and other pluggable providers.
    /// This is the new version that uses the provider abstraction pattern.
    /// </summary>
    public class DatabaseSchemaExtractorV2
    {
        private readonly IDatabaseProvider _provider;

        /// <summary>
        /// Creates a new instance using auto-detection from connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to auto-detect provider from</param>
        public DatabaseSchemaExtractorV2(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            _provider = DatabaseProviderFactory.GetProvider(connectionString);
        }

        /// <summary>
        /// Creates a new instance with a specific provider type.
        /// </summary>
        /// <param name="providerType">Provider type name (e.g., "SqlServer", "PostgreSQL")</param>
        public DatabaseSchemaExtractorV2(string providerType, string connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(providerType))
            {
                throw new ArgumentNullException(nameof(providerType), "Provider type cannot be null or empty.");
            }

            _provider = DatabaseProviderFactory.GetProviderByType(providerType);
        }

        /// <summary>
        /// Creates a new instance with an explicit provider.
        /// </summary>
        /// <param name="provider">Database provider instance</param>
        public DatabaseSchemaExtractorV2(IDatabaseProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Gets the current provider type.
        /// </summary>
        public string ProviderType => _provider.ProviderType;

        /// <summary>
        /// Tests the database connection to ensure it's accessible.
        /// </summary>
        /// <param name="connectionString">Database connection string to test</param>
        /// <param name="timeoutSeconds">Connection timeout in seconds (default: 30)</param>
        /// <returns>Connection test result with details</returns>
        public ConnectionTestResult TestConnection(string connectionString, int timeoutSeconds = 30)
        {
            return _provider.TestConnection(connectionString, timeoutSeconds);
        }

        /// <summary>
        /// Lists all databases accessible with the given connection string for security auditing.
        /// </summary>
        /// <param name="connectionString">Database connection string (will connect to system database)</param>
        /// <param name="timeoutSeconds">Query timeout in seconds (default: 30)</param>
        /// <returns>List of accessible databases with security-relevant information</returns>
        public DatabaseListResult ListDatabases(string connectionString, int timeoutSeconds = 30)
        {
            return _provider.ListDatabases(connectionString, timeoutSeconds);
        }

        /// <summary>
        /// Extracts the database schema (and optionally data) to a file.
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="outputFilePath">Full path where the export file will be created</param>
        /// <param name="extractOptions">Extraction options</param>
        /// <param name="validateConnection">Whether to validate connection before extraction (default: true)</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        public bool ExtractSchema(string connectionString, string outputFilePath, ExtractOptions extractOptions = null, bool validateConnection = true)
        {
            return _provider.ExtractSchema(connectionString, outputFilePath, extractOptions, validateConnection);
        }

        /// <summary>
        /// Restores a database from a backup/export file.
        /// </summary>
        /// <param name="packageFilePath">Full path to the backup/export file</param>
        /// <param name="targetConnectionString">Connection string to the target database server</param>
        /// <param name="targetDatabaseName">Name of the target database (will be created if it doesn't exist)</param>
        /// <param name="upgradeExisting">Whether to upgrade existing database (true) or create new (false)</param>
        /// <param name="validateConnection">Whether to validate connection before restore (default: true)</param>
        /// <returns>True if restore succeeded, false otherwise</returns>
        public bool RestoreDatabase(string packageFilePath, string targetConnectionString, string targetDatabaseName, bool upgradeExisting = false, bool validateConnection = true)
        {
            return _provider.RestoreDatabase(packageFilePath, targetConnectionString, targetDatabaseName, upgradeExisting, validateConnection);
        }
    }
}

