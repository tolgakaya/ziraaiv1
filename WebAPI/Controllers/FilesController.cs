using Core.Utilities.Results;
using DataAccess.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Secure file serving controller with authorization
    /// Serves voice messages and attachments only to message participants
    /// </summary>
    [Authorize]
    [Route("api/v1/files")]
    [ApiController]
    [EnableCors("AllowFiles")]
    public class FilesController : BaseApiController
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FilesController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _basePath;

        public FilesController(
            IAnalysisMessageRepository messageRepository,
            IConfiguration configuration,
            ILogger<FilesController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _messageRepository = messageRepository;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _basePath = _configuration["FileStorage:Local:BasePath"] ?? "WebAPI/wwwroot/uploads";
        }

        /// <summary>
        /// Get voice message file (authorization required)
        /// Only sender and receiver can access
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <returns>Audio file with range support for seeking</returns>
        [HttpGet("voice-messages/{messageId}")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetVoiceMessage(int messageId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized voice message access attempt (no user): Message {MessageId}", messageId);
                return Unauthorized(new ErrorResult("Authentication required"));
            }

            // Get message from database
            var message = await _messageRepository.GetAsync(m => m.Id == messageId && !m.IsDeleted);
            if (message == null)
            {
                _logger.LogWarning("Voice message not found: {MessageId}", messageId);
                return NotFound(new ErrorResult("Voice message not found"));
            }

            // Authorization: Sender, receiver, or admin can access
            var isAdmin = User.HasClaim(c => c.Type.EndsWith("role") && c.Value == "Admin");
            var isParticipant = message.FromUserId == userId.Value || message.ToUserId == userId.Value;

            if (!isParticipant && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized voice message access attempt. User: {UserId}, Message: {MessageId}, From: {FromUserId}, To: {ToUserId}",
                    userId.Value, messageId, message.FromUserId, message.ToUserId);
                return Forbid();
            }

            // Get file path from VoiceMessageUrl
            if (string.IsNullOrEmpty(message.VoiceMessageUrl))
            {
                _logger.LogWarning("Voice message has no VoiceMessageUrl. Message: {MessageId}", messageId);
                return NotFound(new ErrorResult("Voice file not found"));
            }

            // Check if URL is external (e.g., FreeImageHost, ImgBB, Cloudflare R2, etc.)
            // Proxy the file through API to avoid CORS issues with redirect
            if (message.VoiceMessageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                message.VoiceMessageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Voice message proxy from external storage. User: {UserId}, Message: {MessageId}, Url: {Url}",
                    userId.Value, messageId, message.VoiceMessageUrl);

                // Proxy file through API with proper CORS headers
                return await ProxyExternalFile(message.VoiceMessageUrl, "audio/m4a");
            }

            // Local file - serve from disk
            var filePath = ExtractFilePathFromUrl(message.VoiceMessageUrl);
            var fullPath = GetFullPath(filePath);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogError("Voice file missing on disk. Message: {MessageId}, Path: {FilePath}", messageId, fullPath);
                return NotFound(new ErrorResult("Voice file not found on server"));
            }

            // Log access for audit trail
            _logger.LogInformation(
                "Voice message accessed. User: {UserId}, Message: {MessageId}, File: {FileName}, Size: {Size} bytes",
                userId.Value, messageId, Path.GetFileName(fullPath), new FileInfo(fullPath).Length);

            // Serve file with range support (enables audio seeking)
            return PhysicalFile(fullPath, "audio/m4a", enableRangeProcessing: true);
        }

        /// <summary>
        /// Get attachment file (authorization required)
        /// Only sender and receiver can access
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <param name="attachmentIndex">Attachment index (0-based)</param>
        /// <returns>Attachment file</returns>
        [HttpGet("attachments/{messageId}/{attachmentIndex}")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAttachment(int messageId, int attachmentIndex)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized attachment access attempt (no user): Message {MessageId}", messageId);
                return Unauthorized(new ErrorResult("Authentication required"));
            }

            // Get message from database
            var message = await _messageRepository.GetAsync(m => m.Id == messageId && !m.IsDeleted);
            if (message == null)
            {
                _logger.LogWarning("Message not found for attachment: {MessageId}", messageId);
                return NotFound(new ErrorResult("Message not found"));
            }

            // Authorization: Sender, receiver, or admin can access
            var isAdmin = User.HasClaim(c => c.Type.EndsWith("role") && c.Value == "Admin");
            var isParticipant = message.FromUserId == userId.Value || message.ToUserId == userId.Value;

            if (!isParticipant && !isAdmin)
            {
                _logger.LogWarning(
                    "Unauthorized attachment access attempt. User: {UserId}, Message: {MessageId}, From: {FromUserId}, To: {ToUserId}",
                    userId.Value, messageId, message.FromUserId, message.ToUserId);
                return Forbid();
            }

            // Deserialize attachment URLs from JSON
            if (string.IsNullOrEmpty(message.AttachmentUrls))
            {
                _logger.LogWarning("Message has no attachments. Message: {MessageId}", messageId);
                return NotFound(new ErrorResult("Attachment not found"));
            }

            string[] attachmentUrls;
            try
            {
                attachmentUrls = JsonSerializer.Deserialize<string[]>(message.AttachmentUrls);
            }
            catch (JsonException)
            {
                _logger.LogError("Failed to deserialize attachment URLs. Message: {MessageId}", messageId);
                return NotFound(new ErrorResult("Invalid attachment data"));
            }

            // Validate attachment index
            if (attachmentUrls == null || attachmentIndex >= attachmentUrls.Length || attachmentIndex < 0)
            {
                _logger.LogWarning("Attachment index out of range. Message: {MessageId}, Index: {Index}, Count: {Count}",
                    messageId, attachmentIndex, attachmentUrls?.Length ?? 0);
                return NotFound(new ErrorResult("Attachment not found"));
            }

            var attachmentUrl = attachmentUrls[attachmentIndex];

            // Check if URL is external (FreeImageHost, ImgBB, Cloudflare R2, etc.)
            // Proxy the file through API to avoid CORS issues with redirect
            if (attachmentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                attachmentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Attachment proxy from external storage. User: {UserId}, Message: {MessageId}, Index: {Index}, Url: {Url}",
                    userId.Value, messageId, attachmentIndex, attachmentUrl);

                // Determine content type from URL extension
                var attachmentContentType = GetContentType(attachmentUrl);

                // Proxy file through API with proper CORS headers
                return await ProxyExternalFile(attachmentUrl, attachmentContentType);
            }

            // Local file - serve from disk
            var filePath = ExtractFilePathFromUrl(attachmentUrl);
            var fullPath = GetFullPath(filePath);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogError("Attachment file missing on disk. Message: {MessageId}, Index: {Index}, Path: {FilePath}",
                    messageId, attachmentIndex, fullPath);
                return NotFound(new ErrorResult("Attachment file not found on server"));
            }

            // Determine content type from extension
            var contentType = GetContentType(fullPath);

            // Log access for audit trail
            _logger.LogInformation(
                "Attachment accessed. User: {UserId}, Message: {MessageId}, Index: {Index}, File: {FileName}, Type: {ContentType}, Size: {Size} bytes",
                userId.Value, messageId, attachmentIndex, Path.GetFileName(fullPath), contentType, new FileInfo(fullPath).Length);

            // Serve file with range support
            return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
        }

        #region Helper Methods

        /// <summary>
        /// Proxy external file through API to avoid CORS issues
        /// Downloads file from external URL and streams it with proper CORS headers
        /// </summary>
        private async Task<IActionResult> ProxyExternalFile(string externalUrl, string contentType)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Fetch file from external storage
                var response = await httpClient.GetAsync(externalUrl, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch external file. Url: {Url}, Status: {StatusCode}",
                        externalUrl, response.StatusCode);
                    return NotFound(new ErrorResult("File not found in external storage"));
                }

                // Get content stream
                var stream = await response.Content.ReadAsStreamAsync();

                // Determine content type (use from response or fallback to parameter)
                var responseContentType = response.Content.Headers.ContentType?.MediaType ?? contentType;

                _logger.LogInformation("Proxying external file. Url: {Url}, ContentType: {ContentType}, Size: {Size} bytes",
                    externalUrl, responseContentType, response.Content.Headers.ContentLength ?? 0);

                // Stream file with proper content type and range support
                // CORS headers automatically added by AllowFiles policy
                return File(stream, responseContentType, enableRangeProcessing: true);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching external file. Url: {Url}", externalUrl);
                return StatusCode(502, new ErrorResult("Failed to retrieve file from external storage"));
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while fetching external file. Url: {Url}", externalUrl);
                return StatusCode(504, new ErrorResult("Timeout while retrieving file from external storage"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while proxying external file. Url: {Url}", externalUrl);
                return StatusCode(500, new ErrorResult("Internal error while retrieving file"));
            }
        }

        /// <summary>
        /// Extract file path from URL (supports both old physical URLs and new API URLs)
        /// </summary>
        private string ExtractFilePathFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return string.Empty;

            // If it's already a relative path, return as is
            if (!fileUrl.StartsWith("http"))
                return fileUrl;

            // Extract path from URL
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;

            // Remove leading slash
            if (path.StartsWith("/"))
                path = path.Substring(1);

            // Remove "uploads/" prefix if present
            if (path.StartsWith("uploads/"))
                path = path.Substring(8);

            return path;
        }

        /// <summary>
        /// Get full physical path from relative path
        /// </summary>
        private string GetFullPath(string relativePath)
        {
            // Handle both absolute and relative base paths
            if (Path.IsPathRooted(_basePath))
            {
                return Path.Combine(_basePath, relativePath);
            }
            else
            {
                // Relative to application root
                var appRoot = Directory.GetCurrentDirectory();
                return Path.Combine(appRoot, _basePath, relativePath);
            }
        }

        /// <summary>
        /// Determine content type from file extension
        /// </summary>
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                // Audio
                ".m4a" => "audio/m4a",
                ".mp3" => "audio/mpeg",
                ".aac" => "audio/aac",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",

                // Images
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".tiff" or ".tif" => "image/tiff",

                // Video
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".webm" => "video/webm",

                // Documents
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",

                // Archives
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",

                // Default
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Get current user ID from JWT claims
        /// </summary>
        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        #endregion
    }
}
