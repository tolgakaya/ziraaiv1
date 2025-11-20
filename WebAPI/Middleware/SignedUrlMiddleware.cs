using Business.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAPI.Middleware
{
    /// <summary>
    /// Middleware to validate signed URLs for secure file access
    /// Prevents unauthorized access to uploaded files (voice messages, attachments, etc.)
    /// Must be registered BEFORE UseStaticFiles in Startup.cs
    /// </summary>
    public class SignedUrlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SignedUrlMiddleware> _logger;

        public SignedUrlMiddleware(RequestDelegate next, ILogger<SignedUrlMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISignedUrlService signedUrlService)
        {
            // Only validate /uploads paths (voice messages, attachments, plant images)
            if (context.Request.Path.StartsWithSegments("/uploads"))
            {
                var signature = context.Request.Query["sig"].ToString();
                var expiresStr = context.Request.Query["exp"].ToString();
                var path = context.Request.Path.ToString();

                // Check if signature parameters exist
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(expiresStr))
                {
                    _logger.LogWarning(
                        "Unsigned URL access attempt blocked. Path: {Path}, IP: {IP}",
                        path,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        message = "Access denied: Signature required",
                        data = (object)null
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Parse and validate expiration timestamp
                if (!long.TryParse(expiresStr, out var expires))
                {
                    _logger.LogWarning(
                        "Invalid expiration timestamp. Path: {Path}, Expires: {Expires}",
                        path,
                        expiresStr);

                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        message = "Access denied: Invalid expiration timestamp",
                        data = (object)null
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Validate signature
                if (!signedUrlService.ValidateSignature(path, signature, expires))
                {
                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var isExpired = currentTime > expires;

                    _logger.LogWarning(
                        "Invalid/expired signature. Path: {Path}, Expired: {IsExpired}, IP: {IP}",
                        path,
                        isExpired,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        message = isExpired
                            ? "Access denied: URL expired. Please refresh and try again"
                            : "Access denied: Invalid signature",
                        data = (object)null
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Signature valid - log and continue
                _logger.LogInformation(
                    "Valid signed URL access. Path: {Path}, IP: {IP}",
                    path,
                    context.Connection.RemoteIpAddress);
            }

            // Continue to next middleware (UseStaticFiles)
            await _next(context);
        }
    }
}
