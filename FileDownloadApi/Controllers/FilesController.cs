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
            _filesDirectory = System.Configuration.ConfigurationManager.AppSettings["DacpacFilesPath"] 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
            
            // Ensure directory exists
            if (!Directory.Exists(_filesDirectory))
            {
                Directory.CreateDirectory(_filesDirectory);
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
}

