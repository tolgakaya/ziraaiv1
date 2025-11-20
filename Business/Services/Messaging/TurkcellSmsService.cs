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
    /// Turkcell SMS API integration for Turkey market
    /// Supports bulk SMS sending, delivery tracking, and balance management
    /// </summary>
    public class TurkcellSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TurkcellSmsService> _logger;
        private readonly string _apiUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _senderId;

        public TurkcellSmsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TurkcellSmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            // Turkcell SMS API configuration
            _apiUrl = _configuration["SmsProvider:Turkcell:ApiUrl"] ?? "https://sms.turkcell.com.tr/api";
            _username = _configuration["SmsProvider:Turkcell:Username"];
            _password = _configuration["SmsProvider:Turkcell:Password"];
            _senderId = _configuration["SmsProvider:Turkcell:SenderId"] ?? "ZiraAI";

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_apiUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZiraAI-SMS-Service/1.0");
        }

        public async Task<IResult> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Normalize Turkish phone number
                var normalizedPhone = NormalizeTurkishPhoneNumber(phoneNumber);
                
                _logger.LogInformation("Sending SMS to {Phone} via Turkcell", normalizedPhone);

                var smsRequest = new
                {
                    username = _username,
                    password = _password,
                    sender = _senderId,
                    message = message,
                    phones = new[] { normalizedPhone },
                    unicode = ContainsTurkishChars(message) ? "1" : "0",
                    encoding = "UTF-8"
                };

                var jsonContent = JsonSerializer.Serialize(smsRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/send", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TurkcellSmsResponse>(responseContent);
                    
                    if (result.Status == "Success")
                    {
                        _logger.LogInformation("SMS sent successfully to {Phone}. MessageId: {MessageId}", 
                            normalizedPhone, result.MessageId);
                        return new SuccessResult($"SMS başarıyla gönderildi. Mesaj ID: {result.MessageId}");
                    }
                    else
                    {
                        _logger.LogError("SMS sending failed to {Phone}. Error: {Error}", 
                            normalizedPhone, result.ErrorMessage);
                        return new ErrorResult($"SMS gönderilemedi: {result.ErrorMessage}");
                    }
                }
                else
                {
                    _logger.LogError("HTTP error sending SMS to {Phone}. Status: {Status}, Content: {Content}", 
                        normalizedPhone, response.StatusCode, responseContent);
                    return new ErrorResult($"SMS servisi hatası: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending SMS to {Phone}", phoneNumber);
                return new ErrorResult($"SMS gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IResult> SendOtpAsync(string phoneNumber, string otpCode)
        {
            try
            {
                var normalizedPhone = NormalizeTurkishPhoneNumber(phoneNumber);
                var otpMessage = $"Dogrulama kodunuz: {otpCode}. Bu kodu kimseyle paylasmayin.";

                _logger.LogInformation("Sending OTP SMS to {Phone} via Turkcell", normalizedPhone);

                var smsRequest = new
                {
                    username = _username,
                    password = _password,
                    sender = _senderId,
                    message = otpMessage,
                    phones = new[] { normalizedPhone },
                    unicode = "0", // OTP messages should use ASCII for faster delivery
                    encoding = "UTF-8",
                    priority = "high" // Request high priority for OTP
                };

                var jsonContent = JsonSerializer.Serialize(smsRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/send", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TurkcellSmsResponse>(responseContent);

                    if (result.Status == "Success")
                    {
                        _logger.LogInformation("OTP SMS sent successfully to {Phone}. MessageId: {MessageId}",
                            normalizedPhone, result.MessageId);
                        return new SuccessResult($"OTP SMS başarıyla gönderildi. Mesaj ID: {result.MessageId}");
                    }
                    else
                    {
                        _logger.LogError("OTP SMS sending failed to {Phone}. Error: {Error}",
                            normalizedPhone, result.ErrorMessage);
                        return new ErrorResult($"OTP SMS gönderilemedi: {result.ErrorMessage}");
                    }
                }
                else
                {
                    _logger.LogError("HTTP error sending OTP SMS to {Phone}. Status: {Status}, Content: {Content}",
                        normalizedPhone, response.StatusCode, responseContent);
                    return new ErrorResult($"OTP SMS servisi hatası: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending OTP SMS to {Phone}", phoneNumber);
                return new ErrorResult($"OTP SMS gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IResult> SendBulkSmsAsync(BulkSmsRequest request)
        {
            try
            {
                _logger.LogInformation("Sending bulk SMS to {Count} recipients", request.Recipients.Length);

                var phones = new List<string>();
                var personalizedMessages = new Dictionary<string, string>();

                foreach (var recipient in request.Recipients)
                {
                    var normalizedPhone = NormalizeTurkishPhoneNumber(recipient.PhoneNumber);
                    phones.Add(normalizedPhone);
                    
                    // Use personalized message if available, otherwise default message
                    var finalMessage = !string.IsNullOrEmpty(recipient.PersonalizedMessage) 
                        ? recipient.PersonalizedMessage 
                        : request.Message.Replace("{name}", recipient.Name ?? "Değerli Kullanıcı");
                    
                    personalizedMessages[normalizedPhone] = finalMessage;
                }

                var bulkRequest = new
                {
                    username = _username,
                    password = _password,
                    sender = request.SenderId ?? _senderId,
                    messages = personalizedMessages,
                    unicode = "1", // Always use unicode for Turkish support
                    encoding = "UTF-8",
                    scheduled = request.ScheduledSend ? "1" : "0",
                    scheduledDate = request.ScheduledDate?.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var jsonContent = JsonSerializer.Serialize(bulkRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/bulk-send", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TurkcellBulkSmsResponse>(responseContent);
                    
                    _logger.LogInformation("Bulk SMS sent. Success: {Success}, Failed: {Failed}, Total Cost: {Cost} TL", 
                        result.SuccessCount, result.FailedCount, result.TotalCost);
                        
                    return new SuccessResult(
                        $"Toplu SMS gönderildi. Başarılı: {result.SuccessCount}, " +
                        $"Başarısız: {result.FailedCount}, Toplam Maliyet: {result.TotalCost} TL");
                }
                else
                {
                    _logger.LogError("Bulk SMS HTTP error. Status: {Status}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    return new ErrorResult($"Toplu SMS servisi hatası: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending bulk SMS");
                return new ErrorResult($"Toplu SMS gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/status/{messageId}?username={_username}&password={_password}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TurkcellStatusResponse>(responseContent);
                    
                    var status = new SmsDeliveryStatus
                    {
                        MessageId = messageId,
                        PhoneNumber = result.PhoneNumber,
                        Status = MapTurkcellStatus(result.Status),
                        SentDate = result.SentDate,
                        DeliveredDate = result.DeliveredDate,
                        ErrorMessage = result.ErrorMessage,
                        Cost = result.Cost,
                        Provider = "Turkcell"
                    };

                    return new SuccessDataResult<SmsDeliveryStatus>(status);
                }
                else
                {
                    return new ErrorDataResult<SmsDeliveryStatus>(
                        $"Durum sorgulama hatası: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting SMS delivery status for {MessageId}", messageId);
                return new ErrorDataResult<SmsDeliveryStatus>(
                    $"Durum sorgulama sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IDataResult<SmsSenderInfo>> GetSenderInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/account?username={_username}&password={_password}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TurkcellAccountResponse>(responseContent);
                    
                    var info = new SmsSenderInfo
                    {
                        SenderId = _senderId,
                        Balance = result.Balance,
                        Currency = "TL",
                        MonthlyQuota = result.MonthlyQuota,
                        UsedQuota = result.UsedQuota,
                        Provider = "Turkcell",
                        IsActive = result.IsActive
                    };

                    return new SuccessDataResult<SmsSenderInfo>(info);
                }
                else
                {
                    return new ErrorDataResult<SmsSenderInfo>(
                        $"Hesap bilgisi alınamadı: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting SMS sender info");
                return new ErrorDataResult<SmsSenderInfo>(
                    $"Hesap bilgisi sorgulaması sırasında hata oluştu: {ex.Message}");
            }
        }

        private string NormalizeTurkishPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // Remove all non-digit characters
            phone = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");

            // Handle Turkish mobile number formats
            if (phone.StartsWith("90") && phone.Length == 12)
            {
                return "+" + phone; // +90xxxxxxxxxx
            }
            else if (phone.StartsWith("0") && phone.Length == 11)
            {
                return "+9" + phone; // +90xxxxxxxxxx (remove leading 0, add 90)
            }
            else if (phone.Length == 10 && phone.StartsWith("5"))
            {
                return "+90" + phone; // +90xxxxxxxxxx (add +90 prefix)
            }
            else if (phone.StartsWith("5") && phone.Length == 10)
            {
                return "+90" + phone; // +90xxxxxxxxxx
            }

            // Return original if format doesn't match expected patterns
            _logger.LogWarning("Unusual phone number format: {Phone}", phone);
            return phone.StartsWith("+") ? phone : "+" + phone;
        }

        private bool ContainsTurkishChars(string text)
        {
            return text.Contains('ç') || text.Contains('ğ') || text.Contains('ı') || 
                   text.Contains('ö') || text.Contains('ş') || text.Contains('ü') ||
                   text.Contains('Ç') || text.Contains('Ğ') || text.Contains('I') || 
                   text.Contains('İ') || text.Contains('Ö') || text.Contains('Ş') || 
                   text.Contains('Ü');
        }

        private string MapTurkcellStatus(string turkcellStatus)
        {
            return turkcellStatus?.ToLower() switch
            {
                "sent" => "Sent",
                "delivered" => "Delivered",
                "failed" => "Failed",
                "pending" => "Pending",
                "expired" => "Failed",
                "rejected" => "Failed",
                _ => "Unknown"
            };
        }

        #region Turkcell API Response Models

        private class TurkcellSmsResponse
        {
            public string Status { get; set; }
            public string MessageId { get; set; }
            public string ErrorMessage { get; set; }
            public decimal Cost { get; set; }
        }

        private class TurkcellBulkSmsResponse
        {
            public string Status { get; set; }
            public int SuccessCount { get; set; }
            public int FailedCount { get; set; }
            public decimal TotalCost { get; set; }
            public Dictionary<string, string> MessageIds { get; set; }
            public List<string> FailedNumbers { get; set; }
        }

        private class TurkcellStatusResponse
        {
            public string MessageId { get; set; }
            public string PhoneNumber { get; set; }
            public string Status { get; set; }
            public DateTime SentDate { get; set; }
            public DateTime? DeliveredDate { get; set; }
            public string ErrorMessage { get; set; }
            public decimal Cost { get; set; }
        }

        private class TurkcellAccountResponse
        {
            public decimal Balance { get; set; }
            public int MonthlyQuota { get; set; }
            public int UsedQuota { get; set; }
            public bool IsActive { get; set; }
        }

        #endregion
    }
}