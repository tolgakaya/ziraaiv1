using DataAccess.Abstract;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class FarmerProfileVisibilityService : IFarmerProfileVisibilityService
    {
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;

        public FarmerProfileVisibilityService(
            ISponsorProfileRepository sponsorProfileRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository)
        {
            _sponsorProfileRepository = sponsorProfileRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
        }

        public async Task<bool> CanViewFarmerProfileAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            
            // If not a sponsor, no special farmer visibility rules
            if (profile == null || !profile.IsActive)
                return false;

            // Check if sponsor has L or XL tier (messaging tiers = farmer profile visibility)
            if (profile.SponsorshipPurchases != null && profile.SponsorshipPurchases.Any())
            {
                foreach (var purchase in profile.SponsorshipPurchases)
                {
                    // L=3, XL=4 tiers can see farmer profiles
                    if (purchase.SubscriptionTierId >= 3)
                    {
                        return true;
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
                // Get highest tier
                int maxTier = 0;
                foreach (var purchase in profile.SponsorshipPurchases)
                {
                    if (purchase.SubscriptionTierId > maxTier)
                        maxTier = purchase.SubscriptionTierId;
                }

                return maxTier switch
                {
                    1 => "None",        // S tier: No farmer data
                    2 => "Anonymous",   // M tier: Anonymous farmer profiles
                    3 => "Full",        // L tier: Full farmer profiles
                    4 => "Full",        // XL tier: Full farmer profiles
                    _ => "None"
                };
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