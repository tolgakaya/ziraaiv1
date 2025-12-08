using System;
using System.Threading.Tasks;
using Business.Services.Redemption;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Request model for code redemption
    /// </summary>
    public class RedeemCodeRequest
    {
        public string Code { get; set; }
    }

    [ApiController]
    [AllowAnonymous]
    public class RedemptionController : ControllerBase
    {
        private readonly IRedemptionService _redemptionService;
        private readonly ILogger<RedemptionController> _logger;
        private readonly IConfiguration _configuration;

        public RedemptionController(
            IRedemptionService redemptionService,
            ILogger<RedemptionController> logger,
            IConfiguration configuration)
        {
            _redemptionService = redemptionService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Handle deep link from SMS - Android Universal Links intercepts this!
        /// This endpoint should NOT redirect - Android opens the app directly.
        /// Mobile app extracts code from the HTTPS URL path.
        /// </summary>
        /// <param name="code">The sponsorship code from the deep link</param>
        /// <returns>Simple HTML page with instructions and fallback</returns>
        [HttpGet("~/redeem/{code}")] // Direct access without /api/v1/ prefix for easier link sharing
        public async Task<IActionResult> HandleDeepLink(string code)
        {
            try
            {
                _logger.LogInformation("📱 Deep link accessed for code: {Code}", code);

                // Get client IP for tracking
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                // Log user agent for debugging
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                _logger.LogInformation("User-Agent: {UserAgent}", userAgent);

                // Track link click (analytics only, does NOT redeem code)
                await _redemptionService.TrackLinkClickAsync(code, clientIp);

                // Basic validation: Check if code format is valid (PREFIX-YYYY-XXXXXXXX)
                // Supports both default AGRI- prefix and custom sponsor prefixes
                if (string.IsNullOrWhiteSpace(code) || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z]+-\d{4}-[A-Z0-9]+$"))
                {
                    _logger.LogWarning("❌ Invalid code format: {Code}", code);
                    return BadRequest("Geçersiz kod formatı");
                }

                // Check if this is an Android device (app should handle it)
                var isAndroid = userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase);
                var isMobile = userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase);

                if (isAndroid && isMobile)
                {
                    _logger.LogInformation("🤖 Android device detected - app should handle this");
                    
                    // For Android with app installed: This response shouldn't be seen!
                    // Android intercepts the URL and opens app before reaching here.
                    // If we reach here, it means:
                    // 1. App is not installed, OR
                    // 2. assetlinks.json verification failed, OR
                    // 3. User clicked "Open in browser" option

                    // Get Play Store package name from configuration
                    var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                    var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";
                    var customSchemeLink = $"ziraai://redemption-success/{code}";

                    // Return HTML with fallback options
                    var html = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>ZiraAI Sponsorluk Kodu</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 1rem;
        }}
        .container {{
            background: white;
            padding: 2rem;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 400px;
            width: 100%;
        }}
        .icon {{ font-size: 64px; margin-bottom: 1rem; }}
        h1 {{ color: #1f2937; margin-bottom: 0.5rem; font-size: 1.5rem; }}
        .code {{
            background: #f3f4f6;
            color: #667eea;
            padding: 1rem;
            border-radius: 8px;
            font-size: 1.2rem;
            font-weight: bold;
            margin: 1.5rem 0;
            letter-spacing: 2px;
        }}
        .btn {{
            display: block;
            width: 100%;
            padding: 1rem;
            margin: 0.75rem 0;
            border: none;
            border-radius: 8px;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            text-decoration: none;
            color: white;
            transition: transform 0.2s;
        }}
        .btn:active {{ transform: scale(0.98); }}
        .btn-primary {{ background: #667eea; }}
        .btn-secondary {{ background: #10b981; }}
        .message {{
            color: #6b7280;
            margin-top: 1.5rem;
            font-size: 0.9rem;
            line-height: 1.5;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>🎁</div>
        <h1>Sponsorluk Kodunuz</h1>
        <div class='code'>{code}</div>
        
        <a href='{customSchemeLink}' class='btn btn-primary'>
            📱 ZiraAI Uygulamasını Aç
        </a>
        
        <a href='{playStoreLink}' class='btn btn-secondary'>
            📥 Uygulamayı İndir
        </a>
        
        <p class='message'>
            ZiraAI uygulaması kurulu değilse önce indirin. 
            Kurulu ise üstteki butona tıklayarak kodu kullanabilirsiniz.
        </p>
    </div>
    
    <script>
        // Try to open app immediately (fallback for Android)
        setTimeout(() => {{
            window.location.href = '{customSchemeLink}';
        }}, 500);
    </script>
</body>
</html>";

                    return Content(html, "text/html");
                }

                // For non-Android devices (iOS, Desktop, etc.)
                _logger.LogInformation("🌐 Non-Android device - showing web instructions");
                
                var webHtml = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>ZiraAI Sponsorluk Kodu</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 1rem;
        }}
        .container {{
            background: white;
            padding: 2rem;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 400px;
            width: 100%;
        }}
        .icon {{ font-size: 64px; margin-bottom: 1rem; }}
        h1 {{ color: #1f2937; margin-bottom: 1rem; }}
        .code {{
            background: #f3f4f6;
            color: #667eea;
            padding: 1rem;
            border-radius: 8px;
            font-size: 1.2rem;
            font-weight: bold;
            margin: 1.5rem 0;
            letter-spacing: 2px;
        }}
        .steps {{
            text-align: left;
            margin: 1.5rem 0;
            color: #4b5563;
        }}
        .step {{
            margin: 0.75rem 0;
            padding-left: 1.5rem;
        }}
        .qr-hint {{
            color: #6b7280;
            font-size: 0.9rem;
            margin-top: 1rem;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>📱</div>
        <h1>Android Cihazınızdan Erişin</h1>
        <p>Bu sponsorluk kodu Android ZiraAI uygulamasında kullanılabilir.</p>
        
        <div class='code'>{code}</div>
        
        <div class='steps'>
            <div class='step'>1️⃣ Bu kodu not alın veya ekran görüntüsü alın</div>
            <div class='step'>2️⃣ Android cihazınızda ZiraAI uygulamasını açın</div>
            <div class='step'>3️⃣ Sponsorluk kodu bölümüne gidin</div>
            <div class='step'>4️⃣ Kodu girerek aktifleştirin</div>
        </div>
        
        <p class='qr-hint'>
            💡 İpucu: Bu sayfanın linkini Android cihazınıza gönderin veya SMS'inizi kontrol edin.
        </p>
    </div>
</body>
</html>";

                return Content(webHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error handling deep link for code {Code}", code);
                return BadRequest($"Link işlenirken hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Redeem a sponsorship code - ACTUAL redemption (called by mobile app after deep link)
        /// This endpoint does the real work: validates, creates account, activates subscription
        /// </summary>
        /// <param name="request">Redemption request with code and optional user info</param>
        /// <returns>JWT token and subscription details for auto-login</returns>
        [HttpPost("~/api/v1/redemption/redeem-code")]
        [AllowAnonymous]
        public async Task<IActionResult> RedeemCode([FromBody] RedeemCodeRequest request)
        {
            try
            {
                _logger.LogInformation("🎁 Code redemption started for: {Code}", request.Code);

                // Get client IP for tracking
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                // Validate code (including sponsor self-redemption check)
                var validationResult = await _redemptionService.ValidateCodeWithUserAsync(request.Code, HttpContext);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("❌ Code validation failed for {Code}: {Message}",
                        request.Code, validationResult.Message);
                    return BadRequest(new { success = false, message = validationResult.Message });
                }

                // Check if user exists (via phone number in code)
                var existingUser = await _redemptionService.FindUserByCodeAsync(request.Code);

                // Create account if needed
                if (existingUser == null)
                {
                    _logger.LogInformation("📝 Creating new account for code: {Code}", request.Code);
                    var accountResult = await _redemptionService.CreateAccountFromCodeAsync(request.Code);
                    if (!accountResult.Success)
                    {
                        _logger.LogError("❌ Account creation failed for code {Code}: {Message}",
                            request.Code, accountResult.Message);
                        return BadRequest(new { success = false, message = accountResult.Message });
                    }
                    existingUser = accountResult.Data;
                    _logger.LogInformation("✅ Account created successfully for user ID: {UserId}",
                        existingUser.UserId);
                }

                // Activate subscription
                var subscriptionResult = await _redemptionService.ActivateSubscriptionAsync(
                    request.Code, existingUser.UserId);
                if (!subscriptionResult.Success)
                {
                    _logger.LogError("❌ Subscription activation failed for code {Code}: {Message}",
                        request.Code, subscriptionResult.Message);
                    return BadRequest(new { success = false, message = subscriptionResult.Message });
                }

                _logger.LogInformation("✅ Subscription activated successfully for user {UserId} with code {Code}",
                    existingUser.UserId, request.Code);

                // Generate auto-login token
                var loginToken = await _redemptionService.GenerateAutoLoginTokenAsync(existingUser.UserId);

                return Ok(new
                {
                    success = true,
                    message = "Sponsorluk kodu başarıyla kullanıldı",
                    data = new
                    {
                        token = loginToken,
                        userId = existingUser.UserId,
                        subscription = new
                        {
                            tier = subscriptionResult.Data.SubscriptionTier.DisplayName,
                            tierId = subscriptionResult.Data.SubscriptionTier.Id,
                            validUntil = subscriptionResult.Data.EndDate
                        },
                        user = new
                        {
                            fullName = existingUser.FullName,
                            email = existingUser.Email,
                            mobilePhones = existingUser.MobilePhones
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error redeeming sponsorship code: {Code}", request.Code);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Beklenmeyen bir hata oluştu: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Display success page after successful redemption
        /// </summary>
        [HttpGet]
        [Route("~/redeem/success")]
        [Route("~/api/v1/redemption/success")]
        [ApiVersion("1.0")]
        [ApiVersionNeutral]
        public IActionResult Success([FromQuery] string token, [FromQuery] string subscription)
        {
            // Return a simple HTML page with auto-login script
            var html = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Aktivasyon Başarılı - ZiraAI</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}
        .container {{
            background: white;
            padding: 3rem;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 450px;
        }}
        .success-icon {{
            width: 80px;
            height: 80px;
            background: #10b981;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 1.5rem;
        }}
        .checkmark {{
            color: white;
            font-size: 48px;
        }}
        h1 {{
            color: #1f2937;
            margin-bottom: 0.5rem;
        }}
        .subscription-badge {{
            background: #f3f4f6;
            color: #6b7280;
            padding: 0.5rem 1rem;
            border-radius: 20px;
            display: inline-block;
            margin: 1rem 0;
            font-weight: 600;
        }}
        .message {{
            color: #6b7280;
            margin: 1.5rem 0;
            line-height: 1.6;
        }}
        .loading {{
            margin-top: 2rem;
            color: #9ca3af;
        }}
        .spinner {{
            border: 3px solid #f3f4f6;
            border-top: 3px solid #667eea;
            border-radius: 50%;
            width: 30px;
            height: 30px;
            animation: spin 1s linear infinite;
            margin: 1rem auto;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='success-icon'>
            <span class='checkmark'>✓</span>
        </div>
        <h1>Aktivasyon Başarılı!</h1>
        <div class='subscription-badge'>{subscription} Abonelik</div>
        <p class='message'>
            Aboneliğiniz başarıyla aktive edildi.<br>
            Birkaç saniye içinde ZiraAI uygulamasına yönlendirileceksiniz...
        </p>
        <div class='loading'>
            <div class='spinner'></div>
            <small>Yönlendiriliyor...</small>
        </div>
    </div>
    <script>
        // Store token in localStorage for auto-login
        if ('{token}') {{
            localStorage.setItem('ziraai_auth_token', '{token}');
            
            // Redirect to dashboard after 2 seconds
            setTimeout(() => {{
                window.location.href = '/dashboard';
            }}, 2000);
        }}
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        /// <summary>
        /// Display error page when redemption fails
        /// </summary>
        [HttpGet]
        [Route("~/redeem/error")]
        [Route("~/api/v{version:apiVersion}/redeem/error")]
        [ApiVersion("1.0")]
        [ApiVersionNeutral]
        public IActionResult Error([FromQuery] string message)
        {
            // Return a simple HTML error page
            var html = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Aktivasyon Hatası - ZiraAI</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}
        .container {{
            background: white;
            padding: 3rem;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 450px;
        }}
        .error-icon {{
            width: 80px;
            height: 80px;
            background: #ef4444;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 1.5rem;
        }}
        .cross {{
            color: white;
            font-size: 48px;
        }}
        h1 {{
            color: #1f2937;
            margin-bottom: 1rem;
        }}
        .error-message {{
            background: #fef2f2;
            color: #dc2626;
            padding: 1rem;
            border-radius: 8px;
            margin: 1.5rem 0;
            border: 1px solid #fecaca;
        }}
        .help-text {{
            color: #6b7280;
            margin-top: 2rem;
            line-height: 1.6;
        }}
        .contact-link {{
            color: #667eea;
            text-decoration: none;
            font-weight: 600;
        }}
        .contact-link:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>
            <span class='cross'>✕</span>
        </div>
        <h1>Aktivasyon Başarısız</h1>
        <div class='error-message'>
            {message ?? "Aktivasyon işlemi sırasında bir hata oluştu."}
        </div>
        <p class='help-text'>
            Sorun devam ederse lütfen <a href='mailto:destek@ziraai.com' class='contact-link'>destek@ziraai.com</a> 
            adresinden bizimle iletişime geçin veya sponsorunuz ile görüşün.
        </p>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}