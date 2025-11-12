using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseExtractor.Providers;

namespace DatabaseExtractor
{
    /// <summary>
    /// Factory for creating database provider instances based on connection string or explicit provider type.
    /// </summary>
    public static class DatabaseProviderFactory
    {
        private static readonly List<IDatabaseProvider> _providers = new List<IDatabaseProvider>
        {
            new SqlServerDatabaseProvider(),
            new PostgreSQLDatabaseProvider()
        };

        /// <summary>
        /// Gets a database provider based on the connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to analyze</param>
        /// <returns>Appropriate database provider, or null if no provider can handle the connection string</returns>
        public static IDatabaseProvider GetProvider(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            foreach (var provider in _providers)
            {
                if (provider.CanHandleConnectionString(connectionString))
                {
                    return provider;
                }
            }

            throw new NotSupportedException($"No database provider found that can handle the connection string. Supported providers: {string.Join(", ", _providers.Select(p => p.ProviderType))}");
        }

        /// <summary>
        /// Gets a database provider by explicit type name.
        /// </summary>
        /// <param name="providerType">Provider type name (e.g., "SqlServer", "PostgreSQL")</param>
        /// <returns>Database provider instance, or null if not found</returns>
        public static IDatabaseProvider GetProviderByType(string providerType)
        {
            if (string.IsNullOrWhiteSpace(providerType))
            {
                throw new ArgumentNullException(nameof(providerType), "Provider type cannot be null or empty.");
            }

            var provider = _providers.FirstOrDefault(p => 
                p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new NotSupportedException($"Provider type '{providerType}' is not supported. Supported providers: {string.Join(", ", _providers.Select(p => p.ProviderType))}");
            }

            return provider;
        }

        /// <summary>
        /// Gets all available provider types.
        /// </summary>
        /// <returns>List of provider type names</returns>
        public static IEnumerable<string> GetAvailableProviderTypes()
        {
            return _providers.Select(p => p.ProviderType);
        }

        /// <summary>
        /// Registers a custom database provider.
        /// </summary>
        /// <param name="provider">Database provider instance to register</param>
        public static void RegisterProvider(IDatabaseProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            // Remove existing provider of the same type if present
            _providers.RemoveAll(p => p.ProviderType.Equals(provider.ProviderType, StringComparison.OrdinalIgnoreCase));
            _providers.Add(provider);
        }
    }
}

