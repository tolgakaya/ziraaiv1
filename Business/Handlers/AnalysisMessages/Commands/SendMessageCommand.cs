using Business.Constants;
using Business.Services.Sponsorship;
using Business.Handlers.AnalysisMessages.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Commands
{
    public class SendMessageCommand : IRequest<IDataResult<AnalysisMessageDto>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; } = "Information";
        public string Subject { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Category { get; set; } = "General";

        public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, IDataResult<AnalysisMessageDto>>
        {
            private readonly IAnalysisMessagingService _messagingService;

            public SendMessageCommandHandler(IAnalysisMessagingService messagingService)
            {
                _messagingService = messagingService;
            }

            [ValidationAspect(typeof(SendMessageValidator), Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AnalysisMessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
            {
                // Check if sender can send messages (for sponsors)
                if (!await _messagingService.CanSendMessageAsync(request.FromUserId))
                {
                    return new ErrorDataResult<AnalysisMessageDto>(Messages.MessagingNotAllowed);
                }

                var message = await _messagingService.SendMessageAsync(
                    request.FromUserId,
                    request.ToUserId,
                    request.PlantAnalysisId,
                    request.Message,
                    request.MessageType
                );

                if (message == null)
                    return new ErrorDataResult<AnalysisMessageDto>(Messages.MessageSendFailed);

                var messageDto = new AnalysisMessageDto
                {
                    Id = message.Id,
                    PlantAnalysisId = message.PlantAnalysisId,
                    FromUserId = message.FromUserId,
                    ToUserId = message.ToUserId,
                    Message = message.Message,
                    MessageType = message.MessageType,
                    Subject = message.Subject,
                    IsRead = message.IsRead,
                    SentDate = message.SentDate,
                    ReadDate = message.ReadDate,
                    SenderRole = message.SenderRole,
                    SenderName = message.SenderName,
                    SenderCompany = message.SenderCompany,
                    Priority = message.Priority,
                    Category = message.Category
                };

                return new SuccessDataResult<AnalysisMessageDto>(messageDto, Messages.MessageSent);
            }
        }
    }
}