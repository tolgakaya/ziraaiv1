using Business.Services.FileStorage;
using Business.Services.Messaging;
using Business.Services.Sponsorship;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Commands
{
    /// <summary>
    /// Send voice message (XL tier only - premium feature)
    /// </summary>
    public class SendVoiceMessageCommand : IRequest<IDataResult<AnalysisMessageDto>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public IFormFile VoiceFile { get; set; }
        public int Duration { get; set; } // Duration in seconds
        public string Waveform { get; set; } // Optional waveform JSON data

        public class SendVoiceMessageCommandHandler : IRequestHandler<SendVoiceMessageCommand, IDataResult<AnalysisMessageDto>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IAnalysisMessagingService _messagingService;
            private readonly IMessagingFeatureService _featureService;
            private readonly LocalFileStorageService _localFileStorage;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly DataAccess.Abstract.IUserRepository _userRepository;

            public SendVoiceMessageCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IAnalysisMessagingService messagingService,
                IMessagingFeatureService featureService,
                LocalFileStorageService localFileStorage,
                IUserGroupRepository userGroupRepository,
                IGroupRepository groupRepository,
                DataAccess.Abstract.IUserRepository userRepository)
            {
                _messageRepository = messageRepository;
                _messagingService = messagingService;
                _featureService = featureService;
                _localFileStorage = localFileStorage;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _userRepository = userRepository;
            }

            public async Task<IDataResult<AnalysisMessageDto>> Handle(SendVoiceMessageCommand request, CancellationToken cancellationToken)
            {
                // Determine user role
                var userGroups = await _userGroupRepository.GetListAsync(ug => ug.UserId == request.FromUserId);
                var groupIds = userGroups.Select(ug => ug.GroupId).ToList();
                var groups = await _groupRepository.GetListAsync(g => groupIds.Contains(g.Id));
                var isSponsor = groups.Any(g => g.GroupName == "Sponsor");
                var isFarmer = groups.Any(g => g.GroupName == "Farmer");

                // Role-based validation
                if (isSponsor)
                {
                    // Sponsor sending to farmer
                    var canSend = await _messagingService.CanSendMessageForAnalysisAsync(
                        request.FromUserId,
                        request.ToUserId,
                        request.PlantAnalysisId);

                    if (!canSend.canSend)
                        return new ErrorDataResult<AnalysisMessageDto>(canSend.errorMessage);
                }
                else if (isFarmer)
                {
                    // Farmer replying to sponsor
                    var canReply = await _messagingService.CanFarmerReplyAsync(
                        request.FromUserId,
                        request.ToUserId,
                        request.PlantAnalysisId);

                    if (!canReply.canReply)
                        return new ErrorDataResult<AnalysisMessageDto>(canReply.errorMessage);
                }
                else
                {
                    return new ErrorDataResult<AnalysisMessageDto>("Only sponsors and farmers can send messages");
                }

                // Validate voice message feature access based on ANALYSIS tier
                var featureValidation = await _featureService.ValidateFeatureAccessAsync(
                    "VoiceMessages",
                    request.PlantAnalysisId,
                    request.VoiceFile?.Length,
                    request.Duration);

                if (!featureValidation.Success)
                    return new ErrorDataResult<AnalysisMessageDto>(featureValidation.Message);

                // Validate voice file
                if (request.VoiceFile == null || request.VoiceFile.Length == 0)
                    return new ErrorDataResult<AnalysisMessageDto>("Voice file is required");

                try
                {
                    // Upload voice file to local storage (FreeImageHost doesn't support audio files)
                    var extension = Path.GetExtension(request.VoiceFile.FileName).ToLowerInvariant();
                    var fileName = $"voice_msg_{request.FromUserId}_{DateTime.Now.Ticks}{extension}";

                    // Upload file and get physical storage URL
                    var physicalUrl = await _localFileStorage.UploadFileAsync(
                        request.VoiceFile.OpenReadStream(),
                        fileName,
                        request.VoiceFile.ContentType,
                        "voice-messages"); // Store in voice-messages subfolder

                    if (string.IsNullOrEmpty(physicalUrl))
                        return new ErrorDataResult<AnalysisMessageDto>("Failed to upload voice message");

                    // ✅ IMPORTANT: Database stores physical URL for FilesController to locate file
                    // Response DTO will contain API endpoint URL for secure access
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

                        // Store physical URL in database (FilesController extracts path from this)
                        VoiceMessageUrl = physicalUrl,
                        VoiceMessageDuration = request.Duration,
                        VoiceMessageWaveform = request.Waveform
                    };

                    _messageRepository.Add(message);
                    await _messageRepository.SaveChangesAsync();

                    // Get sender's avatar URLs
                    var sender = await _userRepository.GetAsync(u => u.UserId == message.FromUserId);

                    // Generate API endpoint URL for response (database has physical URL)
                    var baseUrl = _localFileStorage.BaseUrl;
                    var apiVoiceUrl = $"{baseUrl}/api/v1/files/voice-messages/{message.Id}";

                    var messageDto = new AnalysisMessageDto
                    {
                        Id = message.Id,
                        PlantAnalysisId = message.PlantAnalysisId,
                        FromUserId = message.FromUserId,
                        ToUserId = message.ToUserId,
                        Message = message.Message,
                        MessageType = message.MessageType,
                        Subject = message.Subject,
                        
                        // Status fields
                        MessageStatus = message.MessageStatus ?? "Sent",
                        IsRead = message.IsRead,
                        SentDate = message.SentDate,
                        DeliveredDate = message.DeliveredDate,
                        ReadDate = message.ReadDate,
                        
                        // Sender info
                        SenderRole = message.SenderRole,
                        SenderName = message.SenderName,
                        SenderCompany = message.SenderCompany,
                        
                        // Avatar URLs
                        SenderAvatarUrl = sender?.AvatarUrl,
                        SenderAvatarThumbnailUrl = sender?.AvatarThumbnailUrl,
                        
                        // Classification
                        Priority = message.Priority,
                        Category = message.Category,
                        
                        // Attachments
                        HasAttachments = false,
                        AttachmentCount = 0,
                        AttachmentUrls = null,
                        AttachmentTypes = null,
                        AttachmentSizes = null,
                        AttachmentNames = null,
                        
                        // Voice Messages (API endpoint URL, not physical path)
                        IsVoiceMessage = true,
                        VoiceMessageUrl = apiVoiceUrl,
                        VoiceMessageDuration = message.VoiceMessageDuration,
                        VoiceMessageWaveform = message.VoiceMessageWaveform,
                        
                        // Edit/Delete/Forward
                        IsEdited = message.IsEdited,
                        EditedDate = message.EditedDate,
                        IsForwarded = message.IsForwarded,
                        ForwardedFromMessageId = message.ForwardedFromMessageId,
                        IsActive = true
                    };

                    return new SuccessDataResult<AnalysisMessageDto>(
                        messageDto,
                        $"Voice message sent ({request.Duration}s)");
                }
                catch (Exception ex)
                {
                    return new ErrorDataResult<AnalysisMessageDto>($"Failed to send voice message: {ex.Message}");
                }
            }
        }
    }
}
