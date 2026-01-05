using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Service for bulk farmer invitation processing via RabbitMQ
    /// Pattern: Same as IBulkDealerInvitationService
    /// </summary>
    public interface IBulkFarmerInvitationService
    {
        /// <summary>
        /// Queue bulk farmer invitations for async processing
        /// </summary>
        /// <param name="excelFile">Excel file with farmer list</param>
        /// <param name="sponsorId">Sponsor ID</param>
        /// <param name="channel">SMS or WhatsApp</param>
        /// <param name="customMessage">Optional custom message template</param>
        /// <returns>Bulk job information with JobId for status tracking</returns>
        Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string channel,
            string customMessage);
    }
}
