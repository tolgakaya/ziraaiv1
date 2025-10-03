using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IReferralConfigurationRepository : IEntityRepository<ReferralConfiguration>
    {
        /// <summary>
        /// Get configuration by key
        /// </summary>
        Task<ReferralConfiguration> GetByKeyAsync(string key);

        /// <summary>
        /// Get configuration value by key
        /// </summary>
        Task<string> GetValueAsync(string key);

        /// <summary>
        /// Update configuration value
        /// </summary>
        Task<bool> UpdateValueAsync(string key, string value, int? updatedBy = null);

        /// <summary>
        /// Get configuration value as integer
        /// </summary>
        Task<int> GetIntValueAsync(string key, int defaultValue = 0);

        /// <summary>
        /// Get configuration value as boolean
        /// </summary>
        Task<bool> GetBoolValueAsync(string key, bool defaultValue = false);

        /// <summary>
        /// Get configuration value as decimal
        /// </summary>
        Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0);
    }
}
