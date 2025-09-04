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
    public class SmartLinkRepository : EfEntityRepositoryBase<SmartLink, ProjectDbContext>, ISmartLinkRepository
    {
        public SmartLinkRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<SmartLink>> GetBySponsorIdAsync(int sponsorId)
        {
            return await Context.SmartLinks
                .Include(x => x.Sponsor)
                .Where(x => x.SponsorId == sponsorId)
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SmartLink>> GetActiveLinksAsync()
        {
            var now = DateTime.Now;
            return await Context.SmartLinks
                .Include(x => x.Sponsor)
                .Where(x => x.IsActive && 
                           x.IsApproved &&
                           (x.StartDate == null || x.StartDate <= now) &&
                           (x.EndDate == null || x.EndDate >= now) &&
                           (x.MaxClickCount == null || x.ClickCount < x.MaxClickCount))
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.RelevanceScore)
                .ToListAsync();
        }

        public async Task<List<SmartLink>> GetMatchingLinksAsync(string[] keywords, string cropType = null, string disease = null, string pest = null)
        {
            var activeLinks = await GetActiveLinksAsync();
            
            var matchingLinks = new List<SmartLink>();
            
            foreach (var link in activeLinks)
            {
                var score = CalculateRelevanceScore(link, keywords, cropType, disease, pest);
                if (score > 0)
                {
                    link.RelevanceScore = score;
                    matchingLinks.Add(link);
                }
            }
            
            return matchingLinks
                .OrderByDescending(x => x.RelevanceScore)
                .ThenByDescending(x => x.Priority)
                .ToList();
        }

        public async Task<List<SmartLink>> GetByProductCategoryAsync(string category)
        {
            return await Context.SmartLinks
                .Include(x => x.Sponsor)
                .Where(x => x.ProductCategory == category && x.IsActive && x.IsApproved)
                .OrderByDescending(x => x.Priority)
                .ToListAsync();
        }

        public async Task IncrementClickCountAsync(int linkId)
        {
            var link = await GetAsync(l => l.Id == linkId);
            if (link != null)
            {
                link.ClickCount++;
                link.LastClickDate = DateTime.Now;
                link.UpdatedDate = DateTime.Now;
                
                // Update CTR
                if (link.DisplayCount > 0)
                {
                    link.ClickThroughRate = (decimal)link.ClickCount / link.DisplayCount * 100;
                }
                
                Context.SmartLinks.Update(link);
                await Context.SaveChangesAsync();
            }
        }

        public async Task UpdateClickThroughRateAsync(int linkId, decimal ctr)
        {
            var link = await GetAsync(l => l.Id == linkId);
            if (link != null)
            {
                link.ClickThroughRate = ctr;
                link.UpdatedDate = DateTime.Now;
                
                Context.SmartLinks.Update(link);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<List<SmartLink>> GetTopPerformingLinksAsync(int sponsorId, int count = 10)
        {
            return await Context.SmartLinks
                .Where(x => x.SponsorId == sponsorId && x.IsActive)
                .OrderByDescending(x => x.ClickThroughRate)
                .ThenByDescending(x => x.ClickCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<SmartLink>> GetPendingApprovalAsync()
        {
            return await Context.SmartLinks
                .Include(x => x.Sponsor)
                .Where(x => !x.IsApproved)
                .OrderBy(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task ApproveSmartLinkAsync(int linkId, int approvedByUserId)
        {
            var link = await GetAsync(l => l.Id == linkId);
            if (link != null)
            {
                link.IsApproved = true;
                link.ApprovalDate = DateTime.Now;
                link.ApprovedByUserId = approvedByUserId;
                link.UpdatedDate = DateTime.Now;
                
                Context.SmartLinks.Update(link);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<List<SmartLink>> GetPromotionalLinksAsync()
        {
            var now = DateTime.Now;
            return await Context.SmartLinks
                .Include(x => x.Sponsor)
                .Where(x => x.IsPromotional && 
                           x.IsActive && 
                           x.IsApproved &&
                           (x.PromotionStartDate == null || x.PromotionStartDate <= now) &&
                           (x.PromotionEndDate == null || x.PromotionEndDate >= now))
                .OrderByDescending(x => x.DiscountPercentage)
                .ThenByDescending(x => x.Priority)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSpentBudgetAsync(int sponsorId)
        {
            return await Context.SmartLinks
                .Where(x => x.SponsorId == sponsorId)
                .SumAsync(x => x.SpentBudget ?? 0);
        }

        public async Task<List<SmartLink>> GetLinksByPerformanceAsync(int sponsorId, decimal minCtr = 0)
        {
            return await Context.SmartLinks
                .Where(x => x.SponsorId == sponsorId && 
                           x.IsActive &&
                           x.ClickThroughRate >= minCtr)
                .OrderByDescending(x => x.ClickThroughRate)
                .ThenByDescending(x => x.ConversionRate)
                .ToListAsync();
        }

        private decimal CalculateRelevanceScore(SmartLink link, string[] keywords, string cropType, string disease, string pest)
        {
            decimal score = 0;
            
            // Parse link keywords
            var linkKeywords = System.Text.Json.JsonSerializer.Deserialize<string[]>(link.Keywords ?? "[]");
            var targetCropTypes = System.Text.Json.JsonSerializer.Deserialize<string[]>(link.TargetCropTypes ?? "[]");
            var targetDiseases = System.Text.Json.JsonSerializer.Deserialize<string[]>(link.TargetDiseases ?? "[]");
            var targetPests = System.Text.Json.JsonSerializer.Deserialize<string[]>(link.TargetPests ?? "[]");
            
            // Keyword matching (30 points max)
            if (keywords != null && linkKeywords != null)
            {
                var matchingKeywords = keywords.Intersect(linkKeywords, StringComparer.OrdinalIgnoreCase).Count();
                score += Math.Min(matchingKeywords * 10, 30);
            }
            
            // Crop type matching (25 points max)
            if (!string.IsNullOrEmpty(cropType) && targetCropTypes != null && 
                targetCropTypes.Any(ct => ct.Equals(cropType, StringComparison.OrdinalIgnoreCase)))
            {
                score += 25;
            }
            
            // Disease matching (25 points max)
            if (!string.IsNullOrEmpty(disease) && targetDiseases != null &&
                targetDiseases.Any(d => d.Equals(disease, StringComparison.OrdinalIgnoreCase)))
            {
                score += 25;
            }
            
            // Pest matching (20 points max)
            if (!string.IsNullOrEmpty(pest) && targetPests != null &&
                targetPests.Any(p => p.Equals(pest, StringComparison.OrdinalIgnoreCase)))
            {
                score += 20;
            }
            
            return score;
        }
    }
}