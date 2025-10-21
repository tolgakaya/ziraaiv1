using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Services.FileStorage
{
    /// <summary>
    /// Local file storage implementation for development and testing
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _basePath;

        public string ProviderType => StorageProviders.Local;

        // ✅ IMPORTANT: BaseUrl must be dynamic to pick up HTTPS scheme correctly
        // Railway performs SSL termination, so we need to determine scheme at runtime
        public string BaseUrl => GetBaseUrl();

        public LocalFileStorageService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LocalFileStorageService> logger)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            // Get base path for file storage
            _basePath = _configuration["FileStorage:Local:BasePath"] ?? "wwwroot/uploads";

            // Ensure directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
        {
            try
            {
                var filePath = GenerateFilePath(fileName, folder);
                var fullPath = Path.Combine(_basePath, filePath);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write file
                await File.WriteAllBytesAsync(fullPath, fileBytes);
                
                _logger.LogInformation($"File uploaded to local storage: {filePath}");
                
                // Return public URL
                return GeneratePublicUrl(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {fileName} to local storage");
                throw new InvalidOperationException($"Failed to upload file: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null)
        {
            try
            {
                var filePath = GenerateFilePath(fileName, folder);
                var fullPath = Path.Combine(_basePath, filePath);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write file from stream
                using var fileStreamOutput = new FileStream(fullPath, FileMode.Create);
                await fileStream.CopyToAsync(fileStreamOutput);
                
                _logger.LogInformation($"File uploaded from stream to local storage: {filePath}");
                
                // Return public URL
                return GeneratePublicUrl(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {fileName} from stream to local storage");
                throw new InvalidOperationException($"Failed to upload file from stream: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadImageFromDataUriAsync(string dataUri, string fileName, string folder = null)
        {
            try
            {
                if (string.IsNullOrEmpty(dataUri))
                    throw new ArgumentException("Data URI is required");

                // Parse data URI
                var parts = dataUri.Split(',');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid data URI format");

                // Extract content type and extension
                var mimeType = ExtractMimeType(parts[0]);
                var extension = GetExtensionFromMimeType(mimeType);
                
                // Generate filename with extension
                var fileNameWithExtension = $"{fileName}{extension}";
                
                // Convert base64 to bytes
                var base64Data = parts[1];
                var fileBytes = Convert.FromBase64String(base64Data);

                // Upload using bytes method
                return await UploadFileAsync(fileBytes, fileNameWithExtension, mimeType, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload image from data URI: {fileName}");
                throw new InvalidOperationException($"Failed to upload image from data URI: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                var filePath = ExtractFilePathFromUrl(fileUrl);
                var fullPath = Path.Combine(_basePath, filePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"File deleted from local storage: {filePath}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete file: {fileUrl}");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                var filePath = ExtractFilePathFromUrl(fileUrl);
                var fullPath = Path.Combine(_basePath, filePath);
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> GetFileSizeAsync(string fileUrl)
        {
            try
            {
                var filePath = ExtractFilePathFromUrl(fileUrl);
                var fullPath = Path.Combine(_basePath, filePath);
                
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    return fileInfo.Length;
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private string GenerateFilePath(string fileName, string folder = null)
        {
            // Sanitize filename
            var sanitizedFileName = SanitizeFileName(fileName);
            
            // Add timestamp to prevent conflicts
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
            var extension = Path.GetExtension(sanitizedFileName);
            var uniqueFileName = $"{nameWithoutExtension}_{timestamp}{extension}";

            // Combine with folder if provided
            if (!string.IsNullOrEmpty(folder))
            {
                return Path.Combine(folder, uniqueFileName).Replace('\\', '/');
            }

            return uniqueFileName;
        }

        private string GeneratePublicUrl(string filePath)
        {
            // Convert backslashes to forward slashes for URLs
            var urlPath = filePath.Replace('\\', '/');
            
            // Remove leading slash if present
            if (urlPath.StartsWith("/"))
                urlPath = urlPath.Substring(1);

            // Get current base URL (dynamic)
            var currentBaseUrl = GetBaseUrl();
            
            // Return physical URL with uploads prefix
            // Note: Voice messages and attachments will be served via FilesController
            // This physical URL is stored internally for file path resolution
            return $"{currentBaseUrl}/uploads/{urlPath}";
        }

        private string GetBaseUrl()
        {
            // Priority 1: Configuration (most reliable for production)
            var configuredBaseUrl = _configuration["FileStorage:Local:BaseUrl"];
            if (!string.IsNullOrEmpty(configuredBaseUrl))
            {
                // Ensure HTTPS for production/staging environments
                if (configuredBaseUrl.StartsWith("http://") &&
                    !configuredBaseUrl.Contains("localhost"))
                {
                    configuredBaseUrl = configuredBaseUrl.Replace("http://", "https://");
                }
                return configuredBaseUrl;
            }

            // Priority 2: Try to get from HttpContext
            var request = _httpContextAccessor?.HttpContext?.Request;
            if (request != null)
            {
                var scheme = request.Scheme;
                var host = request.Host.ToString();

                // Force HTTPS for non-localhost environments (Railway SSL termination)
                if (!host.Contains("localhost") && scheme == "http")
                {
                    scheme = "https";
                }

                return $"{scheme}://{host}";
            }

            // Fallback
            return "https://localhost:5001";
        }

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

            return path;
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }
            
            return fileName;
        }

        private string ExtractMimeType(string dataUriPrefix)
        {
            // Extract from "data:image/jpeg;base64" -> "image/jpeg"
            var parts = dataUriPrefix.Split(':')[1].Split(';')[0];
            return parts;
        }

        private string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                "image/tiff" => ".tiff",
                _ => ".jpg" // Default to JPEG
            };
        }
    }
}