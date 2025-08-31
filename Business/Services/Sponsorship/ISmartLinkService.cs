using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface ISmartLinkService
    {
        Task<bool> CanCreateSmartLinksAsync(int sponsorId);
        Task<int> GetMaxSmartLinksAsync(int sponsorId);
        Task<int> GetActiveSmartLinksCountAsync(int sponsorId);
        Task<Entities.Concrete.SmartLink> CreateSmartLinkAsync(Entities.Concrete.SmartLink smartLink);
        Task<List<Entities.Concrete.SmartLink>> GetMatchingLinksAsync(Entities.Concrete.PlantAnalysis analysis);
        Task<List<Entities.Concrete.SmartLink>> GetSponsorLinksAsync(int sponsorId);
        Task<Entities.Concrete.SmartLink> UpdateSmartLinkAsync(Entities.Concrete.SmartLink smartLink);
        Task<bool> DeleteSmartLinkAsync(int linkId, int sponsorId);
        Task IncrementClickAsync(int linkId);
        Task<List<Entities.Concrete.SmartLink>> GetTopPerformingLinksAsync(int sponsorId, int count = 10);
        Task<decimal> CalculateRelevanceScoreAsync(Entities.Concrete.SmartLink link, Entities.Concrete.PlantAnalysis analysis);
        Task<List<Entities.Concrete.SmartLink>> GetPromotionalLinksAsync();
        Task<bool> ApproveSmartLinkAsync(int linkId, int approvedByUserId);
        Task<List<Entities.Concrete.SmartLink>> GetPendingApprovalLinksAsync();
        Task UpdateSmartLinkPerformanceAsync(int linkId);
        Task<List<Entities.Concrete.SmartLink>> GetAIOptimizedLinksAsync(Entities.Concrete.PlantAnalysis analysis);
    }
}