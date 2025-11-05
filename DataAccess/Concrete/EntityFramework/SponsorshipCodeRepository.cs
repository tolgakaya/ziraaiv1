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
    public class SponsorshipCodeRepository : EfEntityRepositoryBase<SponsorshipCode, ProjectDbContext>, ISponsorshipCodeRepository
    {
        public SponsorshipCodeRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<SponsorshipCode> GetByCodeAsync(string code)
        {
            return await Context.SponsorshipCodes
                .FirstOrDefaultAsync(sc => sc.Code == code);
        }

        public async Task<SponsorshipCode> GetUnusedCodeAsync(string code)
        {
            return await Context.SponsorshipCodes
                .FirstOrDefaultAsync(sc => sc.Code == code && !sc.IsUsed && sc.IsActive && sc.ExpiryDate > DateTime.Now);
        }

        public async Task<List<SponsorshipCode>> GetBySponsorIdAsync(int sponsorId)
        {
            return await Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId)
                .OrderByDescending(sc => sc.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SponsorshipCode>> GetByPurchaseIdAsync(int purchaseId)
        {
            return await Context.SponsorshipCodes
                .Where(sc => sc.SponsorshipPurchaseId == purchaseId)
                .ToListAsync();
        }

        public async Task<List<SponsorshipCode>> GetUnusedCodesBySponsorAsync(int sponsorId)
        {
            return await Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId && !sc.IsUsed && sc.IsActive && sc.ExpiryDate > DateTime.Now)
                .OrderBy(sc => sc.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SponsorshipCode>> GetUsedCodesBySponsorAsync(int sponsorId)
        {
            return await Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId && sc.IsUsed)
                .OrderByDescending(sc => sc.UsedDate)
                .ToListAsync();
        }

        public async Task<List<SponsorshipCode>> GetUnsentCodesBySponsorAsync(int sponsorId)
        {
            return await Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId &&
                            !sc.IsUsed &&
                            sc.IsActive &&
                            sc.ExpiryDate > DateTime.Now &&
                            !sc.DistributionDate.HasValue)  // KEY: Never sent (DistributionDate IS NULL)
                .OrderBy(sc => sc.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SponsorshipCode>> GetSentButUnusedCodesBySponsorAsync(int sponsorId, int? sentDaysAgo = null)
        {
            var query = Context.SponsorshipCodes
                .Where(sc => sc.SponsorId == sponsorId &&
                            !sc.IsUsed &&
                            sc.IsActive &&
                            sc.ExpiryDate > DateTime.Now &&
                            sc.DistributionDate.HasValue);  // KEY: Has been sent (DistributionDate IS NOT NULL)

            // If sentDaysAgo is specified, filter codes sent at least X days ago
            if (sentDaysAgo.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-sentDaysAgo.Value);
                query = query.Where(sc => sc.DistributionDate <= cutoffDate);
            }

            return await query
                .OrderByDescending(sc => sc.DistributionDate)
                .ToListAsync();
        }

        public async Task<int> GetUsedCountByPurchaseAsync(int purchaseId)
        {
            return await Context.SponsorshipCodes
                .CountAsync(sc => sc.SponsorshipPurchaseId == purchaseId && sc.IsUsed);
        }

        public async Task<bool> IsCodeValidAsync(string code)
        {
            return await Context.SponsorshipCodes
                .AnyAsync(sc => sc.Code == code && !sc.IsUsed && sc.IsActive && sc.ExpiryDate > DateTime.Now);
        }

        public async Task<bool> MarkAsUsedAsync(string code, int userId, int subscriptionId)
        {
            var sponsorshipCode = await GetUnusedCodeAsync(code);
            if (sponsorshipCode == null)
                return false;

            sponsorshipCode.IsUsed = true;
            sponsorshipCode.UsedByUserId = userId;
            sponsorshipCode.UsedDate = DateTime.Now;
            sponsorshipCode.CreatedSubscriptionId = subscriptionId;

            Context.SponsorshipCodes.Update(sponsorshipCode);
            await Context.SaveChangesAsync();

            // Update the purchase's used count
            var purchase = await Context.SponsorshipPurchases
                .FirstOrDefaultAsync(p => p.Id == sponsorshipCode.SponsorshipPurchaseId);
            if (purchase != null)
            {
                purchase.CodesUsed = await GetUsedCountByPurchaseAsync(purchase.Id);
                purchase.UpdatedDate = DateTime.Now;
                Context.SponsorshipPurchases.Update(purchase);
                await Context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<List<SponsorshipCode>> GenerateCodesAsync(int purchaseId, int sponsorId, int tierId, int quantity, string prefix, int validityDays)
        {
            var codes = new List<SponsorshipCode>();
            var random = new Random();
            var existingCodes = await Context.SponsorshipCodes.Select(sc => sc.Code).ToListAsync();

            for (int i = 0; i < quantity; i++)
            {
                string code;
                do
                {
                    // Generate unique code: PREFIX-YEAR-XXXX
                    var randomPart = random.Next(1000, 9999).ToString();
                    var uniquePart = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    code = $"{prefix}-{DateTime.Now.Year}-{randomPart}{uniquePart}";
                } while (existingCodes.Contains(code) || codes.Any(c => c.Code == code));

                var sponsorshipCode = new SponsorshipCode
                {
                    Code = code,
                    SponsorId = sponsorId,
                    SubscriptionTierId = tierId,
                    SponsorshipPurchaseId = purchaseId,
                    IsUsed = false,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(validityDays)
                };

                codes.Add(sponsorshipCode);
                existingCodes.Add(code);
            }

            await Context.SponsorshipCodes.AddRangeAsync(codes);
            await Context.SaveChangesAsync();

            return codes;
        }

        /// <summary>
        /// Atomically allocates an available code for distribution
        /// Uses raw SQL with UPDATE + WHERE to prevent race conditions in concurrent scenarios
        /// This ensures the same code is never allocated to multiple farmers simultaneously
        /// </summary>
        public async Task<SponsorshipCode> AllocateCodeForDistributionAsync(
            int purchaseId, 
            string recipientPhone, 
            string recipientName)
        {
            // Step 1: Use raw SQL to atomically reserve a code
            // UPDATE with WHERE clause ensures only ONE worker can claim each code
            // PostgreSQL's MVCC and row-level locking handle concurrency
            var sql = $@"
                UPDATE ""SponsorshipCodes""
                SET 
                    ""DistributionDate"" = NOW(),
                    ""RecipientPhone"" = {{0}},
                    ""RecipientName"" = {{1}}
                WHERE ""Id"" = (
                    SELECT ""Id""
                    FROM ""SponsorshipCodes""
                    WHERE ""SponsorshipPurchaseId"" = {{2}}
                      AND ""IsUsed"" = false
                      AND ""DistributionDate"" IS NULL
                      AND ""ExpiryDate"" > NOW()
                    ORDER BY ""Id""
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED  -- Critical: Skip locked rows to prevent blocking
                )
                RETURNING ""Id"";";

            // Execute atomic UPDATE and get allocated code ID
            int? allocatedCodeId = null;
            
            try
            {
                var result = await Context.Database
                    .SqlQueryRaw<int>(sql, recipientPhone, recipientName, purchaseId)
                    .ToListAsync();
                
                allocatedCodeId = result.FirstOrDefault();
            }
            catch (Exception)
            {
                // If UPDATE fails (no available codes), return null
                return null;
            }

            if (allocatedCodeId == null || allocatedCodeId == 0)
            {
                // No available codes found
                return null;
            }

            // Step 2: Fetch the allocated code entity
            var allocatedCode = await Context.SponsorshipCodes
                .FirstOrDefaultAsync(c => c.Id == allocatedCodeId);

            return allocatedCode;
        }
    }
}