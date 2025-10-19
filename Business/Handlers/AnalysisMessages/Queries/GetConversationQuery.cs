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
    public class GetConversationQuery : IRequest<IDataResult<List<AnalysisMessageDto>>>
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }

        public class GetConversationQueryHandler : IRequestHandler<GetConversationQuery, IDataResult<List<AnalysisMessageDto>>>
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
            public async Task<IDataResult<List<AnalysisMessageDto>>> Handle(GetConversationQuery request, CancellationToken cancellationToken)
            {
                var messages = await _messagingService.GetConversationAsync(request.FromUserId, request.ToUserId, request.PlantAnalysisId);

                var messageDtos = new List<AnalysisMessageDto>();

                foreach (var m in messages)
                {
                    // Get sender's avatar URLs
                    var sender = await _userRepository.GetAsync(u => u.UserId == m.FromUserId);

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
                        
                        // Avatar URLs (Phase 1A)
                        SenderAvatarUrl = sender?.AvatarUrl,
                        SenderAvatarThumbnailUrl = sender?.AvatarThumbnailUrl,
                        
                        // Classification
                        Priority = m.Priority,
                        Category = m.Category
                    });
                }

                return new SuccessDataResult<List<AnalysisMessageDto>>(messageDtos);
            }
        }
    }
}