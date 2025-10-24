using Business.Handlers.AdminPlantAnalysis.Commands;
using Business.Handlers.AdminPlantAnalysis.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for plant analysis management
    /// Provides endpoints for creating analyses on behalf of users
    /// </summary>
    [Route("api/admin/plant-analysis")]
    public class AdminPlantAnalysisController : AdminBaseController
    {
        /// <summary>
        /// Create plant analysis on behalf of a user
        /// </summary>
        /// <param name="request">Analysis creation request</param>
        [HttpPost("on-behalf-of")]
        public async Task<IActionResult> CreateAnalysisOnBehalfOf([FromBody] CreateAnalysisOnBehalfOfRequest request)
        {
            var command = new CreatePlantAnalysisOnBehalfOfCommand
            {
                TargetUserId = request.TargetUserId,
                ImageUrl = request.ImageUrl,
                Notes = request.Notes,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponse(result);
        }

        /// <summary>
        /// Get all analyses for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <param name="isOnBehalfOf">Filter by OBO status (optional)</param>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAnalyses(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string status = null,
            [FromQuery] bool? isOnBehalfOf = null)
        {
            var query = new GetUserAnalysesQuery
            {
                UserId = userId,
                Page = page,
                PageSize = pageSize,
                Status = status,
                IsOnBehalfOf = isOnBehalfOf
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get all analyses created on behalf of users
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <summary>
        /// Get all analyses created on behalf of users
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="adminUserId">Filter by admin user ID (optional)</param>
        /// <param name="targetUserId">Filter by target user ID (optional)</param>
        /// <param name="status">Filter by status (optional)</param>
        [HttpGet("on-behalf-of")]
        public async Task<IActionResult> GetAllOnBehalfOfAnalyses(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? adminUserId = null,
            [FromQuery] int? targetUserId = null,
            [FromQuery] string status = null)
        {
            var query = new GetAllOBOAnalysesQuery
            {
                Page = page,
                PageSize = pageSize,
                AdminUserId = adminUserId,
                TargetUserId = targetUserId,
                Status = status
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }
    }

    /// <summary>
    /// Request model for creating analysis on behalf of user
    /// </summary>
    public class CreateAnalysisOnBehalfOfRequest
    {
        /// <summary>
        /// Target user ID
        /// </summary>
        public int TargetUserId { get; set; }

        /// <summary>
        /// Plant image URL
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }
    }
}
