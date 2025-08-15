using Entities.Concrete;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface ISponsorVisibilityService
    {
        Task<bool> CanShowLogoOnResultScreenAsync(int sponsorId);
        Task<bool> CanShowLogoOnStartScreenAsync(int sponsorId);
        Task<bool> CanShowLogoOnAllScreensAsync(int sponsorId);
        Task<string> GetSponsorLogoUrlAsync(int sponsorId);
        Task<SponsorProfile> GetSponsorDisplayInfoAsync(int sponsorId);
        Task<string> GetVisibilityLevelAsync(int sponsorId);
        Task UpdateSponsorVisibilityAsync(int sponsorId, string visibilityLevel);
    }
}