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
    /// Edit message within time limit (M tier and above, 1 hour limit)
    /// </summary>
    public class EditMessageCommand : IRequest<IResult>
    {
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string NewMessage { get; set; }

        public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, IResult>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IMessagingFeatureService _featureService;

            public EditMessageCommandHandler(
                IAnalysisMessageRepository messageRepository,
                IMessagingFeatureService featureService)
            {
                _messageRepository = messageRepository;
                _featureService = featureService;
            }

            public async Task<IResult> Handle(EditMessageCommand request, CancellationToken cancellationToken)
            {
                var message = await _messageRepository.GetAsync(m => m.Id == request.MessageId);

                if (message == null)
                    return new ErrorResult("Message not found");

                // Verify ownership
                if (message.FromUserId != request.UserId)
                    return new ErrorResult("You can only edit your own messages");

                // Check if already deleted
                if (message.IsDeleted)
                    return new ErrorResult("Cannot edit deleted message");

                // Validate feature access (M tier and above)
                var featureCheck = await _featureService.ValidateFeatureAccessAsync(
                    "MessageEdit",
                    request.UserId);

                if (!featureCheck.Success)
                    return new ErrorResult(featureCheck.Message);

                // Check time limit (1 hour = 3600 seconds)
                var timeSinceSent = (DateTime.Now - message.SentDate).TotalSeconds;
                var feature = await _featureService.GetFeatureAsync("MessageEdit");

                if (feature.Success && feature.Data?.TimeLimit.HasValue == true)
                {
                    if (timeSinceSent > feature.Data.TimeLimit.Value)
                    {
                        return new ErrorResult($"Edit time limit exceeded ({feature.Data.TimeLimit.Value / 3600} hour)");
                    }
                }

                // Store original message if not already edited
                if (!message.IsEdited)
                {
                    message.OriginalMessage = message.Message;
                }

                // Update message
                message.Message = request.NewMessage;
                message.IsEdited = true;
                message.EditedDate = DateTime.Now;

                _messageRepository.Update(message);

                return new SuccessResult("Message edited successfully");
            }
        }
    }
}
