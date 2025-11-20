using Business.Handlers.AnalysisMessages.ValidationRules;
using Business.Hubs;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading;
using System.Threading.Tasks;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.AnalysisMessages.Commands
{
    public class MarkMessageAsReadCommand : IRequest<IResult>
    {
        public int MessageId { get; set; }
        public int UserId { get; set; }

        public class MarkMessageAsReadCommandHandler : IRequestHandler<MarkMessageAsReadCommand, IResult>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IHubContext<PlantAnalysisHub> _hubContext;
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;

            public MarkMessageAsReadCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IHubContext<PlantAnalysisHub> hubContext,
                IPlantAnalysisRepository plantAnalysisRepository)
            {
                _messageRepository = messageRepository;
                _hubContext = hubContext;
                _plantAnalysisRepository = plantAnalysisRepository;
            }

            public async Task<IResult> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
            {
                var message = await _messageRepository.GetAsync(m => m.Id == request.MessageId);

                if (message == null)
                    return new ErrorResult("Message not found");

                // Verify the user is the recipient
                if (message.ToUserId != request.UserId)
                    return new ErrorResult("You can only mark messages sent to you as read");

                // AUTHORIZATION CHECK: Verify user has access to this analysis
                // Get the analysis to check attribution
                var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == message.PlantAnalysisId);
                if (analysis == null)
                    return new ErrorResult("Analysis not found");

                // Check if user has permission to access this analysis's messages
                // - Farmer: UserId matches analysis.UserId
                // - Sponsor/Dealer: SponsorUserId or DealerId matches
                bool hasAccess = (analysis.UserId == request.UserId) ||  // Farmer
                                 (analysis.SponsorUserId == request.UserId) ||  // Main Sponsor
                                 (analysis.DealerId == request.UserId);  // Dealer

                if (!hasAccess)
                    return new ErrorResult("You don't have access to this analysis");

                // Only update if not already read
                if (!message.IsRead)
                {
                    // ✅ FIX: Use repository method that includes SaveChanges
                    await _messageRepository.MarkAsReadAsync(request.MessageId);

                    // Send SignalR notification to sender
                    await _hubContext.Clients.User(message.FromUserId.ToString())
                        .SendAsync("MessageRead", new
                        {
                            MessageId = message.Id,
                            ReadByUserId = request.UserId,
                            ReadAt = DateTime.Now
                        });
                }

                return new SuccessResult("Message marked as read");
            }
        }
    }
}
