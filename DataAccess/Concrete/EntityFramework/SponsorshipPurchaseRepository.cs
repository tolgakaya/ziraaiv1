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
    public class SponsorshipPurchaseRepository : EfEntityRepositoryBase<SponsorshipPurchase, ProjectDbContext>, ISponsorshipPurchaseRepository
    {
        public SponsorshipPurchaseRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<SponsorshipPurchase>> GetBySponsorIdAsync(int sponsorId)
        {
            return await Context.SponsorshipPurchases
                .Include(sp => sp.SubscriptionTier)
                .Include(sp => sp.SponsorshipCodes)
                .Where(sp => sp.SponsorId == sponsorId)
                .OrderByDescending(sp => sp.PurchaseDate)
                .ToListAsync();
        }

        public async Task<SponsorshipPurchase> GetWithCodesAsync(int purchaseId)
        {
            return await Context.SponsorshipPurchases
                .Include(sp => sp.SubscriptionTier)
                .Include(sp => sp.SponsorshipCodes)
                .FirstOrDefaultAsync(sp => sp.Id == purchaseId);
        }

        public async Task<List<SponsorshipPurchase>> GetActivePurchasesAsync(int sponsorId)
        {
            return await Context.SponsorshipPurchases
                .Include(sp => sp.SubscriptionTier)
                .Include(sp => sp.SponsorshipCodes)
                .Where(sp => sp.SponsorId == sponsorId && sp.Status == "Active" && sp.PaymentStatus == "Completed")
                .OrderByDescending(sp => sp.PurchaseDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSpentBySponsorAsync(int sponsorId)
        {
            return await Context.SponsorshipPurchases
                .Where(sp => sp.SponsorId == sponsorId && sp.PaymentStatus == "Completed")
                .SumAsync(sp => sp.TotalAmount);
        }

        public async Task<int> GetTotalCodesPurchasedAsync(int sponsorId)
        {
            return await Context.SponsorshipPurchases
                .Where(sp => sp.SponsorId == sponsorId && sp.PaymentStatus == "Completed")
                .SumAsync(sp => sp.CodesGenerated);
        }

        public async Task<int> GetTotalCodesUsedAsync(int sponsorId)
        {
            return await Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId && sc.IsUsed)
                .CountAsync();
        }

        public async Task UpdateCodesUsedCountAsync(int purchaseId)
        {
            var purchase = await Context.SponsorshipPurchases.FindAsync(purchaseId);
            if (purchase != null)
            {
                var usedCount = await Context.SponsorshipCodes
                    .CountAsync(sc => sc.SponsorshipPurchaseId == purchaseId && sc.IsUsed);
                
                purchase.CodesUsed = usedCount;
                purchase.UpdatedDate = System.DateTime.Now;
                
                Context.SponsorshipPurchases.Update(purchase);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<int, int>> GetUsageStatisticsByTierAsync(int sponsorId)
        {
            var stats = await Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId && sc.IsUsed)
                .GroupBy(sc => sc.SubscriptionTierId)
                .Select(g => new { TierId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TierId, x => x.Count);

            return stats;
        }
    }
}