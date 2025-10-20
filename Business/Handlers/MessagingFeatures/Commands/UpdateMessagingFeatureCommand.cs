using Business.Services.Messaging;
using Core.Utilities.Results;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.MessagingFeatures.Commands
{
    /// <summary>
    /// Admin command to toggle messaging features on/off
    /// </summary>
    public class UpdateMessagingFeatureCommand : IRequest<IResult>
    {
        public int FeatureId { get; set; }
        public bool IsEnabled { get; set; }
        public int AdminUserId { get; set; }

        public class UpdateMessagingFeatureCommandHandler : IRequestHandler<UpdateMessagingFeatureCommand, IResult>
        {
            private readonly IMessagingFeatureService _messagingFeatureService;

            public UpdateMessagingFeatureCommandHandler(IMessagingFeatureService messagingFeatureService)
            {
                _messagingFeatureService = messagingFeatureService;
            }

            public async Task<IResult> Handle(UpdateMessagingFeatureCommand request, CancellationToken cancellationToken)
            {
                return await _messagingFeatureService.UpdateFeatureAsync(
                    request.FeatureId,
                    request.IsEnabled,
                    request.AdminUserId
                );
            }
        }
    }
}
