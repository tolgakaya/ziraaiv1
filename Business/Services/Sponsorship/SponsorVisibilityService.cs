using Business.Services.Sponsorship;
using Business.Services.Subscription;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class SponsorVisibilityConfig
    {
        public bool showLogo { get; set; }
        public bool showProfile { get; set; }
    }

    public class SponsorVisibilityService : ISponsorVisibilityService
    {
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ITierFeatureService _tierFeatureService;

        public SponsorVisibilityService(
            IPlantAnalysisRepository plantAnalysisRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            ITierFeatureService tierFeatureService)
        {
            _plantAnalysisRepository = plantAnalysisRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _tierFeatureService = tierFeatureService;
        }

        public async Task<bool> CanShowLogoOnResultScreenAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive)
                return false;

            // Tüm paket tipleri result screen'de logo gösterebilir
            return true;
        }

        public async Task<bool> CanShowLogoOnStartScreenAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive)
                return false;

            // Check if sponsor has sponsor_visibility feature with showLogo=true
            var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);
            
            foreach (var purchase in purchases)
            {
                var hasVisibility = await _tierFeatureService.HasFeatureAccessAsync(purchase.SubscriptionTierId, "sponsor_visibility");
                if (hasVisibility)
                {
                    var config = await _tierFeatureService.GetFeatureConfigAsync<SponsorVisibilityConfig>(purchase.SubscriptionTierId, "sponsor_visibility");
                    if (config?.showLogo == true)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public async Task<bool> CanShowLogoOnAllScreensAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive)
                return false;

            // Check if sponsor has sponsor_visibility feature with both showLogo and showProfile=true
            var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);
            
            foreach (var purchase in purchases)
            {
                var hasVisibility = await _tierFeatureService.HasFeatureAccessAsync(purchase.SubscriptionTierId, "sponsor_visibility");
                if (hasVisibility)
                {
                    var config = await _tierFeatureService.GetFeatureConfigAsync<SponsorVisibilityConfig>(purchase.SubscriptionTierId, "sponsor_visibility");
                    if (config?.showLogo == true && config?.showProfile == true)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public async Task<bool> CanShowLogoForAnalysisAsync(int plantAnalysisId)
        {
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis?.SponsorshipCodeId == null)
                return false;

            var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(c => c.Id == analysis.SponsorshipCodeId);
            if (sponsorshipCode == null)
                return false;

            var purchase = await _sponsorshipPurchaseRepository.GetAsync(p => p.Id == sponsorshipCode.SponsorshipPurchaseId);
            if (purchase == null)
                return false;

            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(purchase.SponsorId);
            if (sponsorProfile == null || !sponsorProfile.IsActive)
                return false;

            return true;
        }

        public async Task<string> GetTierNameFromAnalysisAsync(int plantAnalysisId)
        {
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis?.SponsorshipCodeId == null)
                return null;

            var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(c => c.Id == analysis.SponsorshipCodeId);
            if (sponsorshipCode == null)
                return null;

            var purchase = await _sponsorshipPurchaseRepository.GetAsync(p => p.Id == sponsorshipCode.SponsorshipPurchaseId);
            if (purchase == null)
                return null;

            var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
            return tier?.TierName;
        }

        public async Task<Entities.Concrete.SponsorProfile> GetSponsorFromAnalysisAsync(int plantAnalysisId)
        {
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis?.SponsorshipCodeId == null)
                return null;

            var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(c => c.Id == analysis.SponsorshipCodeId);
            if (sponsorshipCode == null)
                return null;

            var purchase = await _sponsorshipPurchaseRepository.GetAsync(p => p.Id == sponsorshipCode.SponsorshipPurchaseId);
            if (purchase == null)
                return null;

            return await _sponsorProfileRepository.GetBySponsorIdAsync(purchase.SponsorId);
        }

        public async Task<bool> CanShowLogoOnScreenAsync(int plantAnalysisId, string screenType)
        {
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis?.SponsorshipCodeId == null)
                return false;

            var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(c => c.Id == analysis.SponsorshipCodeId);
            if (sponsorshipCode == null)
                return false;

            var purchase = await _sponsorshipPurchaseRepository.GetAsync(p => p.Id == sponsorshipCode.SponsorshipPurchaseId);
            if (purchase == null)
                return false;

            // Check sponsor_visibility feature access
            var hasVisibility = await _tierFeatureService.HasFeatureAccessAsync(purchase.SubscriptionTierId, "sponsor_visibility");
            if (!hasVisibility)
                return false;

            var config = await _tierFeatureService.GetFeatureConfigAsync<SponsorVisibilityConfig>(purchase.SubscriptionTierId, "sponsor_visibility");
            if (config == null)
                return false;

            return screenType.ToLower() switch
            {
                "result" => config.showLogo, // Show logo if configured
                "start" => config.showLogo,
                "analysis" => config.showLogo && config.showProfile, // Full visibility required
                "profile" => config.showLogo && config.showProfile, // Full visibility required
                _ => false
            };
        }
    }
}