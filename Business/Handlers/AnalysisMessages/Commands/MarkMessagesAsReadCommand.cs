using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.AnalysisMessages.Commands
{
    /// <summary>
    /// Bulk mark multiple messages as read (for conversation view)
    /// </summary>
    public class MarkMessagesAsReadCommand : IRequest<IDataResult<int>>
    {
        public List<int> MessageIds { get; set; }
        public int UserId { get; set; }

        public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, IDataResult<int>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;

            public MarkMessagesAsReadCommandHandler(IAnalysisMessageRepository messageRepository)
            {
                _messageRepository = messageRepository;
            }

            public async Task<IDataResult<int>> Handle(MarkMessagesAsReadCommand request, CancellationToken cancellationToken)
            {
                if (request.MessageIds == null || !request.MessageIds.Any())
                    return new ErrorDataResult<int>(0, "No message IDs provided");

                var messages = await _messageRepository.GetListAsync(m =>
                    request.MessageIds.Contains(m.Id) &&
                    m.ToUserId == request.UserId &&
                    !m.IsRead);

                if (messages == null || !messages.Any())
                    return new SuccessDataResult<int>(0, "No unread messages found");

                int markedCount = 0;
                var now = DateTime.Now;

                foreach (var message in messages)
                {
                    message.IsRead = true;
                    message.ReadDate = now;
                    message.MessageStatus = "Read";
                    message.UpdatedDate = now;
                    _messageRepository.Update(message);
                    markedCount++;
                }

                // ✅ FIX: Save changes to database
                await _messageRepository.SaveChangesAsync();

                return new SuccessDataResult<int>(markedCount, $"{markedCount} message(s) marked as read");
            }
        }
    }
}
