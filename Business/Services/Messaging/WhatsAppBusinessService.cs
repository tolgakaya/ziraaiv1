using Business.Services.Messaging;
using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    /// <summary>
    /// WhatsApp Business API integration for rich messaging
    /// Supports templates, media messages, and advanced delivery tracking
    /// </summary>
    public class WhatsAppBusinessService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppBusinessService> _logger;
        private readonly string _baseUrl;
        private readonly string _accessToken;
        private readonly string _businessPhoneNumberId;
        private readonly string _webhookVerifyToken;

        public WhatsAppBusinessService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WhatsAppBusinessService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // WhatsApp Business API configuration
            _baseUrl = _configuration["WhatsApp:BaseUrl"] ?? "https://graph.facebook.com/v18.0";
            _accessToken = _configuration["WhatsApp:AccessToken"];
            _businessPhoneNumberId = _configuration["WhatsApp:BusinessPhoneNumberId"];
            _webhookVerifyToken = _configuration["WhatsApp:WebhookVerifyToken"];

            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZiraAI-WhatsApp-Service/1.0");
        }

        public async Task<IResult> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                var normalizedPhone = NormalizeTurkishPhoneNumber(phoneNumber);
                
                _logger.LogInformation("Sending WhatsApp message to {Phone}", normalizedPhone);

                var messageRequest = new
                {
                    messaging_product = "whatsapp",
                    to = normalizedPhone,
                    type = "text",
                    text = new { body = message }
                };

                var jsonContent = JsonSerializer.Serialize(messageRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/{_businessPhoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent);
                    
                    _logger.LogInformation("WhatsApp message sent successfully to {Phone}. MessageId: {MessageId}", 
                        normalizedPhone, result.Messages?[0]?.Id);
                        
                    return new SuccessResult(
                        $"WhatsApp mesajı başarıyla gönderildi. Mesaj ID: {result.Messages?[0]?.Id}");
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<WhatsAppErrorResponse>(responseContent);
                    _logger.LogError("WhatsApp message failed to {Phone}. Error: {Error}", 
                        normalizedPhone, errorResponse.Error?.Message);
                        
                    return new ErrorResult(
                        $"WhatsApp mesajı gönderilemedi: {errorResponse.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending WhatsApp message to {Phone}", phoneNumber);
                return new ErrorResult($"WhatsApp mesajı gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IResult> SendTemplateMessageAsync(string phoneNumber, string templateName, object templateParameters)
        {
            try
            {
                var normalizedPhone = NormalizeTurkishPhoneNumber(phoneNumber);
                
                _logger.LogInformation("Sending WhatsApp template {Template} to {Phone}", 
                    templateName, normalizedPhone);

                var templateRequest = new
                {
                    messaging_product = "whatsapp",
                    to = normalizedPhone,
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = "tr" }, // Turkish templates
                        components = BuildTemplateComponents(templateParameters)
                    }
                };

                var jsonContent = JsonSerializer.Serialize(templateRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/{_businessPhoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent);
                    
                    _logger.LogInformation("WhatsApp template sent successfully to {Phone}. MessageId: {MessageId}", 
                        normalizedPhone, result.Messages?[0]?.Id);
                        
                    return new SuccessResult(
                        $"WhatsApp şablon mesajı başarıyla gönderildi. Mesaj ID: {result.Messages?[0]?.Id}");
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<WhatsAppErrorResponse>(responseContent);
                    _logger.LogError("WhatsApp template failed to {Phone}. Error: {Error}", 
                        normalizedPhone, errorResponse.Error?.Message);
                        
                    return new ErrorResult(
                        $"WhatsApp şablon mesajı gönderilemedi: {errorResponse.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending WhatsApp template to {Phone}", phoneNumber);
                return new ErrorResult($"WhatsApp şablon mesajı gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IResult> SendBulkMessageAsync(BulkWhatsAppRequest request)
        {
            try
            {
                _logger.LogInformation("Sending bulk WhatsApp messages to {Count} recipients", 
                    request.Recipients.Length);

                var successCount = 0;
                var failureCount = 0;
                var results = new List<string>();

                foreach (var recipient in request.Recipients)
                {
                    try
                    {
                        IResult result;
                        
                        if (request.UseTemplate && !string.IsNullOrEmpty(request.TemplateName))
                        {
                            result = await SendTemplateMessageAsync(
                                recipient.PhoneNumber, 
                                request.TemplateName, 
                                recipient.TemplateParameters);
                        }
                        else
                        {
                            var personalizedMessage = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                                ? recipient.PersonalizedMessage
                                : request.Message?.Replace("{name}", recipient.Name ?? "Değerli Kullanıcı");
                                
                            result = await SendMessageAsync(recipient.PhoneNumber, personalizedMessage);
                        }

                        if (result.Success)
                        {
                            successCount++;
                            results.Add($"✅ {recipient.PhoneNumber}: Başarılı");
                        }
                        else
                        {
                            failureCount++;
                            results.Add($"❌ {recipient.PhoneNumber}: {result.Message}");
                        }

                        // Rate limiting - WhatsApp has strict rate limits
                        await Task.Delay(1000); // 1 second delay between messages
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        results.Add($"❌ {recipient.PhoneNumber}: {ex.Message}");
                        _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", recipient.PhoneNumber);
                    }
                }

                _logger.LogInformation("Bulk WhatsApp completed. Success: {Success}, Failed: {Failed}", 
                    successCount, failureCount);

                return new SuccessResult(
                    $"Toplu WhatsApp mesajı gönderildi. Başarılı: {successCount}, Başarısız: {failureCount}\n" +
                    string.Join("\n", results));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending bulk WhatsApp messages");
                return new ErrorResult($"Toplu WhatsApp mesajı gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IDataResult<WhatsAppDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
        {
            try
            {
                // WhatsApp uses webhooks for delivery status, so this is mainly for logging
                // In production, you'd query your database for webhook-received status updates
                
                _logger.LogInformation("Querying WhatsApp delivery status for {MessageId}", messageId);

                // For now, return a mock response - in production, query your webhook data storage
                var status = new WhatsAppDeliveryStatus
                {
                    MessageId = messageId,
                    Status = "sent", // This would come from webhook data
                    SentDate = DateTime.Now,
                    Provider = "WhatsApp Business"
                };

                return new SuccessDataResult<WhatsAppDeliveryStatus>(status,
                    "WhatsApp durumu webhook verilerinden alınmalıdır");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting WhatsApp delivery status for {MessageId}", messageId);
                return new ErrorDataResult<WhatsAppDeliveryStatus>(
                    $"WhatsApp durum sorgulama sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IDataResult<WhatsAppAccountInfo>> GetAccountInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/{_businessPhoneNumberId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<WhatsAppPhoneNumberInfo>(responseContent);
                    
                    var accountInfo = new WhatsAppAccountInfo
                    {
                        BusinessPhoneNumber = result.DisplayPhoneNumber,
                        BusinessName = result.VerifiedName ?? "ZiraAI",
                        AccountStatus = result.Quality?.Quality ?? "UNKNOWN",
                        IsVerified = !string.IsNullOrEmpty(result.VerifiedName),
                        Currency = "USD" // WhatsApp billing is typically in USD
                    };

                    return new SuccessDataResult<WhatsAppAccountInfo>(accountInfo);
                }
                else
                {
                    return new ErrorDataResult<WhatsAppAccountInfo>(
                        $"WhatsApp hesap bilgisi alınamadı: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting WhatsApp account info");
                return new ErrorDataResult<WhatsAppAccountInfo>(
                    $"WhatsApp hesap bilgisi sorgulaması sırasında hata oluştu: {ex.Message}");
            }
        }

        private string NormalizeTurkishPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // Remove all non-digit characters
            phone = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");

            // Handle Turkish mobile number formats for WhatsApp
            if (phone.StartsWith("90") && phone.Length == 12)
            {
                return phone; // 90xxxxxxxxxx (WhatsApp format without +)
            }
            else if (phone.StartsWith("0") && phone.Length == 11)
            {
                return "9" + phone; // 90xxxxxxxxxx (remove leading 0, add 9)
            }
            else if (phone.Length == 10 && phone.StartsWith("5"))
            {
                return "90" + phone; // 90xxxxxxxxxx (add 90 prefix)
            }

            // Return original if format doesn't match expected patterns
            _logger.LogWarning("Unusual WhatsApp phone number format: {Phone}", phone);
            return phone;
        }

        private object[] BuildTemplateComponents(object templateParameters)
        {
            // This is a simplified template component builder
            // In production, you'd parse the templateParameters based on your template structure
            
            if (templateParameters == null)
                return new object[0];

            // Example for a simple sponsorship template with name and amount parameters
            var components = new List<object>();

            // Body component with parameters
            components.Add(new
            {
                type = "body",
                parameters = new[]
                {
                    new { type = "text", text = templateParameters.ToString() }
                }
            });

            return components.ToArray();
        }

        #region WhatsApp API Response Models

        private class WhatsAppMessageResponse
        {
            public string Messaging_Product { get; set; }
            public WhatsAppContact[] Contacts { get; set; }
            public WhatsAppMessage[] Messages { get; set; }
        }

        private class WhatsAppContact
        {
            public string Input { get; set; }
            public string Wa_Id { get; set; }
        }

        private class WhatsAppMessage
        {
            public string Id { get; set; }
        }

        private class WhatsAppErrorResponse
        {
            public WhatsAppError Error { get; set; }
        }

        private class WhatsAppError
        {
            public string Message { get; set; }
            public string Type { get; set; }
            public int Code { get; set; }
            public string Error_Subcode { get; set; }
            public string Fbtrace_Id { get; set; }
        }

        private class WhatsAppPhoneNumberInfo
        {
            public string Id { get; set; }
            public string DisplayPhoneNumber { get; set; }
            public string VerifiedName { get; set; }
            public WhatsAppQuality Quality { get; set; }
        }

        private class WhatsAppQuality
        {
            public string Quality { get; set; }
        }

        #endregion
    }
}