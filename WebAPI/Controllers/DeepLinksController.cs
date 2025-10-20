using Business.Services.MobileIntegration;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class DeepLinksController : BaseApiController
    {
        private readonly IDeepLinkService _deepLinkService;

        public DeepLinksController(IDeepLinkService deepLinkService)
        {
            _deepLinkService = deepLinkService;
        }

        /// <summary>
        /// Generate a new deep link for mobile app integration
        /// </summary>
        [HttpPost("generate")]
        [Authorize(Roles = "Sponsor,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateDeepLink([FromBody] DeepLinkRequest request)
        {
            var result = await _deepLinkService.GenerateDeepLinkAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get analytics for a specific deep link
        /// </summary>
        [HttpGet("analytics/{linkId}")]
        [Authorize(Roles = "Sponsor,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeepLinkAnalytics(string linkId)
        {
            var result = await _deepLinkService.GetDeepLinkAnalyticsAsync(linkId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Track a deep link click (called by mobile app or web page)
        /// </summary>
        [HttpPost("track-click/{linkId}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrackClick(string linkId)
        {
            try
            {
                // Extract click information from request
                var clickInfo = new DeepLinkClickInfo
                {
                    LinkId = linkId,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = GetClientIpAddress(),
                    Referrer = Request.Headers["Referer"].ToString(),
                    DeviceId = Request.Headers["X-Device-Id"].ToString(), // Custom header from mobile apps
                    ClickDate = DateTime.Now
                };

                var result = await _deepLinkService.TrackDeepLinkClickAsync(linkId, clickInfo);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResult($"Tıklama kaydedilemedi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get universal link configuration for iOS/Android setup
        /// </summary>
        [HttpGet("universal-link-config")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUniversalLinkConfig()
        {
            var result = await _deepLinkService.GetUniversalLinkConfigAsync();
            return Ok(result);
        }

        // REMOVED: .well-known routes are now served via StaticFileMiddleware in Startup.cs
        // This prevents route conflicts and allows proper static file serving
        // Files: WebAPI/.well-known/assetlinks.json, WebAPI/.well-known/apple-app-site-association

        /// <summary>
        /// Smart redirect endpoint - detects platform and redirects appropriately
        /// </summary>
        [HttpGet("/r/{linkId}")]
        [AllowAnonymous]
        public async Task<IActionResult> SmartRedirect(string linkId)
        {
            try
            {
                // Track the click first
                await TrackClick(linkId);

                var userAgent = Request.Headers["User-Agent"].ToString().ToLower();
                var isIOS = userAgent.Contains("iphone") || userAgent.Contains("ipad");
                var isAndroid = userAgent.Contains("android");
                var isMobile = isIOS || isAndroid;

                var config = await _deepLinkService.GetUniversalLinkConfigAsync();
                
                if (isMobile && config.Success && config.Data.IsConfigured)
                {
                    // For mobile devices, return HTML page with app opening logic
                    var html = GenerateSmartRedirectHtml(linkId, isIOS, config.Data);
                    return Content(html, "text/html");
                }
                else
                {
                    // For desktop, redirect to web fallback
                    return Redirect($"{config.Data?.WebFallbackDomain}/redeem/{linkId}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Yönlendirme hatası: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private string GetClientIpAddress()
        {
            // Get real client IP, considering proxies and load balancers
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GenerateSmartRedirectHtml(string linkId, bool isIOS, UniversalLinkConfig config)
        {
            var appScheme = isIOS ? $"ziraai://redeem?linkId={linkId}" : $"ziraai://redeem?linkId={linkId}";
            var storeUrl = isIOS ? config.AppleAppStoreUrl : config.GooglePlayStoreUrl;
            var webFallback = $"{config.WebFallbackDomain}/redeem/{linkId}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>ZiraAI'ye Yönlendiriliyor...</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; text-align: center; padding: 50px; background: #f5f5f5; }}
        .container {{ background: white; padding: 30px; border-radius: 10px; box-shadow: 0 4px 20px rgba(0,0,0,0.1); max-width: 400px; margin: 0 auto; }}
        .logo {{ font-size: 24px; color: #4CAF50; margin-bottom: 20px; }}
        .message {{ color: #666; margin-bottom: 30px; }}
        .spinner {{ border: 3px solid #f3f3f3; border-top: 3px solid #4CAF50; border-radius: 50%; width: 30px; height: 30px; animation: spin 1s linear infinite; margin: 20px auto; }}
        @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
        .fallback {{ margin-top: 20px; }}
        .fallback a {{ color: #4CAF50; text-decoration: none; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""logo"">🌱 ZiraAI</div>
        <div class=""message"">Uygulamaya yönlendiriliyor...</div>
        <div class=""spinner""></div>
        <div class=""fallback"">
            <p>Uygulama açılmadı mı?</p>
            <a href=""{storeUrl}"" id=""storeLink"">App Store'dan İndir</a>
            <br><br>
            <a href=""{webFallback}"" id=""webLink"">Web'de Aç</a>
        </div>
    </div>

    <script>
        // Attempt to open the app
        var appOpened = false;
        
        function openApp() {{
            var startTime = Date.now();
            window.location = '{appScheme}';
            
            // Check if app opened by detecting if browser was paused
            setTimeout(function() {{
                if (Date.now() - startTime < 2000) {{
                    // App likely didn't open, show fallback options
                    document.querySelector('.message').textContent = 'Uygulama bulunamadı. Aşağıdaki seçenekleri deneyin:';
                    document.querySelector('.spinner').style.display = 'none';
                    document.querySelector('.fallback').style.display = 'block';
                }} else {{
                    appOpened = true;
                }}
            }}, 1500);
        }}
        
        // Try to open app immediately
        openApp();
        
        // Auto-redirect to store after 5 seconds if app didn't open
        setTimeout(function() {{
            if (!appOpened) {{
                document.querySelector('.message').textContent = 'App Store\'a yönlendiriliyor...';
                window.location = '{storeUrl}';
            }}
        }}, 5000);
    </script>
</body>
</html>";
        }

        #endregion
    }
}