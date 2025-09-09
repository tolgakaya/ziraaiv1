using System;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Core.Utilities.Messages;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Extensions
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;


        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(httpContext, e);
            }
        }


        private async Task HandleExceptionAsync(HttpContext httpContext, Exception e)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            _ = e.Message;
            string message;
            
            // Log the actual exception details
            _logger.LogError(e, "Exception occurred: {Message} at {Path} {Method}. StackTrace: {StackTrace}", 
                e.Message, 
                httpContext.Request.Path, 
                httpContext.Request.Method, 
                e.StackTrace);
            if (e.GetType() == typeof(ValidationException))
            {
                message = e.Message;
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning("Validation exception: {Message}", e.Message);
            }
            else if (e.GetType() == typeof(ApplicationException))
            {
                message = e.Message;
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning("Application exception: {Message}", e.Message);
            }
            else if (e.GetType() == typeof(UnauthorizedAccessException))
            {
                message = e.Message;
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                _logger.LogWarning("Unauthorized access exception: {Message}", e.Message);
            }
            else if (e.GetType() == typeof(SecurityException))
            {
                message = e.Message;
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                _logger.LogWarning("Security exception: {Message}", e.Message);
            }
            else if (e.GetType() == typeof(NotSupportedException))
            {
                message = e.Message;
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning("Not supported exception: {Message}", e.Message);
            }
            else
            {
                message = ExceptionMessage.InternalServerError;
                _logger.LogError("Unhandled exception: {ExceptionType} - {Message}", e.GetType().Name, e.Message);
            }
            await httpContext.Response.WriteAsync(message);
        }
    }
}