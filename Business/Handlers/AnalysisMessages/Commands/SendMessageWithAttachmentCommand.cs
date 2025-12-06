using Business.Services.Configuration;
using Business.Services.FileStorage;
using Business.Services.ImageProcessing;
using Business.Services.Messaging;
using Business.Services.Sponsorship;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Constants;
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
    public class SendMessageWithAttachmentCommand : IRequest<IDataResult<AnalysisMessageDto>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; } = "Information";
        public List<IFormFile> Attachments { get; set; }

        public class SendMessageWithAttachmentCommandHandler : IRequestHandler<SendMessageWithAttachmentCommand, IDataResult<AnalysisMessageDto>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IAnalysisMessagingService _messagingService;
            private readonly IAttachmentValidationService _attachmentValidation;
            private readonly IFileStorageService _imageStorage; // FreeImageHost for images
            private readonly LocalFileStorageService _localStorage; // Local for documents
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly DataAccess.Abstract.IUserRepository _userRepository;
            private readonly DataAccess.Abstract.IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IImageProcessingService _imageProcessingService;
            private readonly IConfigurationService _configurationService;
            private readonly ICacheManager _cacheManager;

            public SendMessageWithAttachmentCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IAnalysisMessagingService messagingService,
                IAttachmentValidationService attachmentValidation,
                IFileStorageService imageStorage, // FreeImageHost via DI
                LocalFileStorageService localStorage, // Local for non-images
                IUserGroupRepository userGroupRepository,
                IGroupRepository groupRepository,
                DataAccess.Abstract.IUserRepository userRepository,
                DataAccess.Abstract.IPlantAnalysisRepository plantAnalysisRepository,
                IImageProcessingService imageProcessingService,
                IConfigurationService configurationService,
                ICacheManager cacheManager)
            {
                _messageRepository = messageRepository;
                _messagingService = messagingService;
                _attachmentValidation = attachmentValidation;
                _imageStorage = imageStorage;
                _localStorage = localStorage;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _userRepository = userRepository;
                _plantAnalysisRepository = plantAnalysisRepository;
                _imageProcessingService = imageProcessingService;
                _configurationService = configurationService;
                _cacheManager = cacheManager;
            }

            public async Task<IDataResult<AnalysisMessageDto>> Handle(SendMessageWithAttachmentCommand request, CancellationToken cancellationToken)
            {
                // Get analysis to determine context-based role
                var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == request.PlantAnalysisId);
                if (analysis == null)
                {
                    return new ErrorDataResult<AnalysisMessageDto>("Analysis not found");
                }

                // Determine user's roles
                var userGroups = await _userGroupRepository.GetListAsync(ug => ug.UserId == request.FromUserId);
                var groupIds = userGroups.Select(ug => ug.GroupId).ToList();
                var groups = await _groupRepository.GetListAsync(g => groupIds.Contains(g.Id));
                var hasSponsorRole = groups.Any(g => g.GroupName == "Sponsor");
                var hasFarmerRole = groups.Any(g => g.GroupName == "Farmer");

                // Context-based role detection (same logic as SendMessageCommand)
                bool isActingAsFarmer = (analysis.UserId == request.FromUserId);
                bool isActingAsSponsor = (analysis.SponsorUserId == request.FromUserId) ||
                                        (analysis.DealerId == request.FromUserId);

                // Validate user has the appropriate role for their context
                if (isActingAsFarmer && !hasFarmerRole)
                {
                    return new ErrorDataResult<AnalysisMessageDto>("You must have Farmer role to send messages as the analysis owner");
                }

                if (isActingAsSponsor && !hasSponsorRole)
                {
                    return new ErrorDataResult<AnalysisMessageDto>("You must have Sponsor role to send messages as a sponsor");
                }

                // Role-based validation based on CONTEXT
                if (isActingAsSponsor)
                {
                    // User is acting as SPONSOR for this analysis
                    var canSend = await _messagingService.CanSendMessageForAnalysisAsync(
                        request.FromUserId,
                        request.ToUserId,
                        request.PlantAnalysisId);

                    if (!canSend.canSend)
                        return new ErrorDataResult<AnalysisMessageDto>(canSend.errorMessage);
                }
                else if (isActingAsFarmer)
                {
                    // User is acting as FARMER for this analysis
                    var canReply = await _messagingService.CanFarmerReplyAsync(
                        request.FromUserId,
                        request.ToUserId,
                        request.PlantAnalysisId);

                    if (!canReply.canReply)
                        return new ErrorDataResult<AnalysisMessageDto>(canReply.errorMessage);
                }
                else
                {
                    // User is neither farmer nor sponsor for this analysis
                    return new ErrorDataResult<AnalysisMessageDto>("You are not authorized to send messages for this analysis");
                }

                // Validate attachments based on ANALYSIS tier
                if (request.Attachments == null || !request.Attachments.Any())
                    return new ErrorDataResult<AnalysisMessageDto>("No attachments provided");

                var validationResult = await _attachmentValidation.ValidateAttachmentsAsync(
                    request.Attachments,
                    request.PlantAnalysisId);

                if (!validationResult.Success)
                    return new ErrorDataResult<AnalysisMessageDto>(validationResult.Message);

                // Upload attachments
                var uploadedUrls = new List<string>();
                var attachmentTypes = new List<string>();
                var attachmentSizes = new List<long>();
                var attachmentNames = new List<string>();
                var uploadedFileStorages = new List<bool>(); // Track which storage: true = image, false = local

                try
                {
                    // Get resize configuration
                    var enableResize = await _configurationService.GetBoolValueAsync(
                        ConfigurationKeys.Messaging.EnableAttachmentImageResize, true);

                    var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                        ConfigurationKeys.Messaging.AttachmentImageMaxSizeMB, 1.0m);

                    var maxWidth = await _configurationService.GetIntValueAsync(
                        ConfigurationKeys.Messaging.AttachmentImageMaxWidth, 1920);

                    var maxHeight = await _configurationService.GetIntValueAsync(
                        ConfigurationKeys.Messaging.AttachmentImageMaxHeight, 1080);

                    foreach (var file in request.Attachments)
                    {
                        var fileName = $"msg_attachment_{request.FromUserId}_{DateTime.Now.Ticks}_{file.FileName}";

                        // Select appropriate storage based on MIME type
                        var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                        var folder = isImage ? null : "attachments"; // Organize non-images in subfolder

                        string url;
                        if (isImage)
                        {
                            // Resize image if enabled
                            Stream uploadStream;
                            string contentType = file.ContentType;

                            if (enableResize)
                            {
                                // Read file to byte array
                                using var memoryStream = new MemoryStream();
                                await file.CopyToAsync(memoryStream);
                                var imageBytes = memoryStream.ToArray();

                                // Resize to target size (1 MB default)
                                var resizedBytes = await _imageProcessingService.ResizeToTargetSizeAsync(
                                    imageBytes,
                                    (double)maxSizeMB,
                                    maxWidth,
                                    maxHeight);

                                uploadStream = new MemoryStream(resizedBytes);
                                contentType = "image/jpeg"; // ResizeToTargetSizeAsync may convert to JPEG
                            }
                            else
                            {
                                uploadStream = file.OpenReadStream();
                            }

                            // Use FreeImageHost for images
                            url = await _imageStorage.UploadFileAsync(uploadStream, fileName, contentType);

                            if (enableResize)
                                uploadStream.Dispose();
                        }
                        else
                        {
                            // Use local storage for documents, PDFs, etc.
                            url = await _localStorage.UploadFileAsync(file.OpenReadStream(), fileName, file.ContentType, folder);
                        }

                        if (string.IsNullOrEmpty(url))
                        {
                            // Cleanup uploaded files on failure
                            for (int i = 0; i < uploadedUrls.Count; i++)
                            {
                                try
                                {
                                    if (uploadedFileStorages[i])
                                        await _imageStorage.DeleteFileAsync(uploadedUrls[i]);
                                    else
                                        await _localStorage.DeleteFileAsync(uploadedUrls[i]);
                                }
                                catch
                                {
                                    // Log but don't fail cleanup
                                }
                            }
                            return new ErrorDataResult<AnalysisMessageDto>($"Failed to upload {file.FileName}");
                        }

                        uploadedUrls.Add(url);
                        attachmentTypes.Add(file.ContentType);
                        attachmentSizes.Add(file.Length);
                        attachmentNames.Add(file.FileName);
                        uploadedFileStorages.Add(isImage);
                    }

                    // Create message with attachments
                    var message = new AnalysisMessage
                    {
                        PlantAnalysisId = request.PlantAnalysisId,
                        FromUserId = request.FromUserId,
                        ToUserId = request.ToUserId,
                        Message = request.Message ?? string.Empty,
                        MessageType = request.MessageType,
                        MessageStatus = "Sent",
                        IsRead = false,
                        SentDate = DateTime.Now,
                        CreatedDate = DateTime.Now,

                        // Attachment metadata
                        HasAttachments = true,
                        AttachmentCount = uploadedUrls.Count,
                        AttachmentUrls = JsonSerializer.Serialize(uploadedUrls),
                        AttachmentTypes = JsonSerializer.Serialize(attachmentTypes),
                        AttachmentSizes = JsonSerializer.Serialize(attachmentSizes),
                        AttachmentNames = JsonSerializer.Serialize(attachmentNames)
                    };

                    // ✅ IMPORTANT: Database stores physical URLs (for FilesController to locate files)
                    // Response DTO will contain API endpoint URLs (for secure access)
                    _messageRepository.Add(message);
                    await _messageRepository.SaveChangesAsync();

                    // Generate API endpoint URLs for response (database has physical URLs)
                    var apiAttachmentUrls = new List<string>();
                    var apiThumbnailUrls = new List<string>();
                    var baseUrl = _localStorage.BaseUrl;
                    for (int i = 0; i < uploadedUrls.Count; i++)
                    {
                        apiAttachmentUrls.Add($"{baseUrl}/api/v1/files/attachments/{message.Id}/{i}");
                        // Thumbnail URL - same as full-size for now (FilesController will handle resizing)
                        apiThumbnailUrls.Add($"{baseUrl}/api/v1/files/attachments/{message.Id}/{i}");
                    }

                    // Get sender's avatar URLs
                    var sender = await _userRepository.GetAsync(u => u.UserId == message.FromUserId);

                    // 🔔 Send real-time SignalR notification to recipient
                    var senderRole = isActingAsSponsor ? "Sponsor" : "Farmer";
                    await _messagingService.SendMessageNotificationAsync(
                        message, 
                        senderRole,
                        attachmentUrls: apiAttachmentUrls.ToArray(),
                        attachmentThumbnails: apiThumbnailUrls.ToArray());

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
                        
                        // Attachments - use API endpoint URLs (not physical paths)
                        HasAttachments = message.HasAttachments,
                        AttachmentCount = message.AttachmentCount,
                        AttachmentUrls = apiAttachmentUrls.ToArray(),
                        AttachmentThumbnails = apiThumbnailUrls.ToArray(),
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
                        IsVoiceMessage = false,
                        VoiceMessageUrl = null,
                        VoiceMessageDuration = null,
                        VoiceMessageWaveform = null,
                        
                        // Edit/Delete/Forward
                        IsEdited = message.IsEdited,
                        EditedDate = message.EditedDate,
                        IsForwarded = message.IsForwarded,
                        ForwardedFromMessageId = message.ForwardedFromMessageId,
                        IsActive = true
                    };

                    // Invalidate sponsor messaging analytics cache if message involves sponsor
                    if (analysis.SponsorCompanyId.HasValue)
                    {
                        var sponsorId = analysis.SponsorCompanyId.Value;
                        _cacheManager.RemoveByPattern($"SponsorMessagingAnalytics:{sponsorId}*");
                        System.Console.WriteLine($"[SponsorAnalyticsCache] 🗑️ Invalidated messaging analytics cache for sponsor {sponsorId}");
                    }

                    return new SuccessDataResult<AnalysisMessageDto>(
                        messageDto,
                        $"Message sent with {uploadedUrls.Count} attachment(s)");
                }
                catch (Exception ex)
                {
                    // Cleanup on error
                    for (int i = 0; i < uploadedUrls.Count; i++)
                    {
                        try
                        {
                            if (uploadedFileStorages[i])
                                await _imageStorage.DeleteFileAsync(uploadedUrls[i]);
                            else
                                await _localStorage.DeleteFileAsync(uploadedUrls[i]);
                        }
                        catch
                        {
                            // Log but don't fail
                        }
                    }

                    return new ErrorDataResult<AnalysisMessageDto>($"Failed to send message with attachments: {ex.Message}");
                }
            }
        }
    }
}
