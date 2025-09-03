using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Simple health check controller to verify API is running
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)] // Exclude from Swagger
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class HealthCheckController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public HealthCheckController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Basic health check
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.Now,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                assembly = Assembly.GetExecutingAssembly().GetName().Name,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            });
        }

        /// <summary>
        /// List all registered controllers
        /// </summary>
        [HttpGet("controllers")]
        public IActionResult ListControllers()
        {
            var controllers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => new
                {
                    Name = type.Name,
                    Namespace = type.Namespace,
                    Methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == type)
                        .Select(m => m.Name)
                        .ToList()
                })
                .OrderBy(c => c.Name)
                .ToList();

            return Ok(new
            {
                totalControllers = controllers.Count,
                controllers = controllers
            });
        }

        /// <summary>
        /// Check if Swagger provider is registered
        /// </summary>
        [HttpGet("swagger-check")]
        public IActionResult CheckSwagger()
        {
            try
            {
                var swaggerProvider = _serviceProvider.GetService(typeof(Swashbuckle.AspNetCore.Swagger.ISwaggerProvider));
                
                if (swaggerProvider == null)
                {
                    return Ok(new
                    {
                        swaggerRegistered = false,
                        message = "ISwaggerProvider not found in DI container"
                    });
                }

                // Try to get the swagger document
                var getSwaggerMethod = swaggerProvider.GetType().GetMethod("GetSwagger");
                if (getSwaggerMethod != null)
                {
                    try
                    {
                        var swaggerDoc = getSwaggerMethod.Invoke(swaggerProvider, new object[] { "v1", null, "/" });
                        
                        return Ok(new
                        {
                            swaggerRegistered = true,
                            documentGenerated = swaggerDoc != null,
                            message = swaggerDoc != null ? "Swagger document generated successfully" : "Swagger document is null"
                        });
                    }
                    catch (Exception ex)
                    {
                        return Ok(new
                        {
                            swaggerRegistered = true,
                            documentGenerated = false,
                            error = ex.InnerException?.Message ?? ex.Message,
                            exceptionType = ex.InnerException?.GetType().Name ?? ex.GetType().Name
                        });
                    }
                }

                return Ok(new
                {
                    swaggerRegistered = true,
                    message = "ISwaggerProvider found but GetSwagger method not accessible"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}