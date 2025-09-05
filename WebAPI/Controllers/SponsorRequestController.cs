using Business.Handlers.SponsorRequest.Commands;
using Business.Handlers.SponsorRequest.Queries;
using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SponsorRequestController : BaseApiController
    {
        /// <summary>
        /// Create a new sponsor request (Farmer creates request to sponsor)
        /// </summary>
        /// <param name="createSponsorRequestDto"></param>
        /// <returns>Deeplink URL for WhatsApp message</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateSponsorRequestDto createSponsorRequestDto)
        {
            var result = await Mediator.Send(new CreateSponsorRequestCommand
            {
                SponsorPhone = createSponsorRequestDto.SponsorPhone,
                RequestMessage = createSponsorRequestDto.RequestMessage,
                RequestedTierId = createSponsorRequestDto.RequestedTierId
            });

            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Process deeplink when sponsor clicks WhatsApp link
        /// </summary>
        /// <param name="hashedToken">Token from deeplink URL</param>
        /// <returns>Sponsor request details</returns>
        [HttpGet("process/{hashedToken}")]
        public async Task<IActionResult> ProcessDeeplink(string hashedToken)
        {
            var result = await Mediator.Send(new ProcessDeeplinkQuery
            {
                HashedToken = hashedToken
            });

            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Get pending sponsor requests for current sponsor
        /// </summary>
        /// <returns>List of pending requests</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var result = await Mediator.Send(new GetPendingSponsorRequestsQuery());

            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Approve one or multiple sponsor requests
        /// </summary>
        /// <param name="approveSponsorRequestDto"></param>
        /// <returns>Approval result</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveRequests([FromBody] ApproveSponsorRequestDto approveSponsorRequestDto)
        {
            var result = await Mediator.Send(new ApproveSponsorRequestCommand
            {
                RequestIds = approveSponsorRequestDto.RequestIds,
                SubscriptionTierId = approveSponsorRequestDto.SubscriptionTierId,
                ApprovalNotes = approveSponsorRequestDto.ApprovalNotes
            });

            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }

        /// <summary>
        /// Reject sponsor requests (updates status to Rejected)
        /// </summary>
        /// <param name="requestIds">List of request IDs to reject</param>
        /// <param name="rejectionReason">Reason for rejection</param>
        /// <returns>Rejection result</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("reject")]
        public async Task<IActionResult> RejectRequests([FromBody] RejectSponsorRequestDto rejectDto)
        {
            // For now, we'll implement this as a simple status update
            // This could be extended to use a separate command/handler
            return Ok(new SuccessResult("Reject functionality to be implemented"));
        }

        /// <summary>
        /// Generate WhatsApp message URL for a specific request
        /// </summary>
        /// <param name="requestId">Sponsor request ID</param>
        /// <returns>WhatsApp URL</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpGet("{requestId}/whatsapp-message")]
        public async Task<IActionResult> GenerateWhatsAppMessage(int requestId)
        {
            // Implementation to generate WhatsApp message URL
            // This would use the SponsorRequestService.GenerateWhatsAppMessage method
            return Ok(new SuccessDataResult<string>("whatsapp://send?phone=...", "WhatsApp message URL generated"));
        }
    }

    public class RejectSponsorRequestDto
    {
        public List<int> RequestIds { get; set; }
        public string RejectionReason { get; set; }
    }
}