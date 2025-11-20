using Business.Handlers.AdminSms.Commands;
using Business.Handlers.AdminSms.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin SMS management endpoints for testing and monitoring SMS providers
    /// </summary>
    public class AdminSmsController : AdminBaseController
    {
        /// <summary>
        /// Get current SMS provider information and status
        /// </summary>
        /// <remarks>
        /// Returns information about the configured SMS provider including:
        /// - Provider name (Mock, Netgsm, Turkcell)
        /// - Configuration status
        /// - Account balance (if available)
        /// - Quota usage
        ///
        /// Sample response:
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": null,
        ///   "data": {
        ///     "provider": "Netgsm",
        ///     "isConfigured": true,
        ///     "senderId": "ZIRAAI",
        ///     "balance": 1500.50,
        ///     "currency": "TL",
        ///     "isActive": true,
        ///     "statusMessage": "Provider aktif ve çalışıyor"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <returns>SMS provider information and status</returns>
        [HttpGet("provider")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SmsProviderInfoResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProviderInfo()
        {
            var result = await Mediator.Send(new GetSmsProviderInfoQuery());
            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        /// <summary>
        /// Send a test SMS to verify provider configuration
        /// </summary>
        /// <remarks>
        /// Sends a test SMS to the specified phone number using the configured provider.
        /// Use this to verify SMS configuration before production use.
        ///
        /// Phone number formats supported:
        /// - 05371234567
        /// - 5371234567
        /// - 905371234567
        /// - +905371234567
        ///
        /// Sample request:
        /// ```json
        /// {
        ///   "phoneNumber": "05371234567",
        ///   "message": "Test SMS from ZiraAI" // Optional
        /// }
        /// ```
        ///
        /// Sample response:
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Test SMS başarıyla gönderildi.",
        ///   "data": {
        ///     "success": true,
        ///     "messageId": "NETGSM-123456789",
        ///     "provider": "Netgsm",
        ///     "phoneNumber": "05371234567",
        ///     "message": "Test SMS from ZiraAI",
        ///     "sentAt": "2025-11-19T14:30:00",
        ///     "balance": 1500.25,
        ///     "currency": "TL"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="command">Test SMS request with phone number and optional message</param>
        /// <returns>Test result with message ID and provider info</returns>
        [HttpPost("test")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TestSmsResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> TestSms([FromBody] TestSmsCommand command)
        {
            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
