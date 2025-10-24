using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSubscriptions.Queries
{
    /// <summary>
    /// Admin query to get subscription by ID with full details
    /// </summary>
    public class GetSubscriptionByIdQuery : IRequest<IDataResult<UserSubscription>>
    {
        public int SubscriptionId { get; set; }

        public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, IDataResult<UserSubscription>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;

            public GetSubscriptionByIdQueryHandler(IUserSubscriptionRepository subscriptionRepository)
            {
                _subscriptionRepository = subscriptionRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserSubscription>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
            {
                var subscription = await _subscriptionRepository.Query()
                    .Include(s => s.SubscriptionTier)
                    .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

                if (subscription == null)
                {
                    return new ErrorDataResult<UserSubscription>("Subscription not found");
                }

                return new SuccessDataResult<UserSubscription>(subscription, "Subscription retrieved successfully");
            }
        }
    }
}
