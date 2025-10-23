using Business.Handlers.AdminSponsorship.Commands;
using Business.Handlers.AdminSponsorship.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for sponsorship management operations
    /// Provides endpoints for managing purchases and codes
    /// </summary>
    [Route("api/admin/sponsorship")]
    public class AdminSponsorshipController : AdminBaseController
    {
        #region Purchase Management

        /// <summary>
        /// Get all sponsorship purchases with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <param name="paymentStatus">Filter by payment status (optional)</param>
        /// <param name="sponsorId">Filter by sponsor ID (optional)</param>
        [HttpGet("purchases")]
        public async Task<IActionResult> GetAllPurchases(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string status = null,
            [FromQuery] string paymentStatus = null,
            [FromQuery] int? sponsorId = null)
        {
            var query = new GetAllPurchasesQuery
            {
                Page = page,
                PageSize = pageSize,
                Status = status,
                PaymentStatus = paymentStatus,
                SponsorId = sponsorId
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get sponsorship purchase by ID
        /// </summary>
        /// <param name="purchaseId">Purchase ID</param>
        [HttpGet("purchases/{purchaseId}")]
        public async Task<IActionResult> GetPurchaseById(int purchaseId)
        {
            var query = new GetPurchaseByIdQuery { PurchaseId = purchaseId };
            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Approve a pending sponsorship purchase
        /// </summary>
        /// <param name="purchaseId">Purchase ID to approve</param>
        /// <param name="request">Approval request details</param>
        [HttpPost("purchases/{purchaseId}/approve")]
        public async Task<IActionResult> ApprovePurchase(int purchaseId, [FromBody] ApprovePurchaseRequest request)
        {
            var command = new ApprovePurchaseCommand
            {
                PurchaseId = purchaseId,
                Notes = request?.Notes,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Refund a sponsorship purchase
        /// </summary>
        /// <param name="purchaseId">Purchase ID to refund</param>
        /// <param name="request">Refund request details</param>
        [HttpPost("purchases/{purchaseId}/refund")]
        public async Task<IActionResult> RefundPurchase(int purchaseId, [FromBody] RefundPurchaseRequest request)
        {
            var command = new RefundPurchaseCommand
            {
                PurchaseId = purchaseId,
                RefundReason = request?.RefundReason,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        #endregion

        #region Code Management

        /// <summary>
        /// Get all sponsorship codes with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="isUsed">Filter by usage status (optional)</param>
        /// <param name="isActive">Filter by active status (optional)</param>
        /// <param name="sponsorId">Filter by sponsor ID (optional)</param>
        /// <param name="purchaseId">Filter by purchase ID (optional)</param>
        [HttpGet("codes")]
        public async Task<IActionResult> GetAllCodes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool? isUsed = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int? sponsorId = null,
            [FromQuery] int? purchaseId = null)
        {
            var query = new GetAllCodesQuery
            {
                Page = page,
                PageSize = pageSize,
                IsUsed = isUsed,
                IsActive = isActive,
                SponsorId = sponsorId,
                PurchaseId = purchaseId
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get sponsorship code by ID
        /// </summary>
        /// <param name="codeId">Code ID</param>
        [HttpGet("codes/{codeId}")]
        public async Task<IActionResult> GetCodeById(int codeId)
        {
            var query = new GetCodeByIdQuery { CodeId = codeId };
            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Deactivate a sponsorship code
        /// </summary>
        /// <param name="codeId">Code ID to deactivate</param>
        /// <param name="request">Deactivation request details</param>
        [HttpPost("codes/{codeId}/deactivate")]
        public async Task<IActionResult> DeactivateCode(int codeId, [FromBody] DeactivateCodeRequest request)
        {
            var command = new DeactivateCodeCommand
            {
                CodeId = codeId,
                Reason = request?.Reason,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// Request model for approving a purchase
    /// </summary>
    public class ApprovePurchaseRequest
    {
        /// <summary>
        /// Optional notes about the approval
        /// </summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Request model for refunding a purchase
    /// </summary>
    public class RefundPurchaseRequest
    {
        /// <summary>
        /// Reason for the refund
        /// </summary>
        public string RefundReason { get; set; }
    }

    /// <summary>
    /// Request model for deactivating a code
    /// </summary>
    public class DeactivateCodeRequest
    {
        /// <summary>
        /// Reason for deactivation
        /// </summary>
        public string Reason { get; set; }
    }

    #endregion
}
