using Core.Utilities.Results;
using DataAccess.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
    public class FilesController : BaseApiController
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FilesController> _logger;
        private readonly string _basePath;

        public FilesController(
            IAnalysisMessageRepository messageRepository,
            IConfiguration configuration,
            ILogger<FilesController> logger)
        {
            _messageRepository = messageRepository;
            _configuration = configuration;
            _logger = logger;
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

            // Authorization: Only sender or receiver can access
            if (message.FromUserId != userId.Value && message.ToUserId != userId.Value)
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

            // Check if URL is external (e.g., FreeImageHost, ImgBB, etc.) - though voice messages are typically local
            // This handles edge cases where voice files might be hosted externally
            if (message.VoiceMessageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                message.VoiceMessageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Voice message redirect to external storage. User: {UserId}, Message: {MessageId}, Url: {Url}",
                    userId.Value, messageId, message.VoiceMessageUrl);

                return Redirect(message.VoiceMessageUrl);
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

            // Authorization: Only sender or receiver can access
            if (message.FromUserId != userId.Value && message.ToUserId != userId.Value)
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

            // Check if URL is external (FreeImageHost, ImgBB, etc.) - redirect to external URL
            if (attachmentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                attachmentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Attachment redirect to external storage. User: {UserId}, Message: {MessageId}, Index: {Index}, Url: {Url}",
                    userId.Value, messageId, attachmentIndex, attachmentUrl);

                return Redirect(attachmentUrl);
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
