using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class ReferralRewardRepository : EfEntityRepositoryBase<ReferralReward, ProjectDbContext>, IReferralRewardRepository
    {
        public ReferralRewardRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<ReferralReward>> GetByReferrerUserIdAsync(int referrerUserId)
        {
            return await Context.Set<ReferralReward>()
                .Where(rr => rr.ReferrerUserId == referrerUserId)
                .OrderByDescending(rr => rr.AwardedAt)
                .ToListAsync();
        }

        public async Task<ReferralReward> GetByTrackingIdAsync(int trackingId)
        {
            return await Context.Set<ReferralReward>()
                .FirstOrDefaultAsync(rr => rr.ReferralTrackingId == trackingId);
        }

        public async Task<int> GetTotalCreditsByReferrerAsync(int referrerUserId)
        {
            return await Context.Set<ReferralReward>()
                .Where(rr => rr.ReferrerUserId == referrerUserId)
                .SumAsync(rr => rr.CreditAmount);
        }

        public async Task<int> GetRewardCountByReferrerAsync(int referrerUserId)
        {
            return await Context.Set<ReferralReward>()
                .CountAsync(rr => rr.ReferrerUserId == referrerUserId);
        }

        public async Task<bool> HasRewardAsync(int trackingId)
        {
            return await Context.Set<ReferralReward>()
                .AnyAsync(rr => rr.ReferralTrackingId == trackingId);
        }
    }
}
