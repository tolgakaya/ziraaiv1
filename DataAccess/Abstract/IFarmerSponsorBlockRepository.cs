using System.Threading.Tasks;
using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository for farmer-sponsor blocking operations
    /// </summary>
    public interface IFarmerSponsorBlockRepository : IEntityRepository<FarmerSponsorBlock>
    {
        /// <summary>
        /// Checks if farmer has blocked sponsor
        /// </summary>
        /// <param name="farmerId">Farmer user ID</param>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <returns>True if blocked, false otherwise</returns>
        Task<bool> IsBlockedAsync(int farmerId, int sponsorId);

        /// <summary>
        /// Checks if farmer has muted sponsor
        /// </summary>
        /// <param name="farmerId">Farmer user ID</param>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <returns>True if muted, false otherwise</returns>
        Task<bool> IsMutedAsync(int farmerId, int sponsorId);

        /// <summary>
        /// Gets the block record between farmer and sponsor (if exists)
        /// </summary>
        /// <param name="farmerId">Farmer user ID</param>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <returns>Block record or null</returns>
        Task<FarmerSponsorBlock> GetBlockRecordAsync(int farmerId, int sponsorId);
    }
}
