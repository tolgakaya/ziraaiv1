using Business.Handlers.AnalysisMessages.ValidationRules;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
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

            public MarkMessageAsReadCommandHandler(IAnalysisMessageRepository messageRepository)
            {
                _messageRepository = messageRepository;
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
                    message.IsRead = true;
                    message.ReadDate = DateTime.Now;
                    message.MessageStatus = "Read";

                    _messageRepository.Update(message);
                }

                return new SuccessResult("Message marked as read");
            }
        }
    }
}
