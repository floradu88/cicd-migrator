using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

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
                    var dacpacFiles = Directory.GetFiles(_filesDirectory, "*.dacpac");

                    foreach (var filePath in dacpacFiles)
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
}

