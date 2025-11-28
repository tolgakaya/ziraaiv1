using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Services.FileStorage
{
    /// <summary>
    /// Cloudflare R2 storage service implementation using S3-compatible API
    /// Zero egress fees, built-in CDN, cost-effective alternative to AWS S3
    /// </summary>
    public class CloudflareR2StorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CloudflareR2StorageService> _logger;
        private readonly string _bucketName;
        private readonly string _publicDomain;
        private readonly string _accountId;

        public string ProviderType => StorageProviders.CloudflareR2;
        public string BaseUrl => _publicDomain;

        public CloudflareR2StorageService(
            IConfiguration configuration,
            ILogger<CloudflareR2StorageService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Read configuration
            _accountId = _configuration["FileStorage:CloudflareR2:AccountId"];
            var accessKeyId = _configuration["FileStorage:CloudflareR2:AccessKeyId"];
            var secretAccessKey = _configuration["FileStorage:CloudflareR2:SecretAccessKey"];
            _bucketName = _configuration["FileStorage:CloudflareR2:BucketName"];
            _publicDomain = _configuration["FileStorage:CloudflareR2:PublicDomain"]
                ?? $"https://{_bucketName}.{_accountId}.r2.cloudflarestorage.com";

            // Validate configuration
            ValidateConfiguration(_accountId, accessKeyId, secretAccessKey, _bucketName);

            // Initialize S3 client for R2
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = false
            };

            _s3Client = new AmazonS3Client(credentials, config);

            _logger.LogInformation(
                "[CloudflareR2] Initialized - Bucket: {BucketName}, Domain: {PublicDomain}",
                _bucketName,
                _publicDomain
            );
        }

        /// <summary>
        /// Upload file from byte array
        /// </summary>
        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("File bytes cannot be null or empty", nameof(fileBytes));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            var s3Key = GenerateS3Key(fileName, folder);

            try
            {
                _logger.LogInformation(
                    "[CloudflareR2] Uploading file - Key: {S3Key}, Size: {SizeKB} KB",
                    s3Key,
                    fileBytes.Length / 1024.0
                );

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key,
                    InputStream = new MemoryStream(fileBytes),
                    ContentType = contentType ?? "application/octet-stream",
                    CannedACL = S3CannedACL.PublicRead,
                    DisablePayloadSigning = true, // Required for Cloudflare R2 compatibility (R2 doesn't support STREAMING-AWS4-HMAC-SHA256-PAYLOAD)
                    Metadata =
                    {
                        ["x-amz-meta-original-filename"] = fileName,
                        ["x-amz-meta-upload-timestamp"] = DateTime.UtcNow.ToString("O"),
                        ["x-amz-meta-content-length"] = fileBytes.Length.ToString()
                    }
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError(
                        "[CloudflareR2] Upload failed - StatusCode: {StatusCode}",
                        response.HttpStatusCode
                    );
                    throw new InvalidOperationException($"R2 upload failed with status code: {response.HttpStatusCode}");
                }

                var publicUrl = GeneratePublicUrl(s3Key);
                _logger.LogInformation("[CloudflareR2] Upload successful - URL: {PublicUrl}", publicUrl);

                return publicUrl;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[CloudflareR2] S3 exception during upload - Key: {S3Key}, ErrorCode: {ErrorCode}",
                    s3Key,
                    ex.ErrorCode
                );
                throw new InvalidOperationException($"Cloudflare R2 upload failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CloudflareR2] Unexpected error during upload - Key: {S3Key}", s3Key);
                throw;
            }
        }

        /// <summary>
        /// Upload file from stream
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            // Convert stream to byte array for metadata and validation
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            return await UploadFileAsync(fileBytes, fileName, contentType, folder);
        }

        /// <summary>
        /// Upload image from base64 data URI
        /// </summary>
        public async Task<string> UploadImageFromDataUriAsync(string dataUri, string fileName, string folder = null)
        {
            if (string.IsNullOrEmpty(dataUri))
                throw new ArgumentException("Data URI cannot be null or empty", nameof(dataUri));

            if (!dataUri.StartsWith("data:"))
                throw new ArgumentException("Invalid data URI format", nameof(dataUri));

            try
            {
                // Parse data URI: "data:image/jpeg;base64,/9j/4AAQ..."
                var parts = dataUri.Split(',');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid data URI format - missing comma separator", nameof(dataUri));

                // Extract MIME type from header
                var mimeType = ExtractMimeType(parts[0]);
                var extension = GetExtensionFromMimeType(mimeType);
                var fileNameWithExtension = fileName.EndsWith(extension)
                    ? fileName
                    : $"{fileName}{extension}";

                // Decode base64
                var base64Data = parts[1];
                var fileBytes = Convert.FromBase64String(base64Data);

                _logger.LogInformation(
                    "[CloudflareR2] Uploading image from DataURI - FileName: {FileName}, MimeType: {MimeType}, Size: {SizeKB} KB",
                    fileNameWithExtension,
                    mimeType,
                    fileBytes.Length / 1024.0
                );

                return await UploadFileAsync(fileBytes, fileNameWithExtension, mimeType, folder);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "[CloudflareR2] Invalid base64 format in data URI");
                throw new ArgumentException("Invalid base64 data in data URI", nameof(dataUri), ex);
            }
        }

        /// <summary>
        /// Delete file from R2 storage
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                _logger.LogWarning("[CloudflareR2] Delete called with null or empty URL");
                return false;
            }

            try
            {
                var s3Key = ExtractS3KeyFromUrl(fileUrl);

                _logger.LogInformation("[CloudflareR2] Deleting file - Key: {S3Key}", s3Key);

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key
                };

                var response = await _s3Client.DeleteObjectAsync(deleteRequest);

                var success = response.HttpStatusCode == HttpStatusCode.NoContent;
                _logger.LogInformation(
                    "[CloudflareR2] Delete {Result} - Key: {S3Key}",
                    success ? "successful" : "failed",
                    s3Key
                );

                return success;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[CloudflareR2] File not found for deletion - URL: {FileUrl}", fileUrl);
                return false;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[CloudflareR2] S3 exception during delete - URL: {FileUrl}, ErrorCode: {ErrorCode}",
                    fileUrl,
                    ex.ErrorCode
                );
                throw new InvalidOperationException($"Cloudflare R2 delete failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CloudflareR2] Unexpected error during delete - URL: {FileUrl}", fileUrl);
                throw;
            }
        }

        /// <summary>
        /// Check if file exists in R2 storage
        /// </summary>
        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return false;

            try
            {
                var s3Key = ExtractS3KeyFromUrl(fileUrl);

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CloudflareR2] Error checking file existence - URL: {FileUrl}", fileUrl);
                return false;
            }
        }

        /// <summary>
        /// Get file size from R2 storage
        /// </summary>
        public async Task<long> GetFileSizeAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return -1;

            try
            {
                var s3Key = ExtractS3KeyFromUrl(fileUrl);

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key
                };

                var metadata = await _s3Client.GetObjectMetadataAsync(request);
                return metadata.ContentLength;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[CloudflareR2] File not found for size check - URL: {FileUrl}", fileUrl);
                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CloudflareR2] Error getting file size - URL: {FileUrl}", fileUrl);
                return -1;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Generate S3 key with timestamp-based unique naming
        /// </summary>
        private string GenerateS3Key(string fileName, string folder)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{timestamp}_{Guid.NewGuid():N}_{sanitizedFileName}";

            if (string.IsNullOrEmpty(folder))
            {
                return uniqueFileName;
            }

            var sanitizedFolder = folder.Trim('/');
            return $"{sanitizedFolder}/{uniqueFileName}";
        }

        /// <summary>
        /// Generate public URL for uploaded file
        /// </summary>
        private string GeneratePublicUrl(string s3Key)
        {
            // Use custom domain if configured, otherwise use R2 public URL
            return $"{_publicDomain.TrimEnd('/')}/{s3Key}";
        }

        /// <summary>
        /// Extract S3 key from public URL
        /// </summary>
        private string ExtractS3KeyFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty", nameof(fileUrl));

            // Remove domain part to get S3 key
            var uri = new Uri(fileUrl);
            var s3Key = uri.AbsolutePath.TrimStart('/');

            if (string.IsNullOrEmpty(s3Key))
                throw new ArgumentException("Invalid file URL - cannot extract S3 key", nameof(fileUrl));

            return s3Key;
        }

        /// <summary>
        /// Sanitize file name for S3 compatibility
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();

            foreach (var c in fileName)
            {
                if (Array.IndexOf(invalidChars, c) == -1)
                    sanitized.Append(c);
                else
                    sanitized.Append('_');
            }

            return sanitized.ToString();
        }

        /// <summary>
        /// Extract MIME type from data URI header
        /// </summary>
        private string ExtractMimeType(string dataUriHeader)
        {
            // Format: "data:image/jpeg;base64"
            var mimeTypePart = dataUriHeader.Replace("data:", "").Split(';')[0];
            return string.IsNullOrEmpty(mimeTypePart) ? "application/octet-stream" : mimeTypePart;
        }

        /// <summary>
        /// Get file extension from MIME type
        /// </summary>
        private string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType?.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                "image/tiff" => ".tiff",
                "application/pdf" => ".pdf",
                "text/plain" => ".txt",
                _ => ".bin"
            };
        }

        /// <summary>
        /// Validate required configuration settings
        /// </summary>
        private void ValidateConfiguration(string accountId, string accessKeyId, string secretAccessKey, string bucketName)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new InvalidOperationException("Cloudflare R2 Account ID is not configured");

            if (string.IsNullOrEmpty(accessKeyId))
                throw new InvalidOperationException("Cloudflare R2 Access Key ID is not configured");

            if (string.IsNullOrEmpty(secretAccessKey))
                throw new InvalidOperationException("Cloudflare R2 Secret Access Key is not configured");

            if (string.IsNullOrEmpty(bucketName))
                throw new InvalidOperationException("Cloudflare R2 Bucket Name is not configured");

            _logger.LogInformation(
                "[CloudflareR2] Configuration validated - AccountId: {AccountId}, Bucket: {BucketName}",
                accountId,
                bucketName
            );
        }

        #endregion
    }
}
