using Business.Services.MobileIntegration;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Business.Services.MobileIntegration
{
    /// <summary>
    /// Deep Link Service for mobile app integration
    /// Handles iOS/Android deep links, universal links, and QR code generation
    /// </summary>
    public class DeepLinkService : IDeepLinkService
    {
        private readonly IDeepLinkRepository _deepLinkRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeepLinkService> _logger;
        private readonly string _baseDeepLinkUrl;
        private readonly string _universalLinkDomain;
        private readonly string _webFallbackDomain;

        public DeepLinkService(
            IDeepLinkRepository deepLinkRepository,
            IConfiguration configuration,
            ILogger<DeepLinkService> logger)
        {
            _deepLinkRepository = deepLinkRepository;
            _configuration = configuration;
            _logger = logger;

            // Deep link configuration
            _baseDeepLinkUrl = _configuration["DeepLinks:BaseUrl"] ?? "ziraai://";
            _universalLinkDomain = _configuration["DeepLinks:UniversalLinkDomain"] ?? "https://ziraai.com";
            _webFallbackDomain = _configuration["DeepLinks:WebFallbackDomain"] ?? "https://web.ziraai.com";
        }

        public async Task<IDataResult<DeepLinkInfo>> GenerateDeepLinkAsync(DeepLinkRequest request)
        {
            try
            {
                _logger.LogInformation("Generating deep link for type {Type} with parameter {Parameter}", 
                    request.Type, request.PrimaryParameter);

                // Generate unique link ID
                var linkId = GenerateUniqueLinkId();

                // Build deep link URLs
                var deepLinkUrl = BuildDeepLinkUrl(request, linkId);
                var universalLinkUrl = BuildUniversalLinkUrl(request);
                var webFallbackUrl = BuildWebFallbackUrl(request);
                var shortUrl = await GenerateShortUrlAsync(universalLinkUrl);

                // Generate QR code if requested
                string qrCodeUrl = null;
                if (request.GenerateQrCode)
                {
                    qrCodeUrl = await GenerateQrCodeAsync(universalLinkUrl, new QrCodeOptions());
                }

                // Create deep link record
                var deepLink = new DeepLink
                {
                    LinkId = linkId,
                    Type = request.Type,
                    PrimaryParameter = request.PrimaryParameter,
                    AdditionalParameters = SerializeParameters(request.AdditionalParameters),
                    DeepLinkUrl = deepLinkUrl,
                    UniversalLinkUrl = universalLinkUrl,
                    WebFallbackUrl = webFallbackUrl,
                    ShortUrl = shortUrl,
                    QrCodeUrl = qrCodeUrl,
                    CampaignSource = request.CampaignSource,
                    SponsorId = request.SponsorId,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddMonths(6), // 6 months validity
                    IsActive = true,
                    TotalClicks = 0,
                    MobileAppOpens = 0,
                    WebFallbackOpens = 0,
                    UniqueDevices = 0
                };

                _logger.LogInformation("Attempting to save DeepLink - LinkId: {LinkId}, Type: {Type}, QrCodeLength: {QrLength}, SponsorId: {SponsorId}",
                    linkId, request.Type, qrCodeUrl?.Length ?? 0, request.SponsorId);

                _deepLinkRepository.Add(deepLink);
                await _deepLinkRepository.SaveChangesAsync();

                var response = new DeepLinkInfo
                {
                    LinkId = linkId,
                    DeepLinkUrl = deepLinkUrl,
                    UniversalLinkUrl = universalLinkUrl,
                    WebFallbackUrl = webFallbackUrl,
                    QrCodeUrl = qrCodeUrl,
                    ShortUrl = shortUrl,
                    CreatedDate = deepLink.CreatedDate,
                    ExpiryDate = deepLink.ExpiryDate,
                    IsActive = deepLink.IsActive
                };

                _logger.LogInformation("Deep link generated successfully. LinkId: {LinkId}", linkId);
                return new SuccessDataResult<DeepLinkInfo>(response, "Deep link başarıyla oluşturuldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating deep link for type {Type}", request.Type);
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner Exception: " + ex.InnerException.Message;
                }
                return new ErrorDataResult<DeepLinkInfo>("Deep link oluşturulamadı: " + errorMessage);
            }
        }

        public async Task<IDataResult<DeepLinkAnalytics>> GetDeepLinkAnalyticsAsync(string linkId)
        {
            try
            {
                var deepLink = await _deepLinkRepository.GetAsync(dl => dl.LinkId == linkId);
                if (deepLink == null)
                {
                    return new ErrorDataResult<DeepLinkAnalytics>("Deep link bulunamadı");
                }

                // Get click records
                var clicks = await _deepLinkRepository.GetClicksAsync(linkId);
                
                // Calculate analytics
                var platformBreakdown = clicks.GroupBy(c => c.Platform)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                var sourceBreakdown = clicks.GroupBy(c => c.Source ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                var recentClicks = clicks.OrderByDescending(c => c.ClickDate)
                    .Take(50)
                    .Select(c => new DeepLinkClick
                    {
                        ClickDate = c.ClickDate,
                        Platform = c.Platform,
                        Country = c.Country,
                        Source = c.Source,
                        DidOpenApp = c.DidOpenApp,
                        DidCompleteAction = c.DidCompleteAction
                    }).ToList();

                var analytics = new DeepLinkAnalytics
                {
                    LinkId = linkId,
                    TotalClicks = deepLink.TotalClicks,
                    MobileAppOpens = deepLink.MobileAppOpens,
                    WebFallbackOpens = deepLink.WebFallbackOpens,
                    UniqueDevices = deepLink.UniqueDevices,
                    PlatformBreakdown = platformBreakdown,
                    SourceBreakdown = sourceBreakdown,
                    RecentClicks = recentClicks,
                    ConversionRate = deepLink.TotalClicks > 0 
                        ? (double)clicks.Count(c => c.DidCompleteAction) / deepLink.TotalClicks * 100 
                        : 0
                };

                return new SuccessDataResult<DeepLinkAnalytics>(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deep link analytics for {LinkId}", linkId);
                return new ErrorDataResult<DeepLinkAnalytics>("Analytics alınamadı: " + ex.Message);
            }
        }

        public async Task<IResult> TrackDeepLinkClickAsync(string linkId, DeepLinkClickInfo clickInfo)
        {
            try
            {
                var deepLink = await _deepLinkRepository.GetAsync(dl => dl.LinkId == linkId);
                if (deepLink == null)
                {
                    return new ErrorResult("Deep link bulunamadı");
                }

                // Create click record
                var clickRecord = new DeepLinkClickRecord
                {
                    LinkId = linkId,
                    UserAgent = clickInfo.UserAgent,
                    IpAddress = clickInfo.IpAddress,
                    Platform = DetectPlatform(clickInfo.UserAgent),
                    DeviceId = clickInfo.DeviceId,
                    Referrer = clickInfo.Referrer,
                    ClickDate = DateTime.Now,
                    Country = clickInfo.Country ?? await GetCountryFromIpAsync(clickInfo.IpAddress),
                    City = clickInfo.City ?? await GetCityFromIpAsync(clickInfo.IpAddress),
                    Source = deepLink.CampaignSource,
                    DidOpenApp = false, // This will be updated via webhook if app opens
                    DidCompleteAction = false // This will be updated when action completes
                };

                await _deepLinkRepository.AddClickAsync(clickRecord);

                // Update deep link statistics
                deepLink.TotalClicks++;
                
                // Check if this is a unique device
                var existingClicks = await _deepLinkRepository.GetClicksByDeviceAsync(linkId, clickInfo.DeviceId);
                if (!existingClicks.Any())
                {
                    deepLink.UniqueDevices++;
                }

                // Update platform-specific counters
                if (IsAppLikelyToOpen(clickInfo.UserAgent))
                {
                    deepLink.MobileAppOpens++;
                }
                else
                {
                    deepLink.WebFallbackOpens++;
                }

                _deepLinkRepository.Update(deepLink);
                await _deepLinkRepository.SaveChangesAsync();

                _logger.LogInformation("Deep link click tracked. LinkId: {LinkId}, Platform: {Platform}", 
                    linkId, clickRecord.Platform);

                return new SuccessResult("Tıklama kaydedildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking deep link click for {LinkId}", linkId);
                return new ErrorResult("Tıklama kaydedilemedi: " + ex.Message);
            }
        }

        public async Task<IDataResult<UniversalLinkConfig>> GetUniversalLinkConfigAsync()
        {
            try
            {
                var config = new UniversalLinkConfig
                {
                    Domain = _universalLinkDomain.Replace("https://", ""),
                    AppId = _configuration["DeepLinks:AppId"] ?? "com.ziraai.app",
                    PathMappings = new Dictionary<string, string>
                    {
                        { "/redeem/*", "redemption" },
                        { "/analysis/*", "analysis" },
                        { "/dashboard", "dashboard" },
                        { "/profile/*", "profile" }
                    },
                    AppleAppStoreUrl = _configuration["DeepLinks:AppleAppStoreUrl"] ?? 
                        "https://apps.apple.com/app/ziraai/id123456789",
                    GooglePlayStoreUrl = _configuration["DeepLinks:GooglePlayStoreUrl"] ?? 
                        "https://play.google.com/store/apps/details?id=com.ziraai.app",
                    WebFallbackDomain = _webFallbackDomain,
                    IsConfigured = !string.IsNullOrEmpty(_configuration["DeepLinks:AppId"])
                };

                return new SuccessDataResult<UniversalLinkConfig>(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting universal link config");
                return new ErrorDataResult<UniversalLinkConfig>("Konfigürasyon alınamadı: " + ex.Message);
            }
        }

        #region Private Methods

        private string GenerateUniqueLinkId()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 12);
        }

        private string BuildDeepLinkUrl(DeepLinkRequest request, string linkId)
        {
            var urlBuilder = new StringBuilder(_baseDeepLinkUrl);
            urlBuilder.Append(request.Type);
            
            var parameters = new List<string> { $"linkId={linkId}" };
            
            if (!string.IsNullOrEmpty(request.PrimaryParameter))
            {
                var paramName = request.Type switch
                {
                    "redemption" => "code",
                    "analysis" => "analysisId",
                    _ => "param"
                };
                parameters.Add($"{paramName}={request.PrimaryParameter}");
            }

            if (request.AdditionalParameters != null)
            {
                parameters.AddRange(request.AdditionalParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            }

            if (parameters.Any())
            {
                urlBuilder.Append("?" + string.Join("&", parameters));
            }

            return urlBuilder.ToString();
        }

        private string BuildUniversalLinkUrl(DeepLinkRequest request)
        {
            var path = request.Type switch
            {
                "redemption" => $"/redeem/{request.PrimaryParameter}",
                "analysis" => $"/analysis/{request.PrimaryParameter}",
                "dashboard" => "/dashboard",
                _ => $"/{request.Type}"
            };

            return _universalLinkDomain + path;
        }

        private string BuildWebFallbackUrl(DeepLinkRequest request)
        {
            return !string.IsNullOrEmpty(request.FallbackUrl) 
                ? request.FallbackUrl 
                : BuildUniversalLinkUrl(request).Replace(_universalLinkDomain, _webFallbackDomain);
        }

        private async Task<string> GenerateShortUrlAsync(string longUrl)
        {
            // In production, integrate with bit.ly, tinyurl, or your own URL shortener
            // For now, generate a simple short URL
            
            var hash = longUrl.GetHashCode().ToString("X");
            var shortUrl = $"{_universalLinkDomain}/s/{hash}";
            
            _logger.LogInformation("Generated short URL {ShortUrl} for {LongUrl}", shortUrl, longUrl);
            return await Task.FromResult(shortUrl);
        }

        private async Task<string> GenerateQrCodeAsync(string url, QrCodeOptions options)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(url, (QRCodeGenerator.ECCLevel)options.ErrorCorrectionLevel);
                
                using var qrCode = new PngByteQRCode(qrCodeData);
                var pngBytes = qrCode.GetGraphic(20);
                
                // Convert to base64
                var base64String = Convert.ToBase64String(pngBytes);
                
                return $"data:image/{options.Format.ToLower()};base64,{base64String}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for URL {Url}", url);
                return null;
            }
        }

        private string SerializeParameters(Dictionary<string, string> parameters)
        {
            if (parameters == null || !parameters.Any())
                return null;

            return string.Join(";", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        private string DetectPlatform(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("iphone") || userAgent.Contains("ipad"))
                return "iOS";
            else if (userAgent.Contains("android"))
                return "Android";
            else if (userAgent.Contains("mobile"))
                return "Mobile";
            else
                return "Web";
        }

        private bool IsAppLikelyToOpen(string userAgent)
        {
            // This is a simplified check - in production, you'd have more sophisticated detection
            var platform = DetectPlatform(userAgent);
            return platform == "iOS" || platform == "Android";
        }

        private async Task<string> GetCountryFromIpAsync(string ipAddress)
        {
            // In production, integrate with IP geolocation service
            return await Task.FromResult("Turkey"); // Default for Turkish users
        }

        private async Task<string> GetCityFromIpAsync(string ipAddress)
        {
            // In production, integrate with IP geolocation service
            return await Task.FromResult("Istanbul"); // Default for Turkish users
        }

        #endregion
    }
}