using Business.Handlers.AnalysisMessages.Commands;
using FluentValidation;

namespace Business.Handlers.AnalysisMessages.ValidationRules
{
    public class SendMessageValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageValidator()
        {
            RuleFor(x => x.FromUserId).GreaterThan(0);
            RuleFor(x => x.ToUserId).GreaterThan(0);
            RuleFor(x => x.PlantAnalysisId).GreaterThan(0);
            RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.MessageType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Subject).MaximumLength(200);
            RuleFor(x => x.Priority).MaximumLength(20);
            RuleFor(x => x.Category).MaximumLength(50);
        }
    }
}