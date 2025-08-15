using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ISmartLinkRepository : IEntityRepository<SmartLink>
    {
        Task<List<SmartLink>> GetBySponsorIdAsync(int sponsorId);
        Task<List<SmartLink>> GetActiveLinksAsync();
        Task<List<SmartLink>> GetMatchingLinksAsync(string[] keywords, string cropType = null, string disease = null, string pest = null);
        Task<List<SmartLink>> GetByProductCategoryAsync(string category);
        Task IncrementClickCountAsync(int linkId);
        Task UpdateClickThroughRateAsync(int linkId, decimal ctr);
        Task<List<SmartLink>> GetTopPerformingLinksAsync(int sponsorId, int count = 10);
        Task<List<SmartLink>> GetPendingApprovalAsync();
        Task ApproveSmartLinkAsync(int linkId, int approvedByUserId);
        Task<List<SmartLink>> GetPromotionalLinksAsync();
        Task<decimal> GetTotalSpentBudgetAsync(int sponsorId);
        Task<List<SmartLink>> GetLinksByPerformanceAsync(int sponsorId, decimal minCtr = 0);
    }
}