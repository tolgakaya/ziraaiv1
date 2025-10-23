using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSubscriptions.Queries
{
    /// <summary>
    /// Query to get all user subscriptions with pagination and filtering
    /// Admin-only operation for subscription management
    /// </summary>
    public class GetAllSubscriptionsQuery : IRequest<IDataResult<List<UserSubscriptionDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? UserId { get; set; }
        public int? TierId { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsTrial { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? EndDateBefore { get; set; }
        public string SortBy { get; set; } = "StartDate";
        public bool SortDescending { get; set; } = true;

        public class GetAllSubscriptionsQueryHandler : IRequestHandler<GetAllSubscriptionsQuery, IDataResult<List<UserSubscriptionDto>>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;

            public GetAllSubscriptionsQueryHandler(IUserSubscriptionRepository subscriptionRepository)
            {
                _subscriptionRepository = subscriptionRepository;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<UserSubscriptionDto>>> Handle(GetAllSubscriptionsQuery request, CancellationToken cancellationToken)
            {
                var query = _subscriptionRepository.Query()
                    .Include(s => s.SubscriptionTier)
                    .Include(s => s.User);

                // Apply filters
                if (request.UserId.HasValue)
                {
                    query = query.Where(s => s.UserId == request.UserId.Value);
                }

                if (request.TierId.HasValue)
                {
                    query = query.Where(s => s.SubscriptionTierId == request.TierId.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == request.IsActive.Value);
                }

                if (request.IsTrial.HasValue)
                {
                    query = query.Where(s => s.IsTrial == request.IsTrial.Value);
                }

                if (request.StartDateFrom.HasValue)
                {
                    query = query.Where(s => s.StartDate >= request.StartDateFrom.Value);
                }

                if (request.EndDateBefore.HasValue)
                {
                    query = query.Where(s => s.EndDate <= request.EndDateBefore.Value);
                }

                // Apply sorting
                query = request.SortBy switch
                {
                    "EndDate" => request.SortDescending 
                        ? query.OrderByDescending(s => s.EndDate) 
                        : query.OrderBy(s => s.EndDate),
                    "UserId" => request.SortDescending 
                        ? query.OrderByDescending(s => s.UserId) 
                        : query.OrderBy(s => s.UserId),
                    "TierName" => request.SortDescending 
                        ? query.OrderByDescending(s => s.SubscriptionTier.Name) 
                        : query.OrderBy(s => s.SubscriptionTier.Name),
                    _ => request.SortDescending 
                        ? query.OrderByDescending(s => s.StartDate) 
                        : query.OrderBy(s => s.StartDate)
                };

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination
                var subscriptions = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Map to DTOs
                var subscriptionDtos = subscriptions.Select(s => new UserSubscriptionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    SubscriptionTierId = s.SubscriptionTierId,
                    TierName = s.SubscriptionTier?.Name,
                    TierDisplayName = s.SubscriptionTier?.DisplayName,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsActive = s.IsActive,
                    AutoRenew = s.AutoRenew,
                    Status = s.Status ? "Active" : "Inactive",
                    CurrentDailyUsage = s.DailyRequestCount,
                    DailyRequestLimit = s.DailyRequestLimit,
                    CurrentMonthlyUsage = s.MonthlyRequestCount,
                    MonthlyRequestLimit = s.MonthlyRequestLimit,
                    LastUsageResetDate = s.LastDailyReset,
                    MonthlyUsageResetDate = s.LastMonthlyReset,
                    IsTrialSubscription = s.IsTrial,
                    TrialEndDate = s.TrialEndDate
                }).ToList();

                return new SuccessDataResult<List<UserSubscriptionDto>>(
                    subscriptionDtos,
                    $"Retrieved {subscriptionDtos.Count} subscriptions out of {totalCount} total");
            }
        }
    }
}
