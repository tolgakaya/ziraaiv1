using Business.Services.Messaging;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.MessagingFeatures.Queries
{
    /// <summary>
    /// Get messaging features configuration for the current user
    /// Returns feature availability based on user tier and admin toggles
    /// </summary>
    public class GetMessagingFeaturesQuery : IRequest<IDataResult<MessagingFeaturesDto>>
    {
        public int UserId { get; set; }

        public class GetMessagingFeaturesQueryHandler : IRequestHandler<GetMessagingFeaturesQuery, IDataResult<MessagingFeaturesDto>>
        {
            private readonly IMessagingFeatureService _messagingFeatureService;

            public GetMessagingFeaturesQueryHandler(IMessagingFeatureService messagingFeatureService)
            {
                _messagingFeatureService = messagingFeatureService;
            }

            public async Task<IDataResult<MessagingFeaturesDto>> Handle(GetMessagingFeaturesQuery request, CancellationToken cancellationToken)
            {
                return await _messagingFeatureService.GetUserFeaturesAsync(request.UserId);
            }
        }
    }
}
