using Business.Handlers.AdminSponsorship.Commands;
using Business.Handlers.AdminSponsorship.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for sponsorship management operations
    /// Provides endpoints for managing purchases and codes
    /// </summary>
using Business.Handlers.AdminSponsorship.Queries;
using System.Collections.Generic;

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

        /// <summary>
        /// Create sponsorship purchase on behalf of a sponsor (Admin OBO)
        /// Supports manual/offline payments - use AutoApprove=true to bypass payment verification
        /// </summary>
        /// <param name="request">Purchase creation request</param>
        [HttpPost("purchases/create-on-behalf-of")]
        public async Task<IActionResult> CreatePurchaseOnBehalfOf([FromBody] CreatePurchaseOnBehalfOfRequest request)
        {
            var command = new CreatePurchaseOnBehalfOfCommand
            {
                SponsorId = request.SponsorId,
                SubscriptionTierId = request.SubscriptionTierId,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                AutoApprove = request.AutoApprove,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                CompanyName = request.CompanyName,
                TaxNumber = request.TaxNumber,
                InvoiceAddress = request.InvoiceAddress,
                CodePrefix = request.CodePrefix,
                ValidityDays = request.ValidityDays,
                Notes = request.Notes,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponse(result);
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

        /// <summary>
        /// Bulk send sponsorship codes to farmers on behalf of sponsor
        /// Sends codes to a list of phone numbers via SMS/WhatsApp/Email
        /// </summary>
        /// <param name="request">Bulk send request with recipient list</param>
        [HttpPost("codes/bulk-send")]
        public async Task<IActionResult> BulkSendCodes([FromBody] BulkSendCodesRequest request)
        {
            var command = new BulkSendCodesCommand
            {
                SponsorId = request.SponsorId,
                PurchaseId = request.PurchaseId,
                Recipients = request.Recipients?.Select(r => new Business.Handlers.AdminSponsorship.Commands.RecipientInfo
                {
                    PhoneNumber = r.PhoneNumber,
                    Name = r.Name
                }).ToList(),
                SendVia = request.SendVia,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Get detailed report for a specific sponsor
        /// Includes purchase statistics, code distribution, and detailed purchase history
        /// </summary>
        /// <param name="sponsorId">Sponsor ID</param>
        [HttpGet("sponsor/{sponsorId}/report")]
        public async Task<IActionResult> GetSponsorDetailedReport(int sponsorId)
        {
            var query = new GetSponsorDetailedReportQuery { SponsorId = sponsorId };
            var result = await Mediator.Send(query);
            return GetResponse(result);
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


    /// <summary>
    /// Request model for creating purchase on behalf of sponsor
    /// </summary>
    public class CreatePurchaseOnBehalfOfRequest
    {
        /// <summary>
        /// Sponsor user ID
        /// </summary>
        public int SponsorId { get; set; }

        /// <summary>
        /// Subscription tier ID
        /// </summary>
        public int SubscriptionTierId { get; set; }

        /// <summary>
        /// Number of codes to generate
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Price per unit
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Auto-approve without payment (for manual/offline payments)
        /// </summary>
        public bool AutoApprove { get; set; } = false;

        /// <summary>
        /// Payment method (Manual, Offline, BankTransfer, etc.)
        /// </summary>
        public string PaymentMethod { get; set; }

        /// <summary>
        /// Payment reference (optional for manual payments)
        /// </summary>
        public string PaymentReference { get; set; }

        /// <summary>
        /// Invoice company name
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Invoice tax number
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// Code prefix for generated codes (optional)
        /// </summary>
        public string CodePrefix { get; set; }

        /// <summary>
        /// Validity days for generated codes (default: 365)
        /// </summary>
        public int ValidityDays { get; set; } = 365;

        /// <summary>
        /// Invoice address
        /// </summary>
        public string InvoiceAddress { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Request model for bulk sending codes
    /// </summary>
    public class BulkSendCodesRequest
    {
        /// <summary>
        /// Sponsor user ID
        /// </summary>
        public int SponsorId { get; set; }

        /// <summary>
        /// Purchase ID to get codes from
        /// </summary>
        public int PurchaseId { get; set; }

        /// <summary>
        /// List of recipients with phone numbers
        /// </summary>
        public List<RecipientInfo> Recipients { get; set; }

        /// <summary>
        /// Send method: SMS, WhatsApp, Email (default: SMS)
        /// </summary>
        public string SendVia { get; set; } = "SMS";
    }

    /// <summary>
    /// Recipient information for bulk code sending
    /// </summary>
    public class RecipientInfo
    {
        /// <summary>
        /// Phone number (required)
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Recipient name (optional)
        /// </summary>
        public string Name { get; set; }
    }

    #endregion
}
