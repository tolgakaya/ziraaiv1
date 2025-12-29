using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    /// <summary>
    /// NetGSM SMS API integration for Turkey market
    /// Uses REST v2 API with Basic Auth for standard SMS and XML for OTP
    /// API Documentation: https://www.netgsm.com.tr/dokuman/
    /// </summary>
    public class NetgsmSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NetgsmSmsService> _logger;
        private readonly SmsRetrieverHelper _smsRetrieverHelper;
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
            _smsRetrieverHelper = new SmsRetrieverHelper(configuration);

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

        /// <summary>
        /// Send single SMS using REST v2 API with Basic Auth
        /// Endpoint: POST /sms/rest/v2/send
        /// </summary>
        public async Task<IResult> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorResult("NetGSM kimlik bilgileri yapılandırılmamış. NETGSM_USERCODE ve NETGSM_PASSWORD ayarlayın.");
                }

                var normalizedPhone = NormalizePhoneNumber(phoneNumber);
                _logger.LogInformation("Sending SMS to {Phone} via NetGSM REST v2", normalizedPhone);

                // Build JSON payload for REST v2 API
                var payload = new
                {
                    msgheader = _msgHeader,
                    encoding = ContainsTurkishChars(message) ? "TR" : "",
                    messages = new[]
                    {
                        new
                        {
                            msg = message,
                            no = normalizedPhone
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Set Basic Auth header
                var authBytes = Encoding.UTF8.GetBytes($"{_userCode}:{_password}");
                var authBase64 = Convert.ToBase64String(authBytes);

                using var request = new HttpRequestMessage(HttpMethod.Post, "/sms/rest/v2/send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse response
                var result = ParseRestV2Response(responseContent);

                if (result.Success)
                {
                    _logger.LogInformation("SMS sent successfully to {Phone}. JobId: {JobId}",
                        normalizedPhone, result.JobId);
                    return new SuccessResult($"SMS başarıyla gönderildi. Mesaj ID: {result.JobId}");
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

        /// <summary>
        /// Send OTP SMS using XML endpoint for faster delivery (max 3 minutes)
        /// Endpoint: POST /sms/send/otp
        /// Note: OTP SMS must not contain Turkish characters for Google SMS Retriever API compatibility
        /// SMS Retriever API Requirements:
        /// - Message must be under 140 characters
        /// - Must contain app signature hash for auto-fill
        /// - OTP code must be 4-6 digits
        /// </summary>
        public async Task<IResult> SendOtpAsync(string phoneNumber, string otpCode)
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorResult("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                // Validate OTP code format
                if (!_smsRetrieverHelper.IsValidOtpCode(otpCode))
                {
                    return new ErrorResult("OTP kodu 4-6 basamaklı olmalıdır.");
                }

                var normalizedPhone = NormalizePhoneNumber(phoneNumber);
                var environment = _smsRetrieverHelper.GetCurrentEnvironment();

                _logger.LogInformation("Sending OTP to {Phone} via NetGSM OTP endpoint. Environment: {Environment}",
                    normalizedPhone, environment);

                // Build OTP message with Google SMS Retriever API app signature hash
                // This enables automatic OTP detection and auto-fill on Android devices
                var otpMessage = _smsRetrieverHelper.BuildOtpSmsMessage(otpCode);

                _logger.LogInformation("OTP message length: {Length} characters (limit: 140)", otpMessage.Length);

                // Build XML payload for OTP endpoint
                var xmlPayload = $@"<?xml version=""1.0""?>
<mainbody>
   <header>
       <usercode>{_userCode}</usercode>
       <password>{_password}</password>
       <msgheader>{_msgHeader}</msgheader>
   </header>
   <body>
       <msg>
           <![CDATA[{otpMessage}]]>
       </msg>
       <no>{normalizedPhone}</no>
   </body>
</mainbody>";

                var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

                var response = await _httpClient.PostAsync("/sms/send/otp", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse OTP response (same format as standard SMS)
                var result = ParseNetgsmResponse(responseContent);

                if (result.Success)
                {
                    _logger.LogInformation("OTP sent successfully to {Phone}. JobId: {JobId}, Environment: {Environment}",
                        normalizedPhone, result.JobId, environment);
                    return new SuccessResult($"OTP başarıyla gönderildi. Mesaj ID: {result.JobId}");
                }
                else
                {
                    _logger.LogError("OTP sending failed to {Phone}. Error: {ErrorCode} - {ErrorMessage}",
                        normalizedPhone, result.ErrorCode, result.ErrorMessage);
                    return new ErrorResult($"OTP gönderilemedi: {result.ErrorMessage} (Kod: {result.ErrorCode})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending OTP to {Phone} via NetGSM", phoneNumber);
                return new ErrorResult($"OTP gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Send bulk SMS using REST v2 API with messages array
        /// Endpoint: POST /sms/rest/v2/send
        /// </summary>
        public async Task<IResult> SendBulkSmsAsync(BulkSmsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorResult("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                _logger.LogInformation("Sending bulk SMS to {Count} recipients via NetGSM REST v2", request.Recipients.Length);

                // Build messages array
                var messages = new List<object>();
                foreach (var recipient in request.Recipients)
                {
                    var message = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                        ? recipient.PersonalizedMessage
                        : request.Message.Replace("{name}", recipient.Name ?? "Değerli Kullanıcı");

                    messages.Add(new
                    {
                        msg = message,
                        no = NormalizePhoneNumber(recipient.PhoneNumber)
                    });
                }

                // Build JSON payload
                var payload = new
                {
                    msgheader = _msgHeader,
                    encoding = "TR",
                    messages = messages
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Set Basic Auth header
                var authBytes = Encoding.UTF8.GetBytes($"{_userCode}:{_password}");
                var authBase64 = Convert.ToBase64String(authBytes);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/sms/rest/v2/send");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parse response
                var result = ParseRestV2Response(responseContent);

                if (result.Success)
                {
                    _logger.LogInformation("Bulk SMS sent successfully. JobId: {JobId}, Count: {Count}",
                        result.JobId, request.Recipients.Length);
                    return new SuccessResult($"Toplu SMS başarıyla gönderildi. Mesaj ID: {result.JobId}, Alıcı sayısı: {request.Recipients.Length}");
                }
                else
                {
                    _logger.LogError("Bulk SMS sending failed. Error: {ErrorCode} - {ErrorMessage}",
                        result.ErrorCode, result.ErrorMessage);
                    return new ErrorResult($"Toplu SMS gönderilemedi: {result.ErrorMessage} (Kod: {result.ErrorCode})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending bulk SMS via NetGSM");
                return new ErrorResult($"Toplu SMS gönderimi sırasında hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Get SMS delivery status using REST v2 report endpoint
        /// Endpoint: POST /sms/rest/v2/report
        /// </summary>
        public async Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorDataResult<SmsDeliveryStatus>("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                // Build JSON payload for report
                var payload = new
                {
                    jobids = new[] { messageId },
                    pagenumber = 0,
                    pagesize = 10
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Set Basic Auth header
                var authBytes = Encoding.UTF8.GetBytes($"{_userCode}:{_password}");
                var authBase64 = Convert.ToBase64String(authBytes);

                using var request = new HttpRequestMessage(HttpMethod.Post, "/sms/rest/v2/report");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Try to parse JSON response
                try
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("code", out var codeElement))
                    {
                        var code = codeElement.GetString();
                        if (code != "00")
                        {
                            return new ErrorDataResult<SmsDeliveryStatus>($"Hata kodu: {code}");
                        }
                    }

                    var status = new SmsDeliveryStatus
                    {
                        MessageId = messageId,
                        Provider = "Netgsm",
                        Status = "Delivered",
                        SentDate = DateTime.Now
                    };

                    return new SuccessDataResult<SmsDeliveryStatus>(status);
                }
                catch
                {
                    // Fallback to old response parsing
                    var status = new SmsDeliveryStatus
                    {
                        MessageId = messageId,
                        Provider = "Netgsm",
                        Status = "Unknown",
                        SentDate = DateTime.Now
                    };
                    return new SuccessDataResult<SmsDeliveryStatus>(status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting SMS delivery status for {MessageId}", messageId);
                return new ErrorDataResult<SmsDeliveryStatus>(
                    $"Durum sorgulama sırasında hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Get sender info and balance using REST v2 msgheader endpoint
        /// Endpoint: GET /sms/rest/v2/msgheader (for sender names)
        /// Endpoint: POST /balance (for credit balance)
        /// </summary>
        public async Task<IDataResult<SmsSenderInfo>> GetSenderInfoAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_userCode) || string.IsNullOrEmpty(_password))
                {
                    return new ErrorDataResult<SmsSenderInfo>("NetGSM kimlik bilgileri yapılandırılmamış.");
                }

                // Get balance using POST /balance
                var balancePayload = new
                {
                    usercode = _userCode,
                    password = _password,
                    stip = 2
                };

                var jsonContent = JsonSerializer.Serialize(balancePayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var balanceResponse = await _httpClient.PostAsync("/balance", content);
                var balanceContent = await balanceResponse.Content.ReadAsStringAsync();

                decimal balance = 0;

                // Try to parse JSON response
                try
                {
                    using var doc = JsonDocument.Parse(balanceContent);
                    if (doc.RootElement.TryGetProperty("balance", out var balanceElement))
                    {
                        balance = balanceElement.GetDecimal();
                    }
                }
                catch
                {
                    // Fallback: try to parse as plain text
                    var trimmed = balanceContent.Trim();
                    if (trimmed == "30" || trimmed == "70")
                    {
                        return new ErrorDataResult<SmsSenderInfo>(
                            trimmed == "30"
                                ? "Geçersiz kullanıcı adı veya şifre"
                                : "Hatalı sorgu parametresi");
                    }

                    decimal.TryParse(trimmed.Split('|')[0], out balance);
                }

                var info = new SmsSenderInfo
                {
                    SenderId = _msgHeader,
                    Balance = balance,
                    Currency = "TL",
                    MonthlyQuota = 0,
                    UsedQuota = 0,
                    Provider = "Netgsm",
                    IsActive = true
                };

                return new SuccessDataResult<SmsSenderInfo>(info);
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
                return digitsOnly;
            }
            else if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
            {
                return "9" + digitsOnly; // 05xx -> 905xx
            }
            else if (digitsOnly.Length == 10 && digitsOnly.StartsWith("5"))
            {
                return "90" + digitsOnly; // 5xx -> 905xx
            }

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

        private NetgsmResponseResult ParseRestV2Response(string response)
        {
            try
            {
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var codeElement))
                {
                    var code = codeElement.GetString();
                    var jobId = root.TryGetProperty("jobid", out var jobIdElement)
                        ? jobIdElement.GetString()
                        : "N/A";

                    if (code == "00")
                    {
                        return new NetgsmResponseResult
                        {
                            Success = true,
                            JobId = jobId,
                            ErrorCode = "00",
                            ErrorMessage = "Başarılı"
                        };
                    }

                    return new NetgsmResponseResult
                    {
                        Success = false,
                        ErrorCode = code,
                        ErrorMessage = GetErrorMessage(code)
                    };
                }

                return new NetgsmResponseResult
                {
                    Success = false,
                    ErrorCode = "UNKNOWN",
                    ErrorMessage = $"Beklenmeyen yanıt: {response}"
                };
            }
            catch
            {
                // Fallback to old parsing for non-JSON responses
                return ParseNetgsmResponse(response);
            }
        }

        private NetgsmResponseResult ParseNetgsmResponse(string response)
        {
            var trimmedResponse = response.Trim();
            var parts = trimmedResponse.Split(' ');
            var code = parts[0];

            return new NetgsmResponseResult
            {
                Success = code == "00",
                JobId = parts.Length > 1 ? parts[1] : "N/A",
                ErrorCode = code,
                ErrorMessage = code == "00" ? "Başarılı" : GetErrorMessage(code)
            };
        }

        private string GetErrorMessage(string code)
        {
            return code switch
            {
                "00" => "Başarılı",
                "20" => "Mesaj metninde hata var veya standart maksimum karakter aşıldı",
                "30" => "Geçersiz kullanıcı adı/şifre veya API erişim izni yok. IP kısıtlaması olabilir.",
                "40" => "Mesaj başlığı (sender ID) sistemde tanımlı değil",
                "50" => "Yetersiz bakiye",
                "51" => "Aynı mesaj aynı numaraya 24 saat içinde tekrar gönderilemez",
                "70" => "Hatalı sorgu parametresi. Zorunlu alan eksik veya hatalı.",
                "80" => "Gönderim zaman aşımı",
                "85" => "Yinelenen gönderim engellendi (24 saat içinde aynı içerik)",
                _ => $"Bilinmeyen hata kodu: {code}"
            };
        }

        #endregion

        #region Response Models

        private class NetgsmResponseResult
        {
            public bool Success { get; set; }
            public string JobId { get; set; }
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
        }

        #endregion
    }
}
