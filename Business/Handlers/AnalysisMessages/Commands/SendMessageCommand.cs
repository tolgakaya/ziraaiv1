using Business.Constants;
using Business.Services.Sponsorship;
using System.Text.Json;
using Business.Handlers.AnalysisMessages.ValidationRules;
using DataAccess.Abstract;
using System.Linq;
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
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly DataAccess.Abstract.IUserRepository _userRepository;

            public SendMessageCommandHandler(
                IAnalysisMessagingService messagingService,
                IUserGroupRepository userGroupRepository,
                IGroupRepository groupRepository,
                DataAccess.Abstract.IUserRepository userRepository)
            {
                _messagingService = messagingService;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _userRepository = userRepository;
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

                    // Determine user role
                    var userGroups = await _userGroupRepository.GetListAsync(ug => ug.UserId == request.FromUserId);
                    var groupIds = userGroups.Select(ug => ug.GroupId).ToList();
                    var groups = await _groupRepository.GetListAsync(g => groupIds.Contains(g.Id));
                    var isSponsor = groups.Any(g => g.GroupName == "Sponsor");
                    var isFarmer = groups.Any(g => g.GroupName == "Farmer");

                    // Role-based validation
                    if (isSponsor)
                    {
                        // Comprehensive validation for sponsors (tier, ownership, rate limit, block check)
                        var (canSend, errorMessage) = await _messagingService.CanSendMessageForAnalysisAsync(
                            request.FromUserId,
                            toUserId,
                            request.PlantAnalysisId);

                        if (!canSend)
                        {
                            return new ErrorDataResult<AnalysisMessageDto>(errorMessage);
                        }
                    }
                    else if (isFarmer)
                    {
                        // Farmers can only reply to existing sponsor messages
                        var (canReply, errorMessage) = await _messagingService.CanFarmerReplyAsync(
                            request.FromUserId,
                            toUserId,
                            request.PlantAnalysisId);

                        if (!canReply)
                        {
                            return new ErrorDataResult<AnalysisMessageDto>(errorMessage);
                        }
                    }
                    else
                    {
                        return new ErrorDataResult<AnalysisMessageDto>("Only sponsors and farmers can send messages");
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

                    // Get sender's avatar URLs
                    var sender = await _userRepository.GetAsync(u => u.UserId == message.FromUserId);

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
                        HasAttachments = message.HasAttachments,
                        AttachmentCount = message.AttachmentCount,
                        AttachmentUrls = !string.IsNullOrEmpty(message.AttachmentUrls)
                            ? JsonSerializer.Deserialize<string[]>(message.AttachmentUrls)
                            : null,
                        AttachmentTypes = !string.IsNullOrEmpty(message.AttachmentTypes)
                            ? JsonSerializer.Deserialize<string[]>(message.AttachmentTypes)
                            : null,
                        AttachmentSizes = !string.IsNullOrEmpty(message.AttachmentSizes)
                            ? JsonSerializer.Deserialize<long[]>(message.AttachmentSizes)
                            : null,
                        AttachmentNames = !string.IsNullOrEmpty(message.AttachmentNames)
                            ? JsonSerializer.Deserialize<string[]>(message.AttachmentNames)
                            : null,
                        
                        // Voice Messages
                        IsVoiceMessage = !string.IsNullOrEmpty(message.VoiceMessageUrl),
                        VoiceMessageUrl = message.VoiceMessageUrl,
                        VoiceMessageDuration = message.VoiceMessageDuration,
                        VoiceMessageWaveform = message.VoiceMessageWaveform,
                        
                        // Edit/Delete/Forward
                        IsEdited = message.IsEdited,
                        EditedDate = message.EditedDate,
                        IsForwarded = message.IsForwarded,
                        ForwardedFromMessageId = message.ForwardedFromMessageId,
                        IsActive = true // New message is always active
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