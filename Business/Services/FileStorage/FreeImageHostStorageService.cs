using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.FileStorage
{
    /// <summary>
    /// FreeImage.host storage service for free image hosting
    /// Perfect for development and testing with public URLs and 64MB file support
    /// </summary>
    public class FreeImageHostStorageService : IFileStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FreeImageHostStorageService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://freeimage.host/api/1/upload";

        public string ProviderType => StorageProviders.FreeImageHost;
        public string BaseUrl => "https://iili.io";

        public FreeImageHostStorageService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<FreeImageHostStorageService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["FileStorage:FreeImageHost:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("FreeImage.host API key is required. Set FileStorage:FreeImageHost:ApiKey in configuration.");
            }
        }

        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
        {
            try
            {
                // FreeImage.host only supports images
                if (!IsImageContentType(contentType))
                {
                    throw new ArgumentException($"FreeImage.host only supports image files. Content type: {contentType}");
                }

                // Convert to base64
                var base64String = Convert.ToBase64String(fileBytes);
                
                return await UploadToFreeImageHost(base64String, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {fileName} to FreeImage.host");
                throw new InvalidOperationException($"Failed to upload to FreeImage.host: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null)
        {
            try
            {
                // Convert stream to bytes
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                return await UploadFileAsync(fileBytes, fileName, contentType, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {fileName} from stream to FreeImage.host");
                throw new InvalidOperationException($"Failed to upload stream to FreeImage.host: {ex.Message}", ex);
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

                // Extract base64 data (FreeImage.host accepts base64 directly)
                var base64Data = parts[1];
                
                return await UploadToFreeImageHost(base64Data, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload image from data URI to FreeImage.host: {fileName}");
                throw new InvalidOperationException($"Failed to upload data URI to FreeImage.host: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            // FreeImage.host doesn't provide a public delete API for free accounts
            // Images auto-expire based on account settings
            _logger.LogWarning("FreeImage.host doesn't support file deletion through API for free accounts");
            return false;
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                // Try to HEAD request the URL
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, fileUrl));
                return response.IsSuccessStatusCode;
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
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, fileUrl));
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                {
                    return response.Content.Headers.ContentLength.Value;
                }
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private async Task<string> UploadToFreeImageHost(string base64Data, string fileName)
        {
            try
            {
                _logger.LogInformation($"[FreeImageHost] Starting upload for file: {fileName}");
                
                // Prepare form data for FreeImage.host API
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(_apiKey), "key");
                formData.Add(new StringContent("upload"), "action");
                formData.Add(new StringContent(base64Data), "source");
                formData.Add(new StringContent("json"), "format");

                // Upload to FreeImage.host
                var response = await _httpClient.PostAsync(_apiUrl, formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"FreeImage.host upload failed: {response.StatusCode} - {responseContent}");
                    throw new HttpRequestException($"FreeImage.host upload failed: {response.StatusCode}");
                }

                // Parse response
                var result = JsonConvert.DeserializeObject<FreeImageHostResponse>(responseContent);
                
                if (result?.Success?.Code != 200 || string.IsNullOrEmpty(result.Image?.Url))
                {
                    var errorMessage = result?.Error?.Message ?? "Unknown error";
                    throw new InvalidOperationException($"FreeImage.host returned error: {errorMessage}");
                }

                _logger.LogInformation($"[FreeImageHost] Upload successful: {result.Image.Url}");
                
                return result.Image.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FreeImageHost] Upload failed, falling back to local storage might occur");
                throw;
            }
        }

        private bool IsImageContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        #region FreeImage.host API Response Models

        private class FreeImageHostResponse
        {
            [JsonProperty("status_code")]
            public int StatusCode { get; set; }

            [JsonProperty("success")]
            public FreeImageHostSuccess Success { get; set; }

            [JsonProperty("image")]
            public FreeImageHostImage Image { get; set; }

            [JsonProperty("status_txt")]
            public string StatusText { get; set; }

            [JsonProperty("error")]
            public FreeImageHostError Error { get; set; }
        }

        private class FreeImageHostSuccess
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("code")]
            public int Code { get; set; }
        }

        private class FreeImageHostError
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("code")]
            public int Code { get; set; }
        }

        private class FreeImageHostImage
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("date")]
            public string Date { get; set; }

            [JsonProperty("date_gmt")]
            public string DateGmt { get; set; }

            [JsonProperty("storage_id")]
            public string StorageId { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("nsfw")]
            public string Nsfw { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("id_encoded")]
            public string IdEncoded { get; set; }

            [JsonProperty("filename")]
            public string Filename { get; set; }

            [JsonProperty("mime")]
            public string Mime { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("url_viewer")]
            public string UrlViewer { get; set; }

            [JsonProperty("thumb")]
            public FreeImageHostThumb Thumb { get; set; }

            [JsonProperty("medium")]
            public FreeImageHostMedium Medium { get; set; }

            [JsonProperty("delete_url")]
            public string DeleteUrl { get; set; }
        }

        private class FreeImageHostThumb
        {
            [JsonProperty("filename")]
            public string Filename { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }

        private class FreeImageHostMedium
        {
            [JsonProperty("filename")]
            public string Filename { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }

        #endregion
    }
}