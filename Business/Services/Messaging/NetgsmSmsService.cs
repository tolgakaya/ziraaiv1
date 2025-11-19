using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Business.Services.Messaging
{
    /// <summary>
    /// NetGSM SMS API integration for Turkey market
    /// Supports single/bulk SMS sending, OTP, delivery tracking, and balance queries
    /// API Documentation: https://www.netgsm.com.tr/dokuman/
    /// </summary>
    public class NetgsmSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NetgsmSmsService> _logger;
        private readonly string _apiUrl;
        private readonly string _userCode;
        private readonly string _password;
        private readonly string _msgHeader;

        public NetgsmSmsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<NetgsmSmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // NetGSM API configuration - prioritize environment variables
            _apiUrl = Environment.GetEnvironmentVariable("NETGSM_API_URL")
                ?? _configuration["SmsProvider:Netgsm:ApiUrl"]
                ?? "https://api.netgsm.com.tr";

            _userCode = Environment.GetEnvironmentVariable("NETGSM_USERCODE")
                ?? _configuration["SmsProvider:Netgsm:UserCode"];

            _password = Environment.GetEnvironmentVariable("NETGSM_PASSWORD")
                ?? _configuration["SmsProvider:Netgsm:Password"];

            _msgHeader = Environment.GetEnvironmentVariable("NETGSM_MSGHEADER")
                ?? _configuration["SmsProvider:Netgsm:MsgHeader"]
                ?? "ZIRAAI";

            // Validate required credentials
            if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
            {
                _logger.LogWarning("NetGSM credentials not configured. SMS sending will fail. " +
                    "Set NETGSM_USERCODE and NETGSM_PASSWORD environment variables or configure in appsettings.");
            }

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_apiUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZiraAI-SMS-Service/1.0");
        }

        public async Task<IResult> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Validate credentials
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorResult("NetGSM kimlik bilgileri yapılandırılmamış. NETGSM_USERCODE ve NETGSM_PASSWORD ayarlayın.");
                }

                // Normalize Turkish phone number to NetGSM format (905xxxxxxxxx)
                var normalizedPhone = NormalizePhoneNumber(phoneNumber);

                _logger.LogInformation("Sending SMS to {Phone} via NetGSM", normalizedPhone);

                // Build query string for HTTP GET
                var queryParams = new Dictionary<string, string>
                {
                    { "usercode", _userCode },
                    { "password", _password },
                    { "gsmno", normalizedPhone },
                    { "message", message },
                    { "msgheader", _msgHeader }
                };

                // Add Turkish character encoding flag if needed
                if (ContainsTurkishChars(message))
                {
                    queryParams["dil"] = "TR"; // Turkish language encoding
                }

                var queryString = BuildQueryString(queryParams);
                var requestUrl = $"/sms/send/get/?{queryString}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse NetGSM response
                var result = ParseNetgsmResponse(responseContent);

                if (result.Success)
                {
                    _logger.LogInformation("SMS sent successfully to {Phone}. BulkId: {BulkId}",
                        normalizedPhone, result.BulkId);
                    return new SuccessResult($"SMS başarıyla gönderildi. Mesaj ID: {result.BulkId}");
                }
                else
                {
                    _logger.LogError("SMS sending failed to {Phone}. Error: {ErrorCode} - {ErrorMessage}",
                        normalizedPhone, result.ErrorCode, result.ErrorMessage);
                    return new ErrorResult($"SMS gönderilemedi: {result.ErrorMessage} (Kod: {result.ErrorCode})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending SMS to {Phone} via NetGSM", phoneNumber);
                return new ErrorResult($"SMS gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IResult> SendBulkSmsAsync(BulkSmsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorResult("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                _logger.LogInformation("Sending bulk SMS to {Count} recipients via NetGSM", request.Recipients.Length);

                var successCount = 0;
                var failedCount = 0;
                var messageIds = new List<string>();
                var failedNumbers = new List<string>();

                foreach (var recipient in request.Recipients)
                {
                    var message = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                        ? recipient.PersonalizedMessage
                        : request.Message.Replace("{name}", recipient.Name ?? "Değerli Kullanıcı");

                    var result = await SendSmsAsync(recipient.PhoneNumber, message);

                    if (result.Success)
                    {
                        successCount++;
                        // Extract message ID from success message
                        var match = Regex.Match(result.Message, @"Mesaj ID: (\d+)");
                        if (match.Success)
                        {
                            messageIds.Add(match.Groups[1].Value);
                        }
                    }
                    else
                    {
                        failedCount++;
                        failedNumbers.Add(recipient.PhoneNumber);
                    }

                    // Small delay between requests to avoid rate limiting
                    await Task.Delay(100);
                }

                _logger.LogInformation("Bulk SMS sent. Success: {Success}, Failed: {Failed}",
                    successCount, failedCount);

                return new SuccessResult(
                    $"Toplu SMS gönderildi. Başarılı: {successCount}, Başarısız: {failedCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending bulk SMS via NetGSM");
                return new ErrorResult($"Toplu SMS gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        public async Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorDataResult<SmsDeliveryStatus>("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                var queryParams = new Dictionary<string, string>
                {
                    { "usercode", _userCode },
                    { "password", _password },
                    { "bulkid", messageId },
                    { "type", "0" } // 0 = detailed report
                };

                var queryString = BuildQueryString(queryParams);
                var response = await _httpClient.GetAsync($"/sms/dlr/get/?{queryString}");
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse delivery report response
                var lines = responseContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                {
                    return new ErrorDataResult<SmsDeliveryStatus>("Durum bilgisi alınamadı.");
                }

                // First line contains status code
                var statusCode = lines[0].Trim();

                if (statusCode == "60")
                {
                    return new ErrorDataResult<SmsDeliveryStatus>("Mesaj bulunamadı.");
                }

                // Parse detailed status if available
                var status = new SmsDeliveryStatus
                {
                    MessageId = messageId,
                    Provider = "Netgsm",
                    Status = MapNetgsmDeliveryStatus(statusCode),
                    SentDate = DateTime.Now
                };

                // Try to parse additional details if present
                if (lines.Length > 1)
                {
                    var parts = lines[1].Split('|');
                    if (parts.Length >= 2)
                    {
                        status.PhoneNumber = parts[0];
                        status.Status = MapNetgsmDeliveryStatus(parts[1]);
                    }
                }

                return new SuccessDataResult<SmsDeliveryStatus>(status);
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
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorDataResult<SmsSenderInfo>("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                var queryParams = new Dictionary<string, string>
                {
                    { "usercode", _userCode },
                    { "password", _password }
                };

                var queryString = BuildQueryString(queryParams);
                var response = await _httpClient.GetAsync($"/balance/list/get/?{queryString}");
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse balance response
                var trimmedResponse = responseContent.Trim();

                // Check for error codes
                if (trimmedResponse == "30" || trimmedResponse == "70")
                {
                    return new ErrorDataResult<SmsSenderInfo>(
                        trimmedResponse == "30"
                            ? "Geçersiz kullanıcı adı veya şifre"
                            : "Hatalı sorgu parametresi");
                }

                // Parse balance (format: credit amount)
                if (decimal.TryParse(trimmedResponse.Split('|')[0], out var balance))
                {
                    var info = new SmsSenderInfo
                    {
                        SenderId = _msgHeader,
                        Balance = balance,
                        Currency = "TL",
                        MonthlyQuota = 0, // NetGSM uses credit-based system
                        UsedQuota = 0,
                        Provider = "Netgsm",
                        IsActive = balance > 0
                    };

                    return new SuccessDataResult<SmsSenderInfo>(info);
                }

                return new ErrorDataResult<SmsSenderInfo>($"Beklenmeyen yanıt formatı: {trimmedResponse}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting SMS sender info from NetGSM");
                return new ErrorDataResult<SmsSenderInfo>(
                    $"Hesap bilgisi sorgulaması sırasında hata oluştu: {ex.Message}");
            }
        }

        #region Helper Methods

        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // Remove all non-digit characters
            var digitsOnly = Regex.Replace(phone, @"\D", string.Empty);

            // NetGSM expects format: 905xxxxxxxxx (12 digits)
            if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
            {
                return digitsOnly; // Already in correct format
            }
            else if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
            {
                return "9" + digitsOnly; // 05xx -> 905xx
            }
            else if (digitsOnly.Length == 10 && digitsOnly.StartsWith("5"))
            {
                return "90" + digitsOnly; // 5xx -> 905xx
            }
            else if (digitsOnly.StartsWith("+90"))
            {
                return digitsOnly.Substring(1); // +90 -> 90
            }

            // Log warning for unusual format
            _logger.LogWarning("Unusual phone number format: {Phone}, using as-is: {Normalized}",
                phone, digitsOnly);

            return digitsOnly;
        }

        private bool ContainsTurkishChars(string text)
        {
            return text.Contains('ç') || text.Contains('ğ') || text.Contains('ı') ||
                   text.Contains('ö') || text.Contains('ş') || text.Contains('ü') ||
                   text.Contains('Ç') || text.Contains('Ğ') || text.Contains('İ') ||
                   text.Contains('Ö') || text.Contains('Ş') || text.Contains('Ü');
        }

        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (var param in parameters)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(HttpUtility.UrlEncode(param.Key));
                sb.Append('=');
                sb.Append(HttpUtility.UrlEncode(param.Value));
            }
            return sb.ToString();
        }

        private NetgsmResponseResult ParseNetgsmResponse(string response)
        {
            var trimmedResponse = response.Trim();
            var parts = trimmedResponse.Split(' ');
            var code = parts[0];

            return code switch
            {
                "00" => new NetgsmResponseResult
                {
                    Success = true,
                    BulkId = parts.Length > 1 ? parts[1] : "N/A",
                    ErrorCode = "00",
                    ErrorMessage = "Başarılı"
                },
                "20" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "20",
                    ErrorMessage = "Mesaj metninde hata var veya standart maksimum karakter aşıldı"
                },
                "30" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "30",
                    ErrorMessage = "Geçersiz kullanıcı adı/şifre veya API erişim izni yok. IP kısıtlaması olabilir."
                },
                "40" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "40",
                    ErrorMessage = "Mesaj başlığı (sender ID) sistemde tanımlı değil"
                },
                "50" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "50",
                    ErrorMessage = "Yetersiz bakiye"
                },
                "51" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "51",
                    ErrorMessage = "Aynı mesaj aynı numaraya 24 saat içinde tekrar gönderilemez"
                },
                "70" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "70",
                    ErrorMessage = "Hatalı sorgu parametresi. Zorunlu alan eksik veya hatalı."
                },
                "80" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "80",
                    ErrorMessage = "Gönderim zaman aşımı"
                },
                "85" => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "85",
                    ErrorMessage = "Yinelenen gönderim engellendi (24 saat içinde aynı içerik)"
                },
                _ => new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = code,
                    ErrorMessage = $"Bilinmeyen hata kodu: {code}"
                }
            };
        }

        private string MapNetgsmDeliveryStatus(string statusCode)
        {
            return statusCode switch
            {
                "0" => "Pending",
                "1" => "Delivered",
                "2" => "Failed",
                "3" => "Sent",
                "4" => "Waiting",
                "11" => "Network Error",
                "12" => "Invalid Number",
                "13" => "Server Error",
                _ => "Unknown"
            };
        }

        #endregion

        #region Response Models

        private class NetgsmResponseResult
        {
            public bool Success { get; set; }
            public string BulkId { get; set; }
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
        }

        #endregion
    }
}
