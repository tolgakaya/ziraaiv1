using Business.Services.FileStorage;
using Business.Services.Messaging;
using Business.Services.Sponsorship;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Commands
{
    /// <summary>
    /// Send voice message (XL tier only - premium feature)
    /// </summary>
    public class SendVoiceMessageCommand : IRequest<IDataResult<AnalysisMessage>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public IFormFile VoiceFile { get; set; }
        public int Duration { get; set; } // Duration in seconds
        public string Waveform { get; set; } // Optional waveform JSON data

        public class SendVoiceMessageCommandHandler : IRequestHandler<SendVoiceMessageCommand, IDataResult<AnalysisMessage>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IAnalysisMessagingService _messagingService;
            private readonly IMessagingFeatureService _featureService;
            private readonly IFileStorageService _fileStorage;

            public SendVoiceMessageCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IAnalysisMessagingService messagingService,
                IMessagingFeatureService featureService,
                IFileStorageService fileStorage)
            {
                _messageRepository = messageRepository;
                _messagingService = messagingService;
                _featureService = featureService;
                _fileStorage = fileStorage;
            }

            public async Task<IDataResult<AnalysisMessage>> Handle(SendVoiceMessageCommand request, CancellationToken cancellationToken)
            {
                // Validate messaging permission
                var canSend = await _messagingService.CanSendMessageForAnalysisAsync(
                    request.FromUserId,
                    request.ToUserId,
                    request.PlantAnalysisId);

                if (!canSend.canSend)
                    return new ErrorDataResult<AnalysisMessage>(canSend.errorMessage);

                // Validate voice message feature access (XL tier only)
                var featureValidation = await _featureService.ValidateFeatureAccessAsync(
                    "VoiceMessages",
                    request.FromUserId,
                    request.VoiceFile?.Length,
                    request.Duration);

                if (!featureValidation.Success)
                    return new ErrorDataResult<AnalysisMessage>(featureValidation.Message);

                // Validate voice file
                if (request.VoiceFile == null || request.VoiceFile.Length == 0)
                    return new ErrorDataResult<AnalysisMessage>("Voice file is required");

                try
                {
                    // Upload voice file
                    var fileName = $"voice_msg_{request.FromUserId}_{DateTime.Now.Ticks}.m4a";
                    var voiceUrl = await _fileStorage.UploadFileAsync(
                        request.VoiceFile.OpenReadStream(),
                        fileName,
                        request.VoiceFile.ContentType);

                    if (string.IsNullOrEmpty(voiceUrl))
                        return new ErrorDataResult<AnalysisMessage>("Failed to upload voice message");

                    // Create message with voice
                    var message = new AnalysisMessage
                    {
                        PlantAnalysisId = request.PlantAnalysisId,
                        FromUserId = request.FromUserId,
                        ToUserId = request.ToUserId,
                        Message = "[Voice Message]",
                        MessageType = "VoiceMessage",
                        MessageStatus = "Sent",
                        IsRead = false,
                        SentDate = DateTime.Now,
                        CreatedDate = DateTime.Now,

                        // Voice message data
                        VoiceMessageUrl = voiceUrl,
                        VoiceMessageDuration = request.Duration,
                        VoiceMessageWaveform = request.Waveform
                    };

                    _messageRepository.Add(message);

                    return new SuccessDataResult<AnalysisMessage>(
                        message,
                        $"Voice message sent ({request.Duration}s)");
                }
                catch (Exception ex)
                {
                    return new ErrorDataResult<AnalysisMessage>($"Failed to send voice message: {ex.Message}");
                }
            }
        }
    }
}
