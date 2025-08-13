using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    public class SubscriptionTierRepository : EfEntityRepositoryBase<SubscriptionTier, ProjectDbContext>, ISubscriptionTierRepository
    {
        public SubscriptionTierRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<SubscriptionTier> GetByTierNameAsync(string tierName)
        {
            return await Context.SubscriptionTiers
                .FirstOrDefaultAsync(x => x.TierName == tierName && x.IsActive);
        }

        public async Task<List<SubscriptionTier>> GetActiveTiersAsync()
        {
            return await Context.SubscriptionTiers
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<SubscriptionTier>> GetAllTiersOrderedAsync()
        {
            return await Context.SubscriptionTiers
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();
        }
    }
}