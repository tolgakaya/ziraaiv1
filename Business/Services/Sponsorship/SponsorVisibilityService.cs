using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class SponsorVisibilityService : ISponsorVisibilityService
    {
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;

        public SponsorVisibilityService(
            IPlantAnalysisRepository plantAnalysisRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            ISponsorProfileRepository sponsorProfileRepository)
        {
            _plantAnalysisRepository = plantAnalysisRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
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

            // Sponsor'un M, L veya XL paketi satın almış olması gerekiyor
            var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);
            
            foreach (var purchase in purchases)
            {
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
                if (tier != null && (tier.TierName == "M" || tier.TierName == "L" || tier.TierName == "XL"))
                {
                    return true;
                }
            }
            
            return false;
        }

        public async Task<bool> CanShowLogoOnAllScreensAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive)
                return false;

            // Sponsor'un L veya XL paketi satın almış olması gerekiyor
            var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);
            
            foreach (var purchase in purchases)
            {
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
                if (tier != null && (tier.TierName == "L" || tier.TierName == "XL"))
                {
                    return true;
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
            var tierName = await GetTierNameFromAnalysisAsync(plantAnalysisId);
            if (string.IsNullOrEmpty(tierName))
                return false;

            return screenType.ToLower() switch
            {
                "result" => true, // Tüm tier'lar result screen'de gösterebilir
                "start" => tierName == "M" || tierName == "L" || tierName == "XL",
                "analysis" => tierName == "L" || tierName == "XL",
                "profile" => tierName == "L" || tierName == "XL",
                _ => false
            };
        }
    }
}