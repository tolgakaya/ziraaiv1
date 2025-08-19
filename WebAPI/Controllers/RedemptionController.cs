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
        /// Redeem a sponsorship code via public link (no authentication required)
        /// </summary>
        /// <param name="code">The sponsorship code to redeem</param>
        /// <returns>HTML page with redemption result and auto-login if successful</returns>
        [HttpGet]
        [Route("~/redeem/{code}")] // Direct access without /api/v1/ prefix for easier link sharing
        [ApiVersionNeutral] // This endpoint should work regardless of API version
        public async Task<IActionResult> RedeemSponsorshipCode(string code)
        {
            try
            {
                _logger.LogInformation("Redemption link clicked for code: {Code}", code);
                
                // Get client IP for tracking
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                
                // Log user agent and authorization header for debugging
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                _logger.LogInformation("User-Agent: {UserAgent}, Auth Header Present: {HasAuth}", 
                    userAgent, !string.IsNullOrEmpty(authHeader));
                
                // Track link click
                await _redemptionService.TrackLinkClickAsync(code, clientIp);
                
                // Validate code (including sponsor self-redemption check)
                var validationResult = await _redemptionService.ValidateCodeWithUserAsync(code, HttpContext);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("Code validation failed for {Code}: {Message}", 
                        code, validationResult.Message);
                    return RedirectToAction("Error", new { message = validationResult.Message });
                }

                // Check if user exists (via phone number in code)
                var existingUser = await _redemptionService.FindUserByCodeAsync(code);
                
                // Create account if needed
                if (existingUser == null)
                {
                    _logger.LogInformation("Creating new account for code: {Code}", code);
                    var accountResult = await _redemptionService.CreateAccountFromCodeAsync(code);
                    if (!accountResult.Success)
                    {
                        _logger.LogError("Account creation failed for code {Code}: {Message}", 
                            code, accountResult.Message);
                        return RedirectToAction("Error", new { message = accountResult.Message });
                    }
                    existingUser = accountResult.Data;
                    _logger.LogInformation("Account created successfully for user ID: {UserId}", 
                        existingUser.UserId);
                }

                // Activate subscription
                var subscriptionResult = await _redemptionService.ActivateSubscriptionAsync(
                    code, existingUser.UserId);
                if (!subscriptionResult.Success)
                {
                    _logger.LogError("Subscription activation failed for code {Code}: {Message}", 
                        code, subscriptionResult.Message);
                    return RedirectToAction("Error", new { message = subscriptionResult.Message });
                }

                _logger.LogInformation("Subscription activated successfully for user {UserId} with code {Code}", 
                    existingUser.UserId, code);

                // Generate auto-login token
                var loginToken = await _redemptionService.GenerateAutoLoginTokenAsync(existingUser.UserId);
                
                // Get frontend URL from configuration
                var frontendUrl = _configuration["RedemptionSettings:FrontendUrl"] ?? "https://localhost:5001";
                
                // Redirect to success page with token
                return Redirect($"{frontendUrl}/redemption-success?token={loginToken}&subscription={subscriptionResult.Data.SubscriptionTier.DisplayName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming sponsorship code: {Code}", code);
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner: " + ex.InnerException.Message;
                    _logger.LogError("Inner Exception: {InnerException}", ex.InnerException.Message);
                }
                return RedirectToAction("Error", new { message = $"Beklenmeyen bir hata oluştu: {errorMessage}" });
            }
        }

        /// <summary>
        /// Display success page after successful redemption
        /// </summary>
        [HttpGet]
        [Route("~/redeem/success")]
        [Route("~/api/v{version:apiVersion}/redeem/success")]
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