using Business.Handlers.AnalysisMessages.Commands;
using FluentValidation;

namespace Business.Handlers.AnalysisMessages.ValidationRules
{
    public class SendMessageValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageValidator()
        {
            RuleFor(x => x.FromUserId)
                .GreaterThan(0)
                .WithMessage("FromUserId must be greater than 0");
            
            RuleFor(x => x.PlantAnalysisId)
                .GreaterThan(0)
                .WithMessage("PlantAnalysisId must be greater than 0");
            
            // Custom validator for ToUserId or FarmerId - avoid using Must with complex expressions
            RuleFor(x => x.ToUserId)
                .Must((command, toUserId) => 
                {
                    if (command == null) return false;
                    return (toUserId.HasValue && toUserId.Value > 0) || 
                           (command.FarmerId.HasValue && command.FarmerId.Value > 0);
                })
                .WithMessage("Either ToUserId or FarmerId must be provided and greater than 0");
            
            // Custom validator for message content - avoid using Must with complex expressions  
            RuleFor(x => x.Message)
                .Must((command, message) => 
                {
                    if (command == null) return false;
                    return !string.IsNullOrEmpty(message) || !string.IsNullOrEmpty(command.MessageContent);
                })
                .WithMessage("Either Message or MessageContent must be provided");
            
            // Individual field validations
            When(x => x != null && !string.IsNullOrEmpty(x.Message), () => {
                RuleFor(x => x.Message).MaximumLength(4000);
            });
            
            When(x => x != null && !string.IsNullOrEmpty(x.MessageContent), () => {
                RuleFor(x => x.MessageContent).MaximumLength(4000);
            });
            
            When(x => x != null, () => {
                RuleFor(x => x.MessageType).NotEmpty().MaximumLength(50);
            });
            
            When(x => x != null && !string.IsNullOrEmpty(x.Subject), () => {
                RuleFor(x => x.Subject).MaximumLength(200);
            });
            
            When(x => x != null && !string.IsNullOrEmpty(x.Priority), () => {
                RuleFor(x => x.Priority).MaximumLength(20);
            });
            
            When(x => x != null && !string.IsNullOrEmpty(x.Category), () => {
                RuleFor(x => x.Category).MaximumLength(50);
            });
        }
    }
}