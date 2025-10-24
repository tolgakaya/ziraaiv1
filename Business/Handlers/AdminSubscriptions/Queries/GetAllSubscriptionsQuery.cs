using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSubscriptions.Queries
{
    /// <summary>
    /// Admin query to get all user subscriptions with pagination and filtering
    /// </summary>
    public class GetAllSubscriptionsQuery : IRequest<IDataResult<IEnumerable<UserSubscription>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Status { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsSponsoredSubscription { get; set; }

        public class GetAllSubscriptionsQueryHandler : IRequestHandler<GetAllSubscriptionsQuery, IDataResult<IEnumerable<UserSubscription>>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;

            public GetAllSubscriptionsQueryHandler(IUserSubscriptionRepository subscriptionRepository)
            {
                _subscriptionRepository = subscriptionRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<UserSubscription>>> Handle(GetAllSubscriptionsQuery request, CancellationToken cancellationToken)
            {
                var query = _subscriptionRepository.Query()
                    .Include(s => s.SubscriptionTier)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(s => s.Status == request.Status);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == request.IsActive.Value);
                }

                if (request.IsSponsoredSubscription.HasValue)
                {
                    query = query.Where(s => s.IsSponsoredSubscription == request.IsSponsoredSubscription.Value);
                }

                var subscriptions = await query
                    .OrderByDescending(s => s.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new SuccessDataResult<IEnumerable<UserSubscription>>(subscriptions, "Subscriptions retrieved successfully");
            }
        }
    }
}
