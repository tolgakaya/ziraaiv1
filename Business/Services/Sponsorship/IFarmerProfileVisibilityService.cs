using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface IFarmerProfileVisibilityService
    {
        /// <summary>
        /// Check if sponsor can see farmer profile (L/XL tiers only)
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <returns>True if sponsor can see farmer identities</returns>
        Task<bool> CanViewFarmerProfileAsync(int sponsorId);

        /// <summary>
        /// Check if sponsor can see farmer profile for specific analysis
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <param name="plantAnalysisId">Plant analysis ID</param>
        /// <returns>True if sponsor can see farmer identity for this analysis</returns>
        Task<bool> CanViewFarmerProfileForAnalysisAsync(int sponsorId, int plantAnalysisId);

        /// <summary>
        /// Get farmer visibility level for sponsor tier
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <returns>Visibility level: None, Anonymous, Full</returns>
        Task<string> GetFarmerVisibilityLevelAsync(int sponsorId);

        /// <summary>
        /// Check if farmer data should be anonymized for this sponsor
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <returns>True if farmer data should be anonymized</returns>
        Task<bool> ShouldAnonymizeFarmerDataAsync(int sponsorId);
    }
}