using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Services.FileStorage
{
    /// <summary>
    /// Generic file storage service interface supporting multiple providers (Local, S3, ImgBB, etc.)
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Upload file from bytes with automatic URL generation
        /// </summary>
        /// <param name="fileBytes">File content as byte array</param>
        /// <param name="fileName">Original filename with extension</param>
        /// <param name="contentType">MIME content type (e.g., "image/jpeg")</param>
        /// <param name="folder">Optional folder/prefix for organization</param>
        /// <returns>Public URL to access the uploaded file</returns>
        Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null);

        /// <summary>
        /// Upload file from stream with automatic URL generation
        /// </summary>
        /// <param name="fileStream">File content as stream</param>
        /// <param name="fileName">Original filename with extension</param>
        /// <param name="contentType">MIME content type</param>
        /// <param name="folder">Optional folder/prefix for organization</param>
        /// <returns>Public URL to access the uploaded file</returns>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null);

        /// <summary>
        /// Upload image from base64 data URI with optimization
        /// </summary>
        /// <param name="dataUri">Base64 data URI (e.g., "data:image/jpeg;base64,...")</param>
        /// <param name="fileName">Generated filename (without extension)</param>
        /// <param name="folder">Optional folder for organization</param>
        /// <returns>Public URL to access the uploaded image</returns>
        Task<string> UploadImageFromDataUriAsync(string dataUri, string fileName, string folder = null);

        /// <summary>
        /// Delete file by URL or file path
        /// </summary>
        /// <param name="fileUrl">Public URL or file path to delete</param>
        /// <returns>True if deletion successful</returns>
        Task<bool> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Check if file exists
        /// </summary>
        /// <param name="fileUrl">Public URL or file path to check</param>
        /// <returns>True if file exists</returns>
        Task<bool> FileExistsAsync(string fileUrl);

        /// <summary>
        /// Get file size in bytes
        /// </summary>
        /// <param name="fileUrl">Public URL or file path</param>
        /// <returns>File size in bytes, -1 if not found</returns>
        Task<long> GetFileSizeAsync(string fileUrl);

        /// <summary>
        /// Get storage provider type
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Get base URL for the storage provider
        /// </summary>
        string BaseUrl { get; }
    }

    /// <summary>
    /// File upload result with metadata
    /// </summary>
    public class FileUploadResult
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public long SizeBytes { get; set; }
        public string ContentType { get; set; }
        public string Provider { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Folder { get; set; }
    }

    /// <summary>
    /// Storage provider types
    /// </summary>
    public static class StorageProviders
    {
        public const string Local = "Local";
        public const string S3 = "S3";
        public const string ImgBB = "ImgBB";
        public const string FreeImageHost = "FreeImageHost";
        public const string Azure = "Azure";
        public const string GoogleCloud = "GoogleCloud";
    }
}