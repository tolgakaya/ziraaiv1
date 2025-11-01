using Business.Services.Subscription;
using DataAccess.Abstract;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class FarmerProfileVisibilityService : IFarmerProfileVisibilityService
    {
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;
        private readonly ITierFeatureService _tierFeatureService;

        public FarmerProfileVisibilityService(
            ISponsorProfileRepository sponsorProfileRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
            ITierFeatureService tierFeatureService)
        {
            _sponsorProfileRepository = sponsorProfileRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
            _tierFeatureService = tierFeatureService;
        }

        public async Task<bool> CanViewFarmerProfileAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            
            // If not a sponsor, no special farmer visibility rules
            if (profile == null || !profile.IsActive)
                return false;

            // Check if sponsor has sponsor_visibility feature with showProfile=true
            if (profile.SponsorshipPurchases != null && profile.SponsorshipPurchases.Any())
            {
                foreach (var purchase in profile.SponsorshipPurchases)
                {
                    var hasVisibility = await _tierFeatureService.HasFeatureAccessAsync(purchase.SubscriptionTierId, "sponsor_visibility");
                    if (hasVisibility)
                    {
                        var config = await _tierFeatureService.GetFeatureConfigAsync<SponsorVisibilityConfig>(purchase.SubscriptionTierId, "sponsor_visibility");
                        if (config?.showProfile == true)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        public async Task<bool> CanViewFarmerProfileForAnalysisAsync(int sponsorId, int plantAnalysisId)
        {
            // For now, same logic as general farmer profile visibility
            // Could be extended for analysis-specific rules in the future
            return await CanViewFarmerProfileAsync(sponsorId);
        }

        public async Task<string> GetFarmerVisibilityLevelAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            
            // If not a sponsor or inactive, no visibility
            if (profile == null || !profile.IsActive)
                return "None";

            if (profile.SponsorshipPurchases != null && profile.SponsorshipPurchases.Any())
            {
                // Check if any purchase has full profile visibility
                foreach (var purchase in profile.SponsorshipPurchases)
                {
                    var hasVisibility = await _tierFeatureService.HasFeatureAccessAsync(purchase.SubscriptionTierId, "sponsor_visibility");
                    if (hasVisibility)
                    {
                        var config = await _tierFeatureService.GetFeatureConfigAsync<SponsorVisibilityConfig>(purchase.SubscriptionTierId, "sponsor_visibility");
                        if (config?.showProfile == true)
                        {
                            return "Full"; // L, XL tiers
                        }
                        else if (config?.showLogo == true)
                        {
                            return "Anonymous"; // M tier: logo only
                        }
                    }
                }
                
                // Has purchases but no visibility features
                return "None"; // S tier or Trial
            }
            
            return "None";
        }

        public async Task<bool> ShouldAnonymizeFarmerDataAsync(int sponsorId)
        {
            var visibilityLevel = await GetFarmerVisibilityLevelAsync(sponsorId);
            
            // Anonymize for S and M tiers, show full data for L and XL tiers
            return visibilityLevel != "Full";
        }

        /// <summary>
        /// Get the highest tier ID for sponsor (helper method)
        /// </summary>
        private async Task<int> GetHighestTierIdAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            
            if (profile?.SponsorshipPurchases == null || !profile.SponsorshipPurchases.Any())
                return 0;

            int maxTier = 0;
            foreach (var purchase in profile.SponsorshipPurchases)
            {
                if (purchase.SubscriptionTierId > maxTier)
                    maxTier = purchase.SubscriptionTierId;
            }
            
            return maxTier;
        }
    }
}