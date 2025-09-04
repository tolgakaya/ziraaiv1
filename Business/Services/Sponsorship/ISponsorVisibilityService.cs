using Entities.Concrete;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface ISponsorVisibilityService
    {
        // Legacy methods for sponsor-based checks (for existing purchases)
        Task<bool> CanShowLogoOnResultScreenAsync(int sponsorId);
        Task<bool> CanShowLogoOnStartScreenAsync(int sponsorId);
        Task<bool> CanShowLogoOnAllScreensAsync(int sponsorId);
        
        // New analysis-based methods (tier from redeemed code)
        Task<bool> CanShowLogoForAnalysisAsync(int plantAnalysisId);
        Task<string> GetTierNameFromAnalysisAsync(int plantAnalysisId);
        Task<SponsorProfile> GetSponsorFromAnalysisAsync(int plantAnalysisId);
        Task<bool> CanShowLogoOnScreenAsync(int plantAnalysisId, string screenType);
    }
}