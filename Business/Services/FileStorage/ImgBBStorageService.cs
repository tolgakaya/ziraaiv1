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
    /// ImgBB (imgbb.com) storage service for free image hosting
    /// Perfect for development and testing with public URLs
    /// </summary>
    public class ImgBBStorageService : IFileStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImgBBStorageService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.imgbb.com/1/upload";

        public string ProviderType => StorageProviders.ImgBB;
        public string BaseUrl => "https://i.ibb.co";

        public ImgBBStorageService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ImgBBStorageService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["FileStorage:ImgBB:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("ImgBB API key is required. Set FileStorage:ImgBB:ApiKey in configuration.");
            }
        }

        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
        {
            try
            {
                // ImgBB only supports images
                if (!IsImageContentType(contentType))
                {
                    throw new ArgumentException($"ImgBB only supports image files. Content type: {contentType}");
                }

                // Convert to base64
                var base64String = Convert.ToBase64String(fileBytes);
                
                return await UploadToImgBB(base64String, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {fileName} to ImgBB");
                throw new InvalidOperationException($"Failed to upload to ImgBB: {ex.Message}", ex);
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
                _logger.LogError(ex, $"Failed to upload file {fileName} from stream to ImgBB");
                throw new InvalidOperationException($"Failed to upload stream to ImgBB: {ex.Message}", ex);
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

                // Extract base64 data (ImgBB accepts base64 directly)
                var base64Data = parts[1];
                
                return await UploadToImgBB(base64Data, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload image from data URI to ImgBB: {fileName}");
                throw new InvalidOperationException($"Failed to upload data URI to ImgBB: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            // ImgBB doesn't provide a public delete API for free accounts
            // Images auto-expire based on account settings
            _logger.LogWarning("ImgBB doesn't support file deletion through API for free accounts");
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

        private async Task<string> UploadToImgBB(string base64Data, string fileName)
        {
            try
            {
                // Prepare form data
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(_apiKey), "key");
                formData.Add(new StringContent(base64Data), "image");
                formData.Add(new StringContent(fileName), "name");

                // Optional: Set expiration (in seconds, max 15552000 = 180 days for free)
                var expiration = _configuration.GetValue<int>("FileStorage:ImgBB:ExpirationSeconds", 0);
                if (expiration > 0)
                {
                    formData.Add(new StringContent(expiration.ToString()), "expiration");
                }

                // Upload to ImgBB
                var response = await _httpClient.PostAsync(_apiUrl, formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"ImgBB upload failed: {response.StatusCode} - {responseContent}");
                    throw new HttpRequestException($"ImgBB upload failed: {response.StatusCode}");
                }

                // Parse response
                var result = JsonConvert.DeserializeObject<ImgBBResponse>(responseContent);
                
                if (result?.Success != true || string.IsNullOrEmpty(result.Data?.Url))
                {
                    throw new InvalidOperationException("ImgBB returned invalid response");
                }

                _logger.LogInformation($"Image uploaded to ImgBB successfully: {result.Data.Url}");
                
                return result.Data.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload to ImgBB API");
                throw;
            }
        }

        private bool IsImageContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        #region ImgBB API Response Models

        private class ImgBBResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("data")]
            public ImgBBData Data { get; set; }

            [JsonProperty("status")]
            public int Status { get; set; }
        }

        private class ImgBBData
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("url_viewer")]
            public string UrlViewer { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("display_url")]
            public string DisplayUrl { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("time")]
            public string Time { get; set; }

            [JsonProperty("expiration")]
            public string Expiration { get; set; }

            [JsonProperty("image")]
            public ImgBBImage Image { get; set; }

            [JsonProperty("thumb")]
            public ImgBBImage Thumb { get; set; }

            [JsonProperty("delete_url")]
            public string DeleteUrl { get; set; }
        }

        private class ImgBBImage
        {
            [JsonProperty("filename")]
            public string Filename { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("mime")]
            public string Mime { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }
        }

        #endregion
    }
}