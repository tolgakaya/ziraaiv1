using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AnalysisMessages.Queries
{
    public class GetConversationQuery : IRequest<PaginatedResult<List<AnalysisMessageDto>>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        
        // Pagination parameters
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public class GetConversationQueryHandler : IRequestHandler<GetConversationQuery, PaginatedResult<List<AnalysisMessageDto>>>
        {
            private readonly IAnalysisMessagingService _messagingService;
            private readonly DataAccess.Abstract.IUserRepository _userRepository;

            public GetConversationQueryHandler(
                IAnalysisMessagingService messagingService,
                DataAccess.Abstract.IUserRepository userRepository)
            {
                _messagingService = messagingService;
                _userRepository = userRepository;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<PaginatedResult<List<AnalysisMessageDto>>> Handle(GetConversationQuery request, CancellationToken cancellationToken)
            {
                var allMessages = await _messagingService.GetConversationAsync(request.FromUserId, request.ToUserId, request.PlantAnalysisId);
                
                // Calculate pagination
                var totalRecords = allMessages.Count;
                var totalPages = (int)System.Math.Ceiling((double)totalRecords / request.PageSize);
                
                // Apply pagination (skip and take)
                var messages = allMessages
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var messageDtos = new List<AnalysisMessageDto>();

                foreach (var m in messages)
                {
                    // Get sender's and receiver's user info for avatars
                    var sender = await _userRepository.GetAsync(u => u.UserId == m.FromUserId);
                    var receiver = await _userRepository.GetAsync(u => u.UserId == m.ToUserId);

                    messageDtos.Add(new AnalysisMessageDto
                    {
                        Id = m.Id,
                        PlantAnalysisId = m.PlantAnalysisId,
                        FromUserId = m.FromUserId,
                        ToUserId = m.ToUserId,
                        Message = m.Message,
                        MessageType = m.MessageType,
                        Subject = m.Subject,

                        // Status fields (Phase 1B)
                        MessageStatus = m.MessageStatus ?? "Sent",
                        IsRead = m.IsRead,
                        SentDate = m.SentDate,
                        DeliveredDate = m.DeliveredDate,
                        ReadDate = m.ReadDate,

                        // Sender info
                        SenderRole = m.SenderRole,
                        SenderName = m.SenderName,
                        SenderCompany = m.SenderCompany,

                        // Sender Avatar (Phase 1A)
                        SenderAvatarUrl = sender?.AvatarUrl,
                        SenderAvatarThumbnailUrl = sender?.AvatarThumbnailUrl,

                        // Receiver info (for displaying both participants in chat UI)
                        ReceiverRole = "", // Role info not available in User entity directly
                        ReceiverName = receiver?.FullName ?? "",
                        ReceiverCompany = "", // Company info not available in User entity directly

                        // Receiver Avatar
                        ReceiverAvatarUrl = receiver?.AvatarUrl,
                        ReceiverAvatarThumbnailUrl = receiver?.AvatarThumbnailUrl,
                        
                        // Classification
                        Priority = m.Priority,
                        Category = m.Category,
                        
                        // Attachments (Phase 2A)
                        HasAttachments = m.HasAttachments,
                        AttachmentCount = m.AttachmentCount,
                        AttachmentUrls = !string.IsNullOrEmpty(m.AttachmentUrls) 
                            ? System.Text.Json.JsonSerializer.Deserialize<string[]>(m.AttachmentUrls) 
                            : null,
                        AttachmentTypes = !string.IsNullOrEmpty(m.AttachmentTypes)
                            ? System.Text.Json.JsonSerializer.Deserialize<string[]>(m.AttachmentTypes)
                            : null,
                        AttachmentSizes = !string.IsNullOrEmpty(m.AttachmentSizes)
                            ? System.Text.Json.JsonSerializer.Deserialize<long[]>(m.AttachmentSizes)
                            : null,
                        AttachmentNames = !string.IsNullOrEmpty(m.AttachmentNames)
                            ? System.Text.Json.JsonSerializer.Deserialize<string[]>(m.AttachmentNames)
                            : null,
                        
                        // Voice Messages (Phase 2B)
                        IsVoiceMessage = !string.IsNullOrEmpty(m.VoiceMessageUrl),
                        VoiceMessageUrl = m.VoiceMessageUrl,
                        VoiceMessageDuration = m.VoiceMessageDuration,
                        VoiceMessageWaveform = m.VoiceMessageWaveform,
                        
                        // Edit/Delete/Forward (Phase 4)
                        IsEdited = m.IsEdited,
                        EditedDate = m.EditedDate,
                        IsForwarded = m.IsForwarded,
                        ForwardedFromMessageId = m.ForwardedFromMessageId,
                        IsActive = !m.IsDeleted // Assuming IsDeleted field exists, or use true as default
                    });
                }

                return new PaginatedResult<List<AnalysisMessageDto>>(messageDtos, request.Page, request.PageSize)
                {
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                };
            }
        }
    }
}