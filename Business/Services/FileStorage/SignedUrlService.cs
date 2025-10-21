using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Business.Services.FileStorage
{
    /// <summary>
    /// Service for generating and validating signed URLs with expiration
    /// Provides security layer for file access without database lookups
    /// </summary>
    public interface ISignedUrlService
    {
        /// <summary>
        /// Generate a signed URL with HMAC signature and expiration
        /// </summary>
        /// <param name="url">Base URL to sign</param>
        /// <param name="expiresInMinutes">Expiration time in minutes (default: 15)</param>
        /// <returns>Signed URL with signature and expiration parameters</returns>
        string SignUrl(string url, int expiresInMinutes = 15);

        /// <summary>
        /// Validate signature and expiration of a signed URL
        /// </summary>
        /// <param name="path">URL path to validate</param>
        /// <param name="signature">HMAC signature from query string</param>
        /// <param name="expires">Expiration timestamp from query string</param>
        /// <returns>True if signature is valid and not expired</returns>
        bool ValidateSignature(string path, string signature, long expires);
    }

    public class SignedUrlService : ISignedUrlService
    {
        private readonly string _secretKey;

        public SignedUrlService(IConfiguration configuration)
        {
            _secretKey = configuration["FileStorage:SignatureSecret"];

            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException(
                    "FileStorage:SignatureSecret not configured. " +
                    "Please add a strong random secret (min 32 characters) to appsettings.json");
            }

            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "FileStorage:SignatureSecret must be at least 32 characters long for security");
            }
        }

        public string SignUrl(string url, int expiresInMinutes = 15)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            if (expiresInMinutes <= 0)
                throw new ArgumentException("Expiration must be greater than 0", nameof(expiresInMinutes));

            // Remove any existing query parameters
            var baseUrl = url.Split('?')[0];

            // Calculate expiration timestamp (Unix timestamp)
            var expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes).ToUnixTimeSeconds();

            // Generate HMAC-SHA256 signature
            var signature = ComputeHMAC($"{baseUrl}:{expires}");

            // Return signed URL with signature and expiration
            return $"{baseUrl}?sig={signature}&exp={expires}";
        }

        public bool ValidateSignature(string path, string signature, long expires)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(signature))
                return false;

            // Check expiration (reject if expired)
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
                return false;

            // Compute expected signature
            var expectedSignature = ComputeHMAC($"{path}:{expires}");

            // Constant-time comparison to prevent timing attacks
            return ConstantTimeEquals(signature, expectedSignature);
        }

        /// <summary>
        /// Compute HMAC-SHA256 signature for the given data
        /// Uses URL-safe Base64 encoding
        /// </summary>
        private string ComputeHMAC(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

            // URL-safe Base64 encoding (replace +/= with -_)
            return Convert.ToBase64String(hash)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks
        /// </summary>
        private bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
