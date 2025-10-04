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
    public class ReferralTrackingRepository : EfEntityRepositoryBase<ReferralTracking, ProjectDbContext>, IReferralTrackingRepository
    {
        public ReferralTrackingRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<ReferralTracking>> GetByReferralCodeIdAsync(int referralCodeId)
        {
            return await Context.Set<ReferralTracking>()
                .Where(rt => rt.ReferralCodeId == referralCodeId)
                .OrderByDescending(rt => rt.ClickedAt)
                .ToListAsync();
        }

        public async Task<ReferralTracking> GetByRefereeUserIdAsync(int refereeUserId)
        {
            return await Context.Set<ReferralTracking>()
                .FirstOrDefaultAsync(rt => rt.RefereeUserId == refereeUserId);
        }

        public async Task<ReferralTracking> GetByDeviceAndCodeAsync(string deviceId, int referralCodeId)
        {
            return await Context.Set<ReferralTracking>()
                .FirstOrDefaultAsync(rt =>
                    rt.DeviceId == deviceId &&
                    rt.ReferralCodeId == referralCodeId);
        }

        public async Task<List<ReferralTracking>> GetByReferrerUserIdAsync(int referrerUserId)
        {
            return await Context.Set<ReferralTracking>()
                .Join(
                    Context.Set<ReferralCode>(),
                    tracking => tracking.ReferralCodeId,
                    code => code.Id,
                    (tracking, code) => new { tracking, code })
                .Where(x => x.code.UserId == referrerUserId)
                .Select(x => x.tracking)
                .OrderByDescending(rt => rt.ClickedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetStatsByReferralCodeIdAsync(int referralCodeId)
        {
            var trackings = await GetByReferralCodeIdAsync(referralCodeId);

            return new Dictionary<string, int>
            {
                { "Clicked", trackings.Count(t => t.Status == (int)ReferralTrackingStatus.Clicked) },
                { "Registered", trackings.Count(t => t.Status >= (int)ReferralTrackingStatus.Registered) },
                { "Validated", trackings.Count(t => t.Status >= (int)ReferralTrackingStatus.Validated) },
                { "Rewarded", trackings.Count(t => t.Status == (int)ReferralTrackingStatus.Rewarded) },
                { "Total", trackings.Count }
            };
        }

        public async Task<Dictionary<string, int>> GetStatsByReferrerUserIdAsync(int referrerUserId)
        {
            var trackings = await GetByReferrerUserIdAsync(referrerUserId);

            return new Dictionary<string, int>
            {
                { "Clicked", trackings.Count(t => t.Status == (int)ReferralTrackingStatus.Clicked) },
                { "Registered", trackings.Count(t => t.Status >= (int)ReferralTrackingStatus.Registered) },
                { "Validated", trackings.Count(t => t.Status >= (int)ReferralTrackingStatus.Validated) },
                { "Rewarded", trackings.Count(t => t.Status == (int)ReferralTrackingStatus.Rewarded) },
                { "Total", trackings.Count }
            };
        }

        public async Task<bool> UpdateStatusAsync(int trackingId, ReferralTrackingStatus status)
        {
            var tracking = await Context.Set<ReferralTracking>()
                .FirstOrDefaultAsync(rt => rt.Id == trackingId);

            if (tracking == null)
                return false;

            tracking.Status = (int)status;
            await Context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ReferralTracking>> GetPendingValidationsAsync()
        {
            return await Context.Set<ReferralTracking>()
                .Where(rt => rt.Status == (int)ReferralTrackingStatus.Registered)
                .OrderBy(rt => rt.RegisteredAt)
                .ToListAsync();
        }
    }
}
