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

            public MarkMessageAsReadCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IHubContext<PlantAnalysisHub> hubContext)
            {
                _messageRepository = messageRepository;
                _hubContext = hubContext;
            }

            public async Task<IResult> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
            {
                var message = await _messageRepository.GetAsync(m => m.Id == request.MessageId);

                if (message == null)
                    return new ErrorResult("Message not found");

                // Verify the user is the recipient
                if (message.ToUserId != request.UserId)
                    return new ErrorResult("You can only mark messages sent to you as read");

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
