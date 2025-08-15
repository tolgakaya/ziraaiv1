using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class SponsorVisibilityService : ISponsorVisibilityService
    {
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;

        public SponsorVisibilityService(
            ISponsorProfileRepository sponsorProfileRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository)
        {
            _sponsorProfileRepository = sponsorProfileRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
        }

        public async Task<bool> CanShowLogoOnResultScreenAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive || !profile.IsVerified)
                return false;

            // S, M, L, XL paketlerin hepsi result screen'de logo gösterebilir
            return profile.VisibilityLevel == "ResultOnly" ||
                   profile.VisibilityLevel == "StartAndResult" ||
                   profile.VisibilityLevel == "AllScreens";
        }

        public async Task<bool> CanShowLogoOnStartScreenAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive || !profile.IsVerified)
                return false;

            // Sadece M, L, XL paketler start screen'de logo gösterebilir
            return profile.VisibilityLevel == "StartAndResult" ||
                   profile.VisibilityLevel == "AllScreens";
        }

        public async Task<bool> CanShowLogoOnAllScreensAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive || !profile.IsVerified)
                return false;

            // Sadece L ve XL paketler tüm ekranlarda logo gösterebilir
            return profile.VisibilityLevel == "AllScreens";
        }

        public async Task<string> GetSponsorLogoUrlAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            return profile?.SponsorLogoUrl;
        }

        public async Task<SponsorProfile> GetSponsorDisplayInfoAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive || !profile.IsVerified)
                return null;

            return new SponsorProfile
            {
                Id = profile.Id,
                SponsorId = profile.SponsorId,
                CompanyName = profile.CompanyName,
                SponsorLogoUrl = profile.SponsorLogoUrl,
                WebsiteUrl = profile.WebsiteUrl,
                VisibilityLevel = profile.VisibilityLevel,
                DataAccessLevel = profile.DataAccessLevel,
                HasMessaging = profile.HasMessaging,
                HasSmartLinking = profile.HasSmartLinking,
                IsVerified = profile.IsVerified,
                IsActive = profile.IsActive
            };
        }

        public async Task<string> GetVisibilityLevelAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            return profile?.VisibilityLevel ?? "None";
        }

        public async Task UpdateSponsorVisibilityAsync(int sponsorId, string visibilityLevel)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile != null)
            {
                profile.VisibilityLevel = visibilityLevel;
                profile.UpdatedDate = System.DateTime.Now;
                
                await _sponsorProfileRepository.UpdateAsync(profile);
            }
        }
    }
}