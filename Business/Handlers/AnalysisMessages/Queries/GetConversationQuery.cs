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

            public GetConversationQueryHandler(IAnalysisMessagingService messagingService)
            {
                _messagingService = messagingService;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<AnalysisMessageDto>>> Handle(GetConversationQuery request, CancellationToken cancellationToken)
            {
                var messages = await _messagingService.GetConversationAsync(request.FromUserId, request.ToUserId, request.PlantAnalysisId);

                var messageDtos = messages.Select(m => new AnalysisMessageDto
                {
                    Id = m.Id,
                    PlantAnalysisId = m.PlantAnalysisId,
                    FromUserId = m.FromUserId,
                    ToUserId = m.ToUserId,
                    Message = m.Message,
                    MessageType = m.MessageType,
                    Subject = m.Subject,
                    IsRead = m.IsRead,
                    SentDate = m.SentDate,
                    ReadDate = m.ReadDate,
                    SenderRole = m.SenderRole,
                    SenderName = m.SenderName,
                    SenderCompany = m.SenderCompany,
                    Priority = m.Priority,
                    Category = m.Category
                }).ToList();

                return new SuccessDataResult<List<AnalysisMessageDto>>(messageDtos);
            }
        }
    }
}