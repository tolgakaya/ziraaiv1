using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Business.Handlers.AdminBulkSubscription.Commands;
using Business.Handlers.AdminBulkSubscription.Queries;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin bulk subscription assignment operations
    /// Allows admins to assign subscriptions to multiple farmers via Excel upload
    /// </summary>
    [Route("api/v1/admin/subscriptions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminBulkSubscriptionController : BaseApiController
    {
        /// <summary>
        /// Queue a bulk subscription assignment job from Excel file
        /// </summary>
        /// <remarks>
        /// Excel file should contain farmer information with the following columns:
        /// - Email (optional if Phone provided)
        /// - Phone (optional if Email provided)
        /// - FirstName (optional)
        /// - LastName (optional)
        /// - TierName (optional if DefaultTierId provided) - One of: Trial, S, M, L, XL
        /// - DurationDays (optional if DefaultDurationDays provided) - Number of days
        /// - Notes (optional)
        ///
        /// Default values apply when Excel doesn't have TierName or DurationDays columns.
        /// At least one of Email or Phone must be provided for each farmer.
        /// Maximum file size: 5 MB
        /// Maximum rows: 2000
        /// </remarks>
        /// <param name="formDto">Form data with Excel file and optional default values</param>
        /// <returns>Job information with status check URL</returns>
        [HttpPost("bulk-assignment")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BulkSubscriptionAssignmentJobDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> QueueBulkSubscriptionAssignment([FromForm] BulkSubscriptionAssignmentFormDto formDto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (adminId == 0)
            {
                return Unauthorized("Invalid admin ID");
            }

            var command = new QueueBulkSubscriptionAssignmentCommand
            {
                ExcelFile = formDto.ExcelFile,
                DefaultTierId = formDto.DefaultTierId,
                DefaultDurationDays = formDto.DefaultDurationDays,
                SendNotification = formDto.SendNotification,
                NotificationMethod = formDto.NotificationMethod,
                AutoActivate = formDto.AutoActivate,
                AdminId = adminId
            };

            var result = await Mediator.Send(command);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get bulk subscription assignment job status and progress
        /// </summary>
        /// <param name="jobId">Job ID to check status</param>
        /// <returns>Job progress details including success/failure counts</returns>
        [HttpGet("bulk-assignment/status/{jobId}")]
        [ProducesResponseType(typeof(BulkSubscriptionAssignmentProgressDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetJobStatus(int jobId)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var query = new GetBulkSubscriptionAssignmentStatusQuery
            {
                JobId = jobId,
                AdminId = adminId
            };

            var result = await Mediator.Send(query);

            if (result.Success)
            {
                return Ok(result);
            }

            return NotFound(result);
        }

        /// <summary>
        /// Get bulk subscription assignment job history for the current admin
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <returns>Paginated list of jobs ordered by creation date descending</returns>
        [HttpGet("bulk-assignment/history")]
        [ProducesResponseType(typeof(List<BulkSubscriptionAssignmentJobHistoryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetJobHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            var query = new GetBulkSubscriptionAssignmentHistoryQuery
            {
                AdminId = adminId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Get bulk subscription assignment job result file URL
        /// </summary>
        /// <remarks>
        /// Result file contains detailed information about each processed farmer:
        /// - Success/failure status
        /// - Error messages for failed assignments
        /// - Created subscription details
        ///
        /// File is available only after job completion.
        /// </remarks>
        /// <param name="jobId">Job ID to get result file</param>
        /// <returns>Result file download URL</returns>
        [HttpGet("bulk-assignment/result/{jobId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetJobResult(int jobId)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var query = new GetBulkSubscriptionAssignmentResultQuery
            {
                JobId = jobId,
                AdminId = adminId
            };

            var result = await Mediator.Send(query);

            if (result.Success)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
    }
}
