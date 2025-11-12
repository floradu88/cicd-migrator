using System;
using System.Collections.Generic;

namespace DatabaseExtractor
{
    /// <summary>
    /// Interface for database provider implementations (SQL Server, PostgreSQL, etc.)
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Gets the database provider type (e.g., "SqlServer", "PostgreSQL").
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Gets the default file extension for backup/export files (e.g., ".bacpac", ".sql", ".dump").
        /// </summary>
        string DefaultFileExtension { get; }

        /// <summary>
        /// Tests the database connection to ensure it's accessible.
        /// </summary>
        /// <param name="connectionString">Database connection string to test</param>
        /// <param name="timeoutSeconds">Connection timeout in seconds (default: 30)</param>
        /// <returns>Connection test result with details</returns>
        ConnectionTestResult TestConnection(string connectionString, int timeoutSeconds = 30);

        /// <summary>
        /// Lists all databases accessible with the given connection string for security auditing.
        /// </summary>
        /// <param name="connectionString">Database connection string (will connect to system database)</param>
        /// <param name="timeoutSeconds">Query timeout in seconds (default: 30)</param>
        /// <returns>List of accessible databases with security-relevant information</returns>
        DatabaseListResult ListDatabases(string connectionString, int timeoutSeconds = 30);

        /// <summary>
        /// Extracts the database schema (and optionally data) to a file.
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="outputFilePath">Full path where the export file will be created</param>
        /// <param name="extractOptions">Extraction options</param>
        /// <param name="validateConnection">Whether to validate connection before extraction (default: true)</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        bool ExtractSchema(string connectionString, string outputFilePath, ExtractOptions extractOptions = null, bool validateConnection = true);

        /// <summary>
        /// Restores a database from a backup/export file.
        /// </summary>
        /// <param name="packageFilePath">Full path to the backup/export file</param>
        /// <param name="targetConnectionString">Connection string to the target database server</param>
        /// <param name="targetDatabaseName">Name of the target database (will be created if it doesn't exist)</param>
        /// <param name="upgradeExisting">Whether to upgrade existing database (true) or create new (false)</param>
        /// <param name="validateConnection">Whether to validate connection before restore (default: true)</param>
        /// <returns>True if restore succeeded, false otherwise</returns>
        bool RestoreDatabase(string packageFilePath, string targetConnectionString, string targetDatabaseName, bool upgradeExisting = false, bool validateConnection = true);

        /// <summary>
        /// Detects the database provider type from a connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to analyze</param>
        /// <returns>True if this provider can handle the connection string, false otherwise</returns>
        bool CanHandleConnectionString(string connectionString);
    }
}

