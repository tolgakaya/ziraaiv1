using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok(new 
            { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }
        
        [HttpGet("detailed")]
        [AllowAnonymous]
        public IActionResult GetDetailed()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                uptime = Environment.TickCount64,
                platform = Environment.OSVersion.Platform.ToString(),
                railway = new
                {
                    serviceName = Environment.GetEnvironmentVariable("RAILWAY_SERVICE_NAME"),
                    deploymentId = Environment.GetEnvironmentVariable("RAILWAY_DEPLOYMENT_ID"),
                    environmentId = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT_ID")
                }
            });
        }
    }
}