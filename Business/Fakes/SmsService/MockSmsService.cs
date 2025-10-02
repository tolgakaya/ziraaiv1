using System;
using System.Threading.Tasks;
using Business.Adapters.SmsService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Fakes.SmsService
{
    /// <summary>
    /// Mock SMS Service for Development/Staging environments.
    /// Does not send real SMS, only logs to console.
    /// </summary>
    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _useFixedCode;
        private readonly string _fixedCode;

        public MockSmsService(ILogger<MockSmsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Read mock configuration
            _useFixedCode = bool.Parse(_configuration["SmsService:MockSettings:UseFixedCode"] ?? "true");
            _fixedCode = _configuration["SmsService:MockSettings:FixedCode"] ?? "123456";
        }

        /// <summary>
        /// Mock SMS send with password parameter (not used in mock)
        /// </summary>
        public async Task<bool> Send(string password, string text, string cellPhone)
        {
            return await SendAssist(text, cellPhone);
        }

        /// <summary>
        /// Mock SMS send assist - logs to console instead of sending real SMS
        /// </summary>
        public async Task<bool> SendAssist(string text, string cellPhone)
        {
            // Simulate network delay
            await Task.Delay(100);

            // Extract OTP code from text (if exists)
            var otpCode = ExtractOtpCode(text);

            if (_useFixedCode && !string.IsNullOrEmpty(otpCode))
            {
                // Log with fixed code for easy testing
                Console.WriteLine($"📱 MOCK SMS to {cellPhone}");
                Console.WriteLine($"   Fixed OTP Code: {_fixedCode}");
                Console.WriteLine($"   (Original code would be: {otpCode})");

                _logger.LogInformation(
                    "📱 MOCK SMS sent to {Phone}. Fixed OTP: {FixedCode}",
                    cellPhone, _fixedCode);
            }
            else
            {
                // Log actual message
                Console.WriteLine($"📱 MOCK SMS to {cellPhone}");
                Console.WriteLine($"   Message: {text}");

                _logger.LogInformation(
                    "📱 MOCK SMS sent to {Phone}. Message: {Message}",
                    cellPhone, text);
            }

            // Always return success for mock
            return true;
        }

        /// <summary>
        /// Extract OTP code from SMS text
        /// Looks for patterns like "PAROLANIZ : 123456" or "CODE: 123456"
        /// </summary>
        private string ExtractOtpCode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // Pattern 1: "PAROLANIZ : 123456"
            var match1 = System.Text.RegularExpressions.Regex.Match(
                text,
                @"PAROLANIZ\s*:\s*(\d{4,6})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match1.Success)
                return match1.Groups[1].Value;

            // Pattern 2: "CODE: 123456" or "CODE : 123456"
            var match2 = System.Text.RegularExpressions.Regex.Match(
                text,
                @"CODE\s*:?\s*(\d{4,6})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match2.Success)
                return match2.Groups[1].Value;

            // Pattern 3: Any 4-6 digit number in the text
            var match3 = System.Text.RegularExpressions.Regex.Match(
                text,
                @"\b(\d{4,6})\b");

            if (match3.Success)
                return match3.Groups[1].Value;

            return null;
        }
    }
}
