using Core.Utilities.Results;
using IResult = Core.Utilities.Results.IResult;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.SponsorRequest
{
    public interface ISponsorRequestService
    {
        Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId);
        Task<IDataResult<Entities.Concrete.SponsorRequest>> ProcessDeeplinkAsync(string hashedToken);
        Task<IResult> ApproveRequestsAsync(List<int> requestIds, int sponsorId, int tierId, string notes);
        Task<IDataResult<List<Entities.Concrete.SponsorRequest>>> GetPendingRequestsAsync(int sponsorId);
        string GenerateWhatsAppMessage(Entities.Concrete.SponsorRequest request);
        string GenerateRequestToken(string farmerPhone, string sponsorPhone, int farmerId);
        Task<Entities.Concrete.SponsorRequest> ValidateRequestTokenAsync(string token);
    }
}