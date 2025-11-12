using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using DatabaseExtractor;

namespace FileDownloadApi.Controllers
{
    /// <summary>
    /// API controller for downloading DACPAC files and checking file status.
    /// </summary>
    [RoutePrefix("api/files")]
    public class FilesController : ApiController
    {
        private readonly string _filesDirectory;

        public FilesController()
        {
            // Get the files directory from configuration or use default
            var configuredPath = System.Configuration.ConfigurationManager.AppSettings["DacpacFilesPath"];
            
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                _filesDirectory = configuredPath;
            }
            else
            {
                // Use default: App_Data/Files relative to application root
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                
                // Handle empty or invalid base directory
                if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory))
                {
                    // Try HttpRuntime.AppDomainAppPath for web applications
                    try
                    {
                        baseDirectory = System.Web.HttpRuntime.AppDomainAppPath;
                    }
                    catch
                    {
                        // Fallback to current directory
                        baseDirectory = Directory.GetCurrentDirectory();
                    }
                }
                
                // Ensure we have a valid base directory
                if (string.IsNullOrWhiteSpace(baseDirectory))
                {
                    baseDirectory = Environment.CurrentDirectory;
                }
                
                _filesDirectory = Path.Combine(baseDirectory, "App_Data", "Files");
            }
            
            // Ensure directory exists
            if (!string.IsNullOrWhiteSpace(_filesDirectory) && !Directory.Exists(_filesDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_filesDirectory);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail constructor - directory creation will be attempted on first use
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not create directory '{_filesDirectory}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Downloads a DACPAC file by filename.
        /// GET: api/files/{filename}
        /// </summary>
        /// <param name="filename">Name of the file to download (e.g., "MyDatabase.dacpac")</param>
        /// <returns>File download response or 404 if not found</returns>
        [HttpGet]
        [Route("{filename}")]
        public HttpResponseMessage DownloadFile(string filename)
        {
            // Sanitize filename to prevent directory traversal
            string safeFilename = Path.GetFileName(filename);
            string filePath = Path.Combine(_filesDirectory, safeFilename);

            if (!File.Exists(filePath))
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, 
                    $"File '{filename}' not found.");
            }

            try
            {
                var fileBytes = File.ReadAllBytes(filePath);
                var fileInfo = new FileInfo(filePath);

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(fileBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = safeFilename
                };
                response.Content.Headers.ContentLength = fileInfo.Length;

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, 
                    $"Error reading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the status information for a DACPAC file.
        /// GET: api/files/{filename}/status
        /// </summary>
        /// <param name="filename">Name of the file to check</param>
        /// <returns>File status information (exists, size, last modified, etc.)</returns>
        [HttpGet]
        [Route("{filename}/status")]
        public IHttpActionResult GetFileStatus(string filename)
        {
            // Sanitize filename
            string safeFilename = Path.GetFileName(filename);
            string filePath = Path.Combine(_filesDirectory, safeFilename);

            var status = new FileStatusResponse
            {
                Filename = safeFilename,
                Exists = File.Exists(filePath)
            };

            if (status.Exists)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    status.Size = fileInfo.Length;
                    status.SizeFormatted = FormatFileSize(fileInfo.Length);
                    status.LastModified = fileInfo.LastWriteTime;
                    status.Created = fileInfo.CreationTime;
                    status.FullPath = filePath;
                }
                catch (Exception ex)
                {
                    status.Error = ex.Message;
                }
            }

            return Ok(status);
        }

        /// <summary>
        /// Lists all available DACPAC files.
        /// GET: api/files
        /// </summary>
        /// <returns>List of available files with basic information</returns>
        [HttpGet]
        [Route("")]
        public IHttpActionResult ListFiles()
        {
            try
            {
                var files = new List<FileInfoResponse>();

                if (Directory.Exists(_filesDirectory))
                {
                    // Get both DACPAC and BACPAC files
                    var dacpacFiles = Directory.GetFiles(_filesDirectory, "*.dacpac");
                    var bacpacFiles = Directory.GetFiles(_filesDirectory, "*.bacpac");
                    var allFiles = new List<string>(dacpacFiles);
                    allFiles.AddRange(bacpacFiles);

                    foreach (var filePath in allFiles)
                    {
                        var fileInfo = new FileInfo(filePath);
                        files.Add(new FileInfoResponse
                        {
                            Filename = Path.GetFileName(filePath),
                            Size = fileInfo.Length,
                            SizeFormatted = FormatFileSize(fileInfo.Length),
                            LastModified = fileInfo.LastWriteTime,
                            DownloadUrl = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Authority}/api/files/{Path.GetFileName(filePath)}"
                        });
                    }
                }

                return Ok(new { Files = files, Count = files.Count, Directory = _filesDirectory });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Formats file size in human-readable format.
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Restores a DACPAC or BACPAC file to a target database.
        /// POST: api/files/{filename}/restore
        /// </summary>
        /// <param name="filename">Name of the DACPAC or BACPAC file to restore</param>
        /// <param name="request">Restore request containing target connection string and database name</param>
        /// <returns>Restore operation result</returns>
        [HttpPost]
        [Route("{filename}/restore")]
        public IHttpActionResult RestoreFile(string filename, [FromBody] RestoreRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.TargetConnectionString))
            {
                return BadRequest("TargetConnectionString is required.");
            }

            if (string.IsNullOrWhiteSpace(request.TargetDatabaseName))
            {
                return BadRequest("TargetDatabaseName is required.");
            }

            // Sanitize filename
            string safeFilename = Path.GetFileName(filename);
            string filePath = Path.Combine(_filesDirectory, safeFilename);

            if (!File.Exists(filePath))
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"File '{filename}' not found."));
            }

            try
            {
                var extractor = new DatabaseSchemaExtractor();
                
                bool success = extractor.RestoreDatabase(
                    packageFilePath: filePath,
                    targetConnectionString: request.TargetConnectionString,
                    targetDatabaseName: request.TargetDatabaseName,
                    upgradeExisting: request.UpgradeExisting,
                    validateConnection: request.ValidateConnection ?? true
                );

                if (success)
                {
                    return Ok(new RestoreResponse
                    {
                        Success = true,
                        Message = $"Successfully restored {safeFilename} to database '{request.TargetDatabaseName}'",
                        Filename = safeFilename,
                        TargetDatabaseName = request.TargetDatabaseName
                    });
                }
                else
                {
                    return InternalServerError(new Exception("Restore operation returned false"));
                }
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Unexpected error during restore: {ex.Message}"));
            }
        }

        /// <summary>
        /// Tests a database connection.
        /// POST: api/files/test-connection
        /// </summary>
        /// <param name="request">Connection test request</param>
        /// <returns>Connection test result</returns>
        [HttpPost]
        [Route("test-connection")]
        public IHttpActionResult TestConnection([FromBody] ConnectionTestRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ConnectionString))
            {
                return BadRequest("ConnectionString is required.");
            }

            try
            {
                var extractor = new DatabaseSchemaExtractor();
                var result = extractor.TestConnection(request.ConnectionString, request.TimeoutSeconds ?? 30);

                return Ok(result);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Connection test failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Lists all databases accessible with the given connection string for security auditing.
        /// POST: api/files/list-databases
        /// </summary>
        /// <param name="request">Database list request</param>
        /// <returns>List of accessible databases with security information</returns>
        [HttpPost]
        [Route("list-databases")]
        public IHttpActionResult ListDatabases([FromBody] DatabaseListRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ConnectionString))
            {
                return BadRequest("ConnectionString is required.");
            }

            try
            {
                var extractor = new DatabaseSchemaExtractor();
                var result = extractor.ListDatabases(request.ConnectionString, request.TimeoutSeconds ?? 30);

                return Ok(result);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Failed to list databases: {ex.Message}"));
            }
        }

        /// <summary>
        /// Extracts database schema and automatically saves to the configured files directory.
        /// POST: api/files/extract
        /// </summary>
        /// <param name="request">Extract request containing connection string and options</param>
        /// <returns>Extract operation result with file path</returns>
        [HttpPost]
        [Route("extract")]
        public IHttpActionResult ExtractSchema([FromBody] ExtractRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ConnectionString))
            {
                return BadRequest("ConnectionString is required.");
            }

            try
            {
                // Ensure the files directory exists
                if (!Directory.Exists(_filesDirectory))
                {
                    Directory.CreateDirectory(_filesDirectory);
                }

                // Generate output filename from connection string or use provided name
                string outputFilename = request.OutputFilename;
                if (string.IsNullOrWhiteSpace(outputFilename))
                {
                    try
                    {
                        // Parse connection string to extract database name
                        var connectionString = request.ConnectionString;
                        var dbName = "Database";
                        
                        // Try to extract database name from connection string
                        var dbIndex = connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase);
                        if (dbIndex >= 0)
                        {
                            var startIndex = dbIndex + 9; // "Database=".Length
                            var endIndex = connectionString.IndexOf(';', startIndex);
                            if (endIndex < 0) endIndex = connectionString.Length;
                            dbName = connectionString.Substring(startIndex, endIndex - startIndex).Trim();
                        }
                        else
                        {
                            // Try "Initial Catalog="
                            var catalogIndex = connectionString.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
                            if (catalogIndex >= 0)
                            {
                                var startIndex = catalogIndex + 16; // "Initial Catalog=".Length
                                var endIndex = connectionString.IndexOf(';', startIndex);
                                if (endIndex < 0) endIndex = connectionString.Length;
                                dbName = connectionString.Substring(startIndex, endIndex - startIndex).Trim();
                            }
                        }
                        
                        if (string.IsNullOrWhiteSpace(dbName))
                        {
                            dbName = "Database";
                        }
                        
                        outputFilename = $"{dbName}.bacpac";
                    }
                    catch
                    {
                        outputFilename = $"Database_{DateTime.UtcNow:yyyyMMddHHmmss}.bacpac";
                    }
                }

                // Ensure filename has .bacpac or .dacpac extension
                if (!outputFilename.EndsWith(".bacpac", StringComparison.OrdinalIgnoreCase) &&
                    !outputFilename.EndsWith(".dacpac", StringComparison.OrdinalIgnoreCase))
                {
                    outputFilename = Path.ChangeExtension(outputFilename, ".bacpac");
                }

                // Sanitize filename
                string safeFilename = Path.GetFileName(outputFilename);
                string outputPath = Path.Combine(_filesDirectory, safeFilename);

                // Extract schema using DatabaseExtractor
                var extractor = new DatabaseSchemaExtractor();
                var extractOptions = new ExtractOptions
                {
                    ExtractAllTableData = request.ExtractTableData ?? false,
                    IgnoreExtendedProperties = request.IgnoreExtendedProperties ?? false,
                    VerifyExtraction = request.VerifyExtraction ?? true
                };

                bool success = extractor.ExtractSchema(
                    connectionString: request.ConnectionString,
                    outputDacpacPath: outputPath,
                    extractOptions: extractOptions,
                    validateConnection: request.ValidateConnection ?? true
                );

                if (success)
                {
                    var fileInfo = new FileInfo(outputPath);
                    return Ok(new ExtractResponse
                    {
                        Success = true,
                        Message = "Schema extracted successfully",
                        Filename = safeFilename,
                        FilePath = outputPath,
                        FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                        FileSizeFormatted = fileInfo.Exists ? FormatFileSize(fileInfo.Length) : "0 B",
                        DownloadUrl = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Authority}/api/files/{safeFilename}"
                    });
                }
                else
                {
                    return InternalServerError(new Exception("Extraction completed but returned false."));
                }
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Failed to extract schema: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Response model for file status.
    /// </summary>
    public class FileStatusResponse
    {
        public string Filename { get; set; }
        public bool Exists { get; set; }
        public long? Size { get; set; }
        public string SizeFormatted { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? Created { get; set; }
        public string FullPath { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Response model for file listing.
    /// </summary>
    public class FileInfoResponse
    {
        public string Filename { get; set; }
        public long Size { get; set; }
        public string SizeFormatted { get; set; }
        public DateTime LastModified { get; set; }
        public string DownloadUrl { get; set; }
    }

    /// <summary>
    /// Request model for database restore operation.
    /// </summary>
    public class RestoreRequest
    {
        /// <summary>
        /// Connection string to the target database server.
        /// </summary>
        public string TargetConnectionString { get; set; }

        /// <summary>
        /// Name of the target database (will be created if it doesn't exist).
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Whether to upgrade existing database (true) or create new (false).
        /// Default: false (create new database).
        /// </summary>
        public bool UpgradeExisting { get; set; } = false;

        /// <summary>
        /// Whether to validate connection before restore (default: true).
        /// </summary>
        public bool? ValidateConnection { get; set; } = true;
    }

    /// <summary>
    /// Response model for restore operation.
    /// </summary>
    public class RestoreResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Filename { get; set; }
        public string TargetDatabaseName { get; set; }
    }

    /// <summary>
    /// Request model for connection test operation.
    /// </summary>
    public class ConnectionTestRequest
    {
        /// <summary>
        /// Connection string to test.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Connection timeout in seconds (default: 30).
        /// </summary>
        public int? TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Request model for database list operation.
    /// </summary>
    public class DatabaseListRequest
    {
        /// <summary>
        /// Connection string to use for listing databases (will connect to master database).
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Query timeout in seconds (default: 30).
        /// </summary>
        public int? TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Request model for schema extraction operation.
    /// </summary>
    public class ExtractRequest
    {
        /// <summary>
        /// SQL Server connection string (local or Azure SQL Database).
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Output filename (optional). If not provided, generated from database name.
        /// Files are automatically saved to the configured DacpacFilesPath.
        /// </summary>
        public string OutputFilename { get; set; }

        /// <summary>
        /// Whether to extract table data along with schema (default: false - schema only).
        /// </summary>
        public bool? ExtractTableData { get; set; } = false;

        /// <summary>
        /// Whether to ignore extended properties (default: false).
        /// </summary>
        public bool? IgnoreExtendedProperties { get; set; } = false;

        /// <summary>
        /// Whether to verify extraction (default: true).
        /// </summary>
        public bool? VerifyExtraction { get; set; } = true;

        /// <summary>
        /// Whether to validate connection before extraction (default: true).
        /// </summary>
        public bool? ValidateConnection { get; set; } = true;
    }

    /// <summary>
    /// Response model for extract operation.
    /// </summary>
    public class ExtractResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Filename { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; }
        public string DownloadUrl { get; set; }
    }
}

