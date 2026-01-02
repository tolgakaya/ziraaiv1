using Microsoft.Extensions.Configuration;
using System;

namespace Business.Services.Messaging
{
    /// <summary>
    /// Helper class for Google SMS Retriever API integration
    /// Provides environment-specific app signature hashes for OTP auto-fill
    ///
    /// Last Updated: 2026-01-02
    /// Documentation: https://developers.google.com/identity/sms-retriever/overview
    /// Package: https://pub.dev/packages/sms_autofill
    /// </summary>
    public class SmsRetrieverHelper
    {
        private readonly IConfiguration _configuration;

        public SmsRetrieverHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Get app signature hash for current environment
        /// </summary>
        public string GetAppSignatureHash()
        {
            var environment = GetCurrentEnvironment();
            return GetAppSignatureHashForEnvironment(environment);
        }

        /// <summary>
        /// Get app signature hash for specific environment
        /// </summary>
        public string GetAppSignatureHashForEnvironment(string environment)
        {
            // Hash codes from mobile app configuration
            // These must match the package name signatures from mobile app
            var hashes = new
            {
                Production = "4EIBGGTwGxF",  // com.ziraai.app
                Staging = "2YocBG2c6D1",     // com.ziraai.app.staging
                Development = "jEcisGBcK6d"  // com.ziraai.app.dev
            };

            return environment?.ToLower() switch
            {
                "production" => hashes.Production,
                "staging" => hashes.Staging,
                "development" => hashes.Development,
                _ => hashes.Production // Default to production for safety
            };
        }

        /// <summary>
        /// Detect current environment from configuration
        /// </summary>
        public string GetCurrentEnvironment()
        {
            // Check ASPNETCORE_ENVIRONMENT first (standard ASP.NET Core env)
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(aspnetEnv))
            {
                return aspnetEnv;
            }

            // Check configuration fallback
            var configEnv = _configuration["Environment"];
            if (!string.IsNullOrEmpty(configEnv))
            {
                return configEnv;
            }

            // Default to Production for safety
            return "Production";
        }

        /// <summary>
        /// Build OTP SMS message with app signature hash
        /// </summary>
        /// <param name="otpCode">The OTP code (4-6 digits)</param>
        /// <param name="environment">Environment name (optional, auto-detected if null)</param>
        /// <returns>SMS message with hash code (under 140 characters)</returns>
        public string BuildOtpSmsMessage(string otpCode, string environment = null)
        {
            var hash = string.IsNullOrEmpty(environment)
                ? GetAppSignatureHash()
                : GetAppSignatureHashForEnvironment(environment);

            // SMS Retriever API requirements:
            // 1. Message must be under 140 characters
            // 2. Must contain <#> followed by 11-character hash
            // 3. Hash must be on same line or next line after OTP
            // 4. OTP must be 4-6 digits

            // Turkish version (shorter for character limit)
            var message = $"ZiraAI dogrulama kodunuz: {otpCode}\n<#> {hash}";

            // Verify message length requirement
            if (message.Length > 140)
            {
                throw new InvalidOperationException(
                    $"OTP SMS message exceeds 140 character limit: {message.Length} characters");
            }

            return message;
        }

        /// <summary>
        /// Build OTP SMS message in English
        /// </summary>
        public string BuildOtpSmsMessageEnglish(string otpCode, string environment = null)
        {
            var hash = string.IsNullOrEmpty(environment)
                ? GetAppSignatureHash()
                : GetAppSignatureHashForEnvironment(environment);

            var message = $"Your ZiraAI code is {otpCode}\n<#> {hash}";

            if (message.Length > 140)
            {
                throw new InvalidOperationException(
                    $"OTP SMS message exceeds 140 character limit: {message.Length} characters");
            }

            return message;
        }

        /// <summary>
        /// Validate OTP code format (4-6 digits as required by SMS Retriever API)
        /// </summary>
        public bool IsValidOtpCode(string otpCode)
        {
            if (string.IsNullOrEmpty(otpCode))
                return false;

            // Must be 4-6 digits
            return otpCode.Length >= 4 && otpCode.Length <= 6 && int.TryParse(otpCode, out _);
        }
    }
}
