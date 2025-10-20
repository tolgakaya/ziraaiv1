using Business.Services.Messaging;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.AnalysisMessages.Commands
{
    /// <summary>
    /// Delete message within time limit (All tiers, 24 hour limit)
    /// </summary>
    public class DeleteMessageCommand : IRequest<IResult>
    {
        public int MessageId { get; set; }
        public int UserId { get; set; }

        public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, IResult>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IMessagingFeatureService _featureService;

            public DeleteMessageCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IMessagingFeatureService featureService)
            {
                _messageRepository = messageRepository;
                _featureService = featureService;
            }

            public async Task<IResult> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
            {
                var message = await _messageRepository.GetAsync(m => m.Id == request.MessageId);

                if (message == null)
                    return new ErrorResult("Message not found");

                // Verify ownership
                if (message.FromUserId != request.UserId)
                    return new ErrorResult("You can only delete your own messages");

                // Check if already deleted
                if (message.IsDeleted)
                    return new ErrorResult("Message already deleted");

                // Validate feature access (All tiers)
                var featureCheck = await _featureService.ValidateFeatureAccessAsync(
                    "MessageDelete",
                    request.UserId);

                if (!featureCheck.Success)
                    return new ErrorResult(featureCheck.Message);

                // Check time limit (24 hours = 86400 seconds)
                var timeSinceSent = (DateTime.Now - message.SentDate).TotalSeconds;
                var feature = await _featureService.GetFeatureAsync("MessageDelete");

                if (feature.Success && feature.Data?.TimeLimit.HasValue == true)
                {
                    if (timeSinceSent > feature.Data.TimeLimit.Value)
                    {
                        return new ErrorResult($"Delete time limit exceeded ({feature.Data.TimeLimit.Value / 3600} hours)");
                    }
                }

                // Soft delete
                message.IsDeleted = true;
                message.DeletedDate = DateTime.Now;

                _messageRepository.Update(message);

                return new SuccessResult("Message deleted successfully");
            }
        }
    }
}
