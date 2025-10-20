using Business.Services.Messaging;
using Business.Services.Sponsorship;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Commands
{
    /// <summary>
    /// Forward message to another conversation (M tier and above)
    /// </summary>
    public class ForwardMessageCommand : IRequest<IDataResult<AnalysisMessage>>
    {
        public int MessageId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }

        public class ForwardMessageCommandHandler : IRequestHandler<ForwardMessageCommand, IDataResult<AnalysisMessage>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IAnalysisMessagingService _messagingService;
            private readonly IMessagingFeatureService _featureService;

            public ForwardMessageCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IAnalysisMessagingService messagingService,
                IMessagingFeatureService featureService)
            {
                _messageRepository = messageRepository;
                _messagingService = messagingService;
                _featureService = featureService;
            }

            public async Task<IDataResult<AnalysisMessage>> Handle(ForwardMessageCommand request, CancellationToken cancellationToken)
            {
                // Get original message
                var originalMessage = await _messageRepository.GetAsync(m => m.Id == request.MessageId);

                if (originalMessage == null)
                    return new ErrorDataResult<AnalysisMessage>("Message not found");

                if (originalMessage.IsDeleted)
                    return new ErrorDataResult<AnalysisMessage>("Cannot forward deleted message");

                // Validate messaging permission
                var canSend = await _messagingService.CanSendMessageForAnalysisAsync(
                    request.FromUserId,
                    request.ToUserId,
                    request.PlantAnalysisId);

                if (!canSend.canSend)
                    return new ErrorDataResult<AnalysisMessage>(canSend.errorMessage);

                // Validate feature access (M tier and above)
                var featureCheck = await _featureService.ValidateFeatureAccessAsync(
                    "MessageForward",
                    request.FromUserId);

                if (!featureCheck.Success)
                    return new ErrorDataResult<AnalysisMessage>(featureCheck.Message);

                // Create forwarded message
                var forwardedMessage = new AnalysisMessage
                {
                    PlantAnalysisId = request.PlantAnalysisId,
                    FromUserId = request.FromUserId,
                    ToUserId = request.ToUserId,
                    Message = originalMessage.Message,
                    MessageType = originalMessage.MessageType,
                    MessageStatus = "Sent",
                    IsRead = false,
                    SentDate = DateTime.Now,
                    CreatedDate = DateTime.Now,

                    // Forward metadata
                    IsForwarded = true,
                    ForwardedFromMessageId = originalMessage.Id,

                    // Copy attachments if present
                    HasAttachments = originalMessage.HasAttachments,
                    AttachmentCount = originalMessage.AttachmentCount,
                    AttachmentUrls = originalMessage.AttachmentUrls,
                    AttachmentTypes = originalMessage.AttachmentTypes,
                    AttachmentSizes = originalMessage.AttachmentSizes,
                    AttachmentNames = originalMessage.AttachmentNames,

                    // Copy voice message if present
                    VoiceMessageUrl = originalMessage.VoiceMessageUrl,
                    VoiceMessageDuration = originalMessage.VoiceMessageDuration,
                    VoiceMessageWaveform = originalMessage.VoiceMessageWaveform
                };

                _messageRepository.Add(forwardedMessage);

                return new SuccessDataResult<AnalysisMessage>(
                    forwardedMessage,
                    "Message forwarded successfully");
            }
        }
    }
}
