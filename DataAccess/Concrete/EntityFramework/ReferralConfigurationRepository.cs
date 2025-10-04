using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class ReferralConfigurationRepository : EfEntityRepositoryBase<ReferralConfiguration, ProjectDbContext>, IReferralConfigurationRepository
    {
        public ReferralConfigurationRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<ReferralConfiguration> GetByKeyAsync(string key)
        {
            return await Context.Set<ReferralConfiguration>()
                .FirstOrDefaultAsync(rc => rc.Key == key);
        }

        public async Task<string> GetValueAsync(string key)
        {
            var config = await GetByKeyAsync(key);
            return config?.Value;
        }

        public async Task<bool> UpdateValueAsync(string key, string value, int? updatedBy = null)
        {
            var config = await GetByKeyAsync(key);
            if (config == null)
                return false;

            config.Value = value;
            config.UpdatedAt = DateTime.Now;
            config.UpdatedBy = updatedBy;

            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetIntValueAsync(string key, int defaultValue = 0)
        {
            var value = await GetValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task<bool> GetBoolValueAsync(string key, bool defaultValue = false)
        {
            var value = await GetValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0)
        {
            var value = await GetValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }
    }
}
