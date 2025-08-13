using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface ISubscriptionTierRepository : IEntityRepository<SubscriptionTier>
    {
        Task<SubscriptionTier> GetByTierNameAsync(string tierName);
        Task<List<SubscriptionTier>> GetActiveTiersAsync();
        Task<List<SubscriptionTier>> GetAllTiersOrderedAsync();
    }
}