using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Handlers.AnalysisMessages.Commands;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Commands
{
    /// <summary>
    /// Admin command to send message on behalf of a sponsor
    /// </summary>
    public class SendMessageAsSponsorCommand : IRequest<IDataResult<AnalysisMessageDto>>
    {
        public int SponsorId { get; set; }
        public int FarmerUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; } = "Information";
        public string Subject { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Category { get; set; } = "General";

        public class SendMessageAsSponsorCommandHandler : IRequestHandler<SendMessageAsSponsorCommand, IDataResult<AnalysisMessageDto>>
        {
            private readonly IMediator _mediator;

            public SendMessageAsSponsorCommandHandler(IMediator mediator)
            {
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AnalysisMessageDto>> Handle(SendMessageAsSponsorCommand request, CancellationToken cancellationToken)
            {
                // Reuse existing SendMessageCommand logic
                // Admin sends message impersonating sponsor (FromUserId = SponsorId)
                var sendMessageCommand = new SendMessageCommand
                {
                    FromUserId = request.SponsorId,
                    ToUserId = request.FarmerUserId,
                    PlantAnalysisId = request.PlantAnalysisId,
                    Message = request.Message,
                    MessageType = request.MessageType,
                    Subject = request.Subject,
                    Priority = request.Priority,
                    Category = request.Category
                };

                var result = await _mediator.Send(sendMessageCommand, cancellationToken);

                if (!result.Success)
                {
                    return new ErrorDataResult<AnalysisMessageDto>(result.Message);
                }

                return new SuccessDataResult<AnalysisMessageDto>(
                    result.Data,
                    $"Admin sent message on behalf of sponsor {request.SponsorId}"
                );
            }
        }
    }
}
