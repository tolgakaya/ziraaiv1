using Business.Services.Queue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Sponsor,Admin")]
    public class BulkOperationsController : BaseApiController
    {
        private readonly IBulkOperationService _bulkOperationService;

        public BulkOperationsController(IBulkOperationService bulkOperationService)
        {
            _bulkOperationService = bulkOperationService;
        }

        /// <summary>
        /// Process bulk link sending operation with queue management
        /// Supports SMS and WhatsApp channels with personalized messages
        /// </summary>
        [HttpPost("send-links")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ProcessBulkLinkSend([FromBody] BulkLinkSendRequest request)
        {
            try
            {
                var sponsorId = GetCurrentUserId();
                if (!sponsorId.HasValue)
                {
                    return Forbid("Sponsor ID bulunamadı");
                }

                // Set sponsor ID from token
                request.SponsorId = sponsorId.Value;

                var result = await _bulkOperationService.ProcessBulkLinkSendAsync(request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"Bulk link gönderim işlemi başarısız: {ex.Message}" });
            }
        }

        /// <summary>
        /// Process bulk sponsorship code generation
        /// Generates codes in batches with configurable validity periods
        /// </summary>
        [HttpPost("generate-codes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ProcessBulkCodeGeneration([FromBody] BulkCodeGenerationRequest request)
        {
            try
            {
                var sponsorId = GetCurrentUserId();
                if (!sponsorId.HasValue)
                {
                    return Forbid("Sponsor ID bulunamadı");
                }

                // Set sponsor ID from token
                request.SponsorId = sponsorId.Value;

                var result = await _bulkOperationService.ProcessBulkCodeGenerationAsync(request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"Bulk kod oluşturma işlemi başarısız: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get real-time status of bulk operation with progress metrics
        /// </summary>
        [HttpGet("status/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOperationStatus(string operationId)
        {
            try
            {
                var result = await _bulkOperationService.GetBulkOperationStatusAsync(operationId);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"İşlem durumu alınamadı: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get paginated history of bulk operations for current sponsor
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOperationHistory([FromQuery] int pageSize = 50)
        {
            try
            {
                var sponsorId = GetCurrentUserId();
                if (!sponsorId.HasValue)
                {
                    return Forbid("Sponsor ID bulunamadı");
                }

                var result = await _bulkOperationService.GetBulkOperationHistoryAsync(sponsorId.Value, pageSize);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"İşlem geçmişi alınamadı: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cancel a running bulk operation
        /// </summary>
        [HttpPost("cancel/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOperation(string operationId)
        {
            try
            {
                var result = await _bulkOperationService.CancelBulkOperationAsync(operationId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"İşlem iptal edilemedi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retry failed items in a bulk operation
        /// </summary>
        [HttpPost("retry/{operationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RetryFailedItems(string operationId)
        {
            try
            {
                var result = await _bulkOperationService.RetryFailedBulkItemsAsync(operationId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"Başarısız öğeler yeniden denenemedi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get bulk operation templates for common scenarios
        /// </summary>
        [HttpGet("templates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBulkOperationTemplates()
        {
            try
            {
                var templates = new
                {
                    link_send_templates = new[]
                    {
                        new
                        {
                            name = "Sponsorship Invitation",
                            channel = "WhatsApp",
                            message = "Merhaba {name}! Tarım sponsorluğu programımıza katılmak için bu linki kullanın: {link}",
                            processing_options = new
                            {
                                batch_size = 25,
                                max_concurrency = 3,
                                retry_attempts = 2
                            }
                        },
                        new
                        {
                            name = "SMS Campaign",
                            channel = "SMS",
                            message = "Değerli çiftçimiz {name}, ücretsiz tarım analizi için: {link}. Kod: {code}",
                            processing_options = new
                            {
                                batch_size = 50,
                                max_concurrency = 5,
                                retry_attempts = 3
                            }
                        }
                    },
                    code_generation_templates = new[]
                    {
                        new
                        {
                            name = "Small Batch (S Tier)",
                            quantity = 100,
                            subscription_tier_id = 1,
                            code_prefix = "SMLS",
                            validity_days = 90
                        },
                        new
                        {
                            name = "Large Batch (L Tier)",
                            quantity = 500,
                            subscription_tier_id = 3,
                            code_prefix = "LRGL",
                            validity_days = 180
                        },
                        new
                        {
                            name = "Enterprise (XL Tier)",
                            quantity = 1000,
                            subscription_tier_id = 4,
                            code_prefix = "XLEN",
                            validity_days = 365
                        }
                    }
                };

                return Ok(new { success = true, data = templates });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"Şablonlar alınamadı: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get bulk operation statistics for dashboard
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBulkOperationStatistics()
        {
            try
            {
                var sponsorId = GetCurrentUserId();
                if (!sponsorId.HasValue)
                {
                    return Forbid("Sponsor ID bulunamadı");
                }

                // Mock statistics - in production, calculate from actual data
                var statistics = new
                {
                    total_operations = 24,
                    successful_operations = 22,
                    failed_operations = 1,
                    cancelled_operations = 1,
                    total_items_processed = 3247,
                    total_successful_items = 3089,
                    overall_success_rate = 95.1,
                    average_processing_time_minutes = 8.5,
                    last_30_days = new
                    {
                        operations_count = 15,
                        links_sent = 2156,
                        codes_generated = 890,
                        success_rate = 96.2
                    },
                    channel_breakdown = new
                    {
                        whatsapp = new { count = 1845, success_rate = 97.1 },
                        sms = new { count = 1402, success_rate = 93.8 }
                    },
                    popular_times = new[]
                    {
                        new { hour = 9, operations = 8, success_rate = 98.2 },
                        new { hour = 14, operations = 6, success_rate = 96.8 },
                        new { hour = 16, operations = 5, success_rate = 94.5 }
                    }
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = $"İstatistikler alınamadı: {ex.Message}" });
            }
        }

        #region Private Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim?.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        #endregion
    }
}