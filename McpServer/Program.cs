using System;
using System.IO;
using System.Linq;
using DatabaseExtractor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace McpServer
{
    /// <summary>
    /// MCP Server for Database Schema Extraction and Restore operations.
    /// Implements Model Context Protocol (MCP) to expose database tools.
    /// </summary>
    class Program
    {
        private static string _defaultOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DacpacFiles");

        static void Main(string[] args)
        {
            // Ensure output directory exists
            if (!Directory.Exists(_defaultOutputPath))
            {
                Directory.CreateDirectory(_defaultOutputPath);
            }

            Console.Error.WriteLine("MCP Server started. Waiting for requests...");
            
            // MCP uses stdio for communication
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                try
                {
                    var request = JObject.Parse(line);
                    var response = ProcessRequest(request);
                    
                    if (response != null)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(response));
                    }
                }
                catch (Exception ex)
                {
                    var errorResponse = new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = -32603,
                            message = "Internal error",
                            data = ex.Message
                        }
                    };
                    Console.WriteLine(JsonConvert.SerializeObject(errorResponse));
                }
            }
        }

        private static object ProcessRequest(JObject request)
        {
            var method = request["method"]?.ToString();
            var id = request["id"];
            var @params = request["params"] as JObject;

            switch (method)
            {
                case "initialize":
                    return HandleInitialize(id, @params);
                
                case "tools/list":
                    return HandleToolsList(id);
                
                case "tools/call":
                    return HandleToolCall(id, @params);
                
                default:
                    return new
                    {
                        jsonrpc = "2.0",
                        id = id,
                        error = new
                        {
                            code = -32601,
                            message = "Method not found"
                        }
                    };
            }
        }

        private static object HandleInitialize(dynamic id, JObject @params)
        {
            return new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { }
                    },
                    serverInfo = new
                    {
                        name = "database-extractor-mcp-server",
                        version = "1.0.0"
                    }
                }
            };
        }

        private static object HandleToolsList(dynamic id)
        {
            return new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    tools = new object[]
                    {
                        new
                        {
                            name = "extract_schema",
                            description = "Extracts database schema from SQL Server (local or Azure) to a BACPAC file. Supports both local SQL Server and Azure SQL Database.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    connectionString = new
                                    {
                                        type = "string",
                                        description = "SQL Server connection string (e.g., 'Server=(local);Database=MyDB;Integrated Security=True;' or Azure SQL connection string)"
                                    },
                                    outputPath = new
                                    {
                                        type = "string",
                                        description = "Full path where the BACPAC file will be created. If not provided, uses default Documents/DacpacFiles directory."
                                    },
                                    extractTableData = new
                                    {
                                        type = "boolean",
                                        description = "Whether to extract table data along with schema (default: false - schema only via BACPAC)"
                                    },
                                    validateConnection = new
                                    {
                                        type = "boolean",
                                        description = "Whether to validate connection before extraction (default: true)"
                                    }
                                },
                                required = new[] { "connectionString" }
                            }
                        },
                        new
                        {
                            name = "restore_database",
                            description = "Restores a DACPAC or BACPAC file to a target database. Supports both local SQL Server and Azure SQL Database.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    packageFilePath = new
                                    {
                                        type = "string",
                                        description = "Full path to the DACPAC or BACPAC file to restore"
                                    },
                                    targetConnectionString = new
                                    {
                                        type = "string",
                                        description = "Connection string to the target database server"
                                    },
                                    targetDatabaseName = new
                                    {
                                        type = "string",
                                        description = "Name of the target database (will be created if it doesn't exist)"
                                    },
                                    upgradeExisting = new
                                    {
                                        type = "boolean",
                                        description = "Whether to upgrade existing database (true) or create new (false). Default: false"
                                    },
                                    validateConnection = new
                                    {
                                        type = "boolean",
                                        description = "Whether to validate connection before restore (default: true)"
                                    }
                                },
                                required = new[] { "packageFilePath", "targetConnectionString", "targetDatabaseName" }
                            }
                        },
                        new
                        {
                            name = "list_connection_examples",
                            description = "Returns example connection strings for various SQL Server configurations (local, LocalDB, Azure SQL, etc.)",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new { }
                            }
                        },
                        new
                        {
                            name = "test_connection",
                            description = "Tests a database connection to ensure it's accessible. Validates connection string and returns detailed connection information.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    connectionString = new
                                    {
                                        type = "string",
                                        description = "SQL Server connection string to test"
                                    },
                                    timeoutSeconds = new
                                    {
                                        type = "integer",
                                        description = "Connection timeout in seconds (default: 30)"
                                    }
                                },
                                required = new[] { "connectionString" }
                            }
                        },
                        new
                        {
                            name = "list_databases",
                            description = "Lists all databases accessible with a given connection string for security auditing. Returns database information including permissions, ownership, and security-relevant details.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    connectionString = new
                                    {
                                        type = "string",
                                        description = "SQL Server connection string (will connect to master database to query all databases)"
                                    },
                                    timeoutSeconds = new
                                    {
                                        type = "integer",
                                        description = "Query timeout in seconds (default: 30)"
                                    }
                                },
                                required = new[] { "connectionString" }
                            }
                        }
                    }
                }
            };
        }

        private static object HandleToolCall(dynamic id, JObject @params)
        {
            var toolName = @params["name"]?.ToString();
            var arguments = @params["arguments"] as JObject;

            try
            {
                object result = null;

                switch (toolName)
                {
                    case "extract_schema":
                        result = HandleExtractSchema(arguments);
                        break;
                    
                    case "restore_database":
                        result = HandleRestoreDatabase(arguments);
                        break;
                    
                    case "list_connection_examples":
                        result = HandleListConnectionExamples();
                        break;
                    
                    case "test_connection":
                        result = HandleTestConnection(arguments);
                        break;
                    
                    case "list_databases":
                        result = HandleListDatabases(arguments);
                        break;
                    
                    default:
                        return new
                        {
                            jsonrpc = "2.0",
                            id = id,
                            error = new
                            {
                                code = -32601,
                                message = $"Unknown tool: {toolName}"
                            }
                        };
                }

                return new
                {
                    jsonrpc = "2.0",
                    id = id,
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = JsonConvert.SerializeObject(result, Formatting.Indented)
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    jsonrpc = "2.0",
                    id = id,
                    error = new
                    {
                        code = -32603,
                        message = "Tool execution failed",
                        data = ex.Message
                    }
                };
            }
        }

        private static object HandleExtractSchema(JObject arguments)
        {
            var connectionString = arguments["connectionString"]?.ToString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connectionString is required");
            }

            var outputPath = arguments["outputPath"]?.ToString();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                // Auto-detect provider to determine default extension
                try
                {
                    var tempExtractor = new DatabaseSchemaExtractorV2(connectionString);
                    string defaultExtension = tempExtractor.ProviderType == "PostgreSQL" ? ".sql" : ".bacpac";
                    
                    // Try to extract database name from connection string
                    string dbName = "Database";
                    try
                    {
                        if (tempExtractor.ProviderType == "SqlServer")
                        {
                            var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                            dbName = builder.InitialCatalog ?? "Database";
                        }
                        else if (tempExtractor.ProviderType == "PostgreSQL")
                        {
                            var builder = new NpgsqlConnectionStringBuilder(connectionString);
                            dbName = builder.Database ?? "Database";
                        }
                    }
                    catch { }
                    
                    outputPath = Path.Combine(_defaultOutputPath, $"{dbName}{defaultExtension}");
                }
                catch
                {
                    outputPath = Path.Combine(_defaultOutputPath, "Database.bacpac");
                }
            }

            var extractTableData = arguments["extractTableData"]?.ToObject<bool>() ?? false;
            var validateConnection = arguments["validateConnection"]?.ToObject<bool?>();
            if (!validateConnection.HasValue) validateConnection = true;

            // Auto-detect provider from connection string
            var extractor = new DatabaseSchemaExtractorV2(connectionString);
            
            // Use ExtractOptions overload with validateConnection
            var extractOptions = new ExtractOptions
            {
                ExtractAllTableData = extractTableData
            };
            extractor.ExtractSchema(connectionString, outputPath, extractOptions, validateConnection.Value);

            return new
            {
                success = true,
                message = "Schema extracted successfully",
                outputPath = outputPath,
                fileExists = File.Exists(outputPath),
                fileSize = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0
            };
        }

        private static object HandleRestoreDatabase(JObject arguments)
        {
            var packageFilePath = arguments["packageFilePath"]?.ToString();
            if (string.IsNullOrWhiteSpace(packageFilePath))
            {
                throw new ArgumentException("packageFilePath is required");
            }

            var targetConnectionString = arguments["targetConnectionString"]?.ToString();
            if (string.IsNullOrWhiteSpace(targetConnectionString))
            {
                throw new ArgumentException("targetConnectionString is required");
            }

            var targetDatabaseName = arguments["targetDatabaseName"]?.ToString();
            if (string.IsNullOrWhiteSpace(targetDatabaseName))
            {
                throw new ArgumentException("targetDatabaseName is required");
            }

            var upgradeExisting = arguments["upgradeExisting"]?.ToObject<bool>() ?? false;
            var validateConnection = arguments["validateConnection"]?.ToObject<bool?>();
            if (!validateConnection.HasValue) validateConnection = true;

            // Auto-detect provider from connection string
            var extractor = new DatabaseSchemaExtractorV2(targetConnectionString);
            extractor.RestoreDatabase(packageFilePath, targetConnectionString, targetDatabaseName, upgradeExisting, validateConnection.Value);

            return new
            {
                success = true,
                message = $"Successfully restored {Path.GetFileName(packageFilePath)} to database '{targetDatabaseName}'",
                packageFilePath = packageFilePath,
                targetDatabaseName = targetDatabaseName,
                upgradeExisting = upgradeExisting
            };
        }

        private static object HandleListConnectionExamples()
        {
            return new
            {
                examples = new
                {
                    localWindowsAuth = new
                    {
                        description = "Local SQL Server with Windows Authentication",
                        connectionString = ConnectionStringExamples.LocalWindowsAuth,
                        usage = "Server=(local);Database=YourDatabase;Integrated Security=True;"
                    },
                    localSqlAuth = new
                    {
                        description = "Local SQL Server with SQL Server Authentication",
                        connectionString = ConnectionStringExamples.LocalSqlAuth,
                        usage = "Server=(local);Database=YourDatabase;User Id=sa;Password=YourPassword;"
                    },
                    localDbWindowsAuth = new
                    {
                        description = "LocalDB with Windows Authentication",
                        connectionString = ConnectionStringExamples.LocalDbWindowsAuth,
                        usage = "Server=(localdb)\\MSSQLLocalDB;Database=YourDatabase;Integrated Security=True;"
                    },
                    namedInstance = new
                    {
                        description = "Named SQL Server instance (e.g., SQLEXPRESS)",
                        connectionString = ConnectionStringExamples.NamedInstanceWindowsAuth,
                        usage = "Server=.\\SQLEXPRESS;Database=YourDatabase;Integrated Security=True;"
                    },
                    azureSql = new
                    {
                        description = "Azure SQL Database",
                        connectionString = ConnectionStringExamples.AzureSql,
                        usage = "Server=tcp:yourserver.database.windows.net,1433;Database=YourDatabase;User ID=YourUser;Password=YourPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
                    }
                }
            };
        }

        private static object HandleTestConnection(JObject arguments)
        {
            var connectionString = arguments["connectionString"]?.ToString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connectionString is required");
            }

            var timeoutSeconds = arguments["timeoutSeconds"]?.ToObject<int?>() ?? 30;

            // Auto-detect provider from connection string
            var extractor = new DatabaseSchemaExtractorV2(connectionString);
            var result = extractor.TestConnection(connectionString, timeoutSeconds);

            return new
            {
                isValid = result.IsValid,
                message = result.Message,
                server = result.Server,
                database = result.Database,
                authenticationType = result.AuthenticationType,
                userId = result.UserId,
                serverVersion = result.ServerVersion,
                tableCount = result.TableCount,
                errorCode = result.ErrorCode,
                errorDetails = result.ErrorDetails,
                testedAt = result.TestedAt
            };
        }

        private static object HandleListDatabases(JObject arguments)
        {
            var connectionString = arguments["connectionString"]?.ToString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connectionString is required");
            }

            var timeoutSeconds = arguments["timeoutSeconds"]?.ToObject<int?>() ?? 30;

            // Auto-detect provider from connection string
            var extractor = new DatabaseSchemaExtractorV2(connectionString);
            var result = extractor.ListDatabases(connectionString, timeoutSeconds);

            return new
            {
                isValid = result.IsValid,
                message = result.Message,
                server = result.Server,
                authenticationType = result.AuthenticationType,
                userId = result.UserId,
                databaseCount = result.DatabaseCount,
                databases = result.Databases?.Select(db => new
                {
                    name = db.Name,
                    databaseId = db.DatabaseId,
                    state = db.State,
                    recoveryModel = db.RecoveryModel,
                    collation = db.Collation,
                    createDate = db.CreateDate,
                    compatibilityLevel = db.CompatibilityLevel,
                    owner = db.Owner,
                    canViewDefinition = db.CanViewDefinition,
                    canConnect = db.CanConnect,
                    canCreateTable = db.CanCreateTable
                }).ToArray() ?? new object[0],
                errorCode = result.ErrorCode,
                errorDetails = result.ErrorDetails,
                listedAt = result.ListedAt
            };
        }
    }
}

