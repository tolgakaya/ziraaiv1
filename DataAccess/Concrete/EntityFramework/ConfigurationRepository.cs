using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class ConfigurationRepository : EfEntityRepositoryBase<Configuration, ProjectDbContext>, IConfigurationRepository
    {
        public ConfigurationRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<Configuration> GetByKeyAsync(string key)
        {
            return await Context.Configurations
                .Where(x => x.Key == key && x.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<string> GetValueByKeyAsync(string key)
        {
            var config = await GetByKeyAsync(key);
            return config?.Value;
        }

        public async Task<T> GetValueByKeyAsync<T>(string key, T defaultValue = default)
        {
            var config = await GetByKeyAsync(key);
            
            if (config == null || string.IsNullOrEmpty(config.Value))
                return defaultValue;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, config.Value);
                }

                return (T)Convert.ChangeType(config.Value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}