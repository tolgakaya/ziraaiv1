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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Commands
{
    public class SendMessageCommand : IRequest<IDataResult<AnalysisMessageDto>>
    {
        public int FromUserId { get; set; }
        
        // Support both field names for backward compatibility
        public int? ToUserId { get; set; }
        public int? FarmerId { get; set; } // Alternative field name
        
        public int PlantAnalysisId { get; set; }
        
        // Support both field names for backward compatibility
        public string Message { get; set; }
        public string MessageContent { get; set; } // Alternative field name
        
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
                try
                {
                    // Normalize field names - support both naming conventions
                    var toUserId = request.ToUserId ?? request.FarmerId ?? 0;
                    var messageContent = !string.IsNullOrEmpty(request.Message) ? request.Message : request.MessageContent;

                    // Comprehensive validation for sponsors (tier, ownership, rate limit, block check)
                    var (canSend, errorMessage) = await _messagingService.CanSendMessageForAnalysisAsync(
                        request.FromUserId,
                        toUserId,
                        request.PlantAnalysisId);

                    if (!canSend)
                    {
                        return new ErrorDataResult<AnalysisMessageDto>(errorMessage);
                    }

                    var message = await _messagingService.SendMessageAsync(
                        request.FromUserId,
                        toUserId,
                        request.PlantAnalysisId,
                        messageContent,
                        request.MessageType
                    );

                    if (message == null)
                    {
                        return new ErrorDataResult<AnalysisMessageDto>(Messages.MessageSendFailed);
                    }

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
                catch (Exception ex)
                {
                    return new ErrorDataResult<AnalysisMessageDto>($"Error sending message: {ex.Message}");
                }
            }
        }
    }
}