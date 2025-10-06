using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class ReferralCodeRepository : EfEntityRepositoryBase<ReferralCode, ProjectDbContext>, IReferralCodeRepository
    {
        public ReferralCodeRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<ReferralCode> GetByCodeAsync(string code)
        {
            return await Context.Set<ReferralCode>()
                .FirstOrDefaultAsync(rc => rc.Code == code);
        }

        public async Task<ReferralCode> GetActiveCodeAsync(string code)
        {
            var now = DateTime.Now;
            return await Context.Set<ReferralCode>()
                .FirstOrDefaultAsync(rc =>
                    rc.Code == code &&
                    rc.IsActive &&
                    rc.Status == (int)ReferralCodeStatus.Active &&
                    rc.ExpiresAt > now);
        }

        public async Task<List<ReferralCode>> GetByUserIdAsync(int userId)
        {
            return await Context.Set<ReferralCode>()
                .Where(rc => rc.UserId == userId)
                .OrderByDescending(rc => rc.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReferralCode>> GetActiveCodesByUserIdAsync(int userId)
        {
            var now = DateTime.Now;
            return await Context.Set<ReferralCode>()
                .Where(rc =>
                    rc.UserId == userId &&
                    rc.IsActive &&
                    rc.Status == (int)ReferralCodeStatus.Active &&
                    rc.ExpiresAt > now)
                .OrderByDescending(rc => rc.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsCodeValidAsync(string code)
        {
            var now = DateTime.Now;
            return await Context.Set<ReferralCode>()
                .AnyAsync(rc =>
                    rc.Code == code &&
                    rc.IsActive &&
                    rc.Status == (int)ReferralCodeStatus.Active &&
                    rc.ExpiresAt > now);
        }

        public async Task<int> MarkExpiredCodesAsync()
        {
            var now = DateTime.Now;
            var expiredCodes = await Context.Set<ReferralCode>()
                .Where(rc =>
                    rc.Status == (int)ReferralCodeStatus.Active &&
                    rc.ExpiresAt <= now)
                .ToListAsync();

            foreach (var code in expiredCodes)
            {
                code.Status = (int)ReferralCodeStatus.Expired;
            }

            if (expiredCodes.Any())
            {
                await Context.SaveChangesAsync();
            }

            return expiredCodes.Count;
        }

        public async Task<bool> DisableCodeAsync(string code)
        {
            var referralCode = await GetByCodeAsync(code);
            if (referralCode == null)
                return false;

            referralCode.IsActive = false;
            referralCode.Status = (int)ReferralCodeStatus.Disabled;

            await Context.SaveChangesAsync();
            return true;
        }
    }
}
