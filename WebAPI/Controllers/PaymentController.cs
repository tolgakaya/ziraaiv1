using Business.BusinessAspects;
using Business.Services.Payment;
using Core.Extensions;
using Core.Utilities.Results;
using Entities.Dtos.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Payment management for iyzico payment gateway integration
    /// Handles payment initialization, verification, and webhook callbacks
    /// </summary>
    [Route("api/v{version:apiVersion}/payments")]
    [ApiController]
    public class PaymentController : BaseApiController
    {
        private readonly IIyzicoPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IIyzicoPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Initialize a new payment transaction
        /// Creates payment transaction and returns iyzico payment page URL
        /// </summary>
        /// <param name="request">Payment initialization request with flow type and data</param>
        /// <returns>Payment initialization response with payment URL and token</returns>
        /// <response code="200">Payment initialized successfully, returns payment URL</response>
        /// <response code="400">Invalid request data or flow type</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="500">Internal server error during payment initialization</response>
        [Authorize]
        [SecuredOperation]
        [HttpPost("initialize")]
        [ProducesResponseType(typeof(IDataResult<PaymentInitializeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InitializePayment([FromBody] PaymentInitializeRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[Payment] Initialize payment failed: User ID not found in claims");
                    return Unauthorized(new ErrorResult("User not authenticated"));
                }

                var userIdValue = userId.Value;

                _logger.LogInformation($"[Payment] Initialize payment request. UserId: {userIdValue}, FlowType: {request.FlowType}");

                var result = await _paymentService.InitializePaymentAsync(userIdValue, request);

                if (result.Success)
                {
                    _logger.LogInformation($"[Payment] Payment initialized successfully. UserId: {userIdValue}, TransactionId: {result.Data.TransactionId}");
                    return Ok(result);
                }

                _logger.LogWarning($"[Payment] Payment initialization failed. UserId: {userIdValue}, Error: {result.Message}");
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment] Error initializing payment");
                return StatusCode(500, new ErrorResult("An error occurred while initializing payment"));
            }
        }

        /// <summary>
        /// Verify payment status after user completes payment on iyzico page
        /// Called by mobile app after user returns from iyzico payment page
        /// </summary>
        /// <param name="request">Payment verification request with payment token</param>
        /// <returns>Payment verification response with final status and flow results</returns>
        /// <response code="200">Payment verified successfully, returns payment status</response>
        /// <response code="400">Invalid token or payment verification failed</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">Payment transaction not found</response>
        /// <response code="500">Internal server error during payment verification</response>
        [Authorize]
        [SecuredOperation]
        [HttpPost("verify")]
        [ProducesResponseType(typeof(IDataResult<PaymentVerifyResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerifyRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[Payment] Verify payment failed: User ID not found in claims");
                    return Unauthorized(new ErrorResult("User not authenticated"));
                }

                var userIdValue = userId.Value;

                _logger.LogInformation($"[Payment] Verify payment request. UserId: {userIdValue}, Token: {request.PaymentToken}");

                var result = await _paymentService.VerifyPaymentAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation($"[Payment] Payment verified successfully. UserId: {userIdValue}, Status: {result.Data.Status}");
                    return Ok(result);
                }

                _logger.LogWarning($"[Payment] Payment verification failed. UserId: {userIdValue}, Error: {result.Message}");

                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment] Error verifying payment");
                return StatusCode(500, new ErrorResult("An error occurred while verifying payment"));
            }
        }


        /// <summary>
        /// iyzico payment callback endpoint (3D Secure redirect target)
        /// This endpoint receives POST from iyzico after 3D Secure completion
        /// and redirects to mobile deep link
        /// </summary>
        /// <param name="token">Payment token from iyzico</param>
        /// <param name="status">Payment status from iyzico</param>
        /// <param name="paymentId">Payment ID from iyzico (optional)</param>
        /// <param name="conversationId">Conversation ID from iyzico (optional)</param>
        /// <returns>HTTP 302 Redirect to mobile deep link</returns>
        /// <response code="302">Redirects to mobile app via deep link</response>
        /// <response code="500">Internal server error during callback processing</response>
        [AllowAnonymous]  // iyzico calls this, not authenticated mobile app
        [HttpPost("callback")]
        [HttpGet("callback")]  // Support both POST and GET from iyzico
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PaymentCallback(
            [FromForm] string token,
            [FromForm] string status,
            [FromForm] string paymentId = null,
            [FromForm] string conversationId = null)
        {
            try
            {
                _logger.LogInformation("[Payment] Callback received from iyzico. Token: {Token}, Status: {Status}, PaymentId: {PaymentId}",
                    token, status, paymentId);

                // Verify payment with iyzico and update transaction
                var verifyRequest = new PaymentVerifyRequestDto { PaymentToken = token };
                var result = await _paymentService.VerifyPaymentAsync(verifyRequest);

                // Prepare deep link URL for mobile app
                var deepLinkUrl = result.Success
                    ? $"ziraai://payment-callback?token={token}&status=success"
                    : $"ziraai://payment-callback?token={token}&status=failed&error={Uri.EscapeDataString(result.Message)}";

                _logger.LogInformation("[Payment] Redirecting to mobile app: {DeepLink}", deepLinkUrl);

                // Redirect browser to deep link (this opens mobile app)
                return Redirect(deepLinkUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment] Callback processing failed for token: {Token}", token);

                // Redirect to mobile with error
                var errorDeepLink = $"ziraai://payment-callback?token={token}&status=failed&error={Uri.EscapeDataString(ex.Message)}";
                return Redirect(errorDeepLink);
            }
        }

        /// <summary>
        /// Webhook endpoint for iyzico payment callbacks
        /// Public endpoint called by iyzico when payment status changes
        /// </summary>
        /// <param name="webhook">Payment webhook data from iyzico</param>
        /// <returns>Success or error result</returns>
        /// <response code="200">Webhook processed successfully</response>
        /// <response code="400">Invalid webhook data or signature</response>
        /// <response code="500">Internal server error during webhook processing</response>
        [AllowAnonymous]
        [HttpPost("/api/payments/webhook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PaymentWebhook([FromBody] PaymentWebhookDto webhook)
        {
            try
            {
                _logger.LogInformation($"[Payment] Webhook received. Token: {webhook.Token}, Status: {webhook.Status}");

                var result = await _paymentService.ProcessWebhookAsync(webhook);

                if (result.Success)
                {
                    _logger.LogInformation($"[Payment] Webhook processed successfully. Token: {webhook.Token}");
                    return Ok(result);
                }

                _logger.LogWarning($"[Payment] Webhook processing failed. Token: {webhook.Token}, Error: {result.Message}");
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment] Error processing webhook");
                return StatusCode(500, new ErrorResult("An error occurred while processing webhook"));
            }
        }

        /// <summary>
        /// Get payment status by token
        /// Returns current status of a payment transaction
        /// </summary>
        /// <param name="token">Payment token</param>
        /// <returns>Payment status information</returns>
        /// <response code="200">Payment status retrieved successfully</response>
        /// <response code="400">Invalid token</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">Payment transaction not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize]
        [SecuredOperation]
        [HttpGet("status/{token}")]
        [ProducesResponseType(typeof(IDataResult<PaymentVerifyResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaymentStatus(string token)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[Payment] Get payment status failed: User ID not found in claims");
                    return Unauthorized(new ErrorResult("User not authenticated"));
                }

                var userIdValue = userId.Value;

                _logger.LogInformation($"[Payment] Get payment status request. UserId: {userIdValue}, Token: {token}");

                var result = await _paymentService.GetPaymentStatusAsync(token);

                if (result.Success)
                {
                    _logger.LogInformation($"[Payment] Payment status retrieved. UserId: {userIdValue}, Status: {result.Data.Status}");
                    return Ok(result);
                }

                _logger.LogWarning($"[Payment] Get payment status failed. UserId: {userIdValue}, Error: {result.Message}");

                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payment] Error getting payment status");
                return StatusCode(500, new ErrorResult("An error occurred while getting payment status"));
            }
        }

        /// <summary>
        /// Get current user ID from JWT claims
        /// </summary>
        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }
    }
}
