using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IConfigurationRepository : IEntityRepository<Configuration>
    {
        Task<Configuration> GetByKeyAsync(string key);
        Task<string> GetValueByKeyAsync(string key);
        Task<T> GetValueByKeyAsync<T>(string key, T defaultValue = default);
    }
}