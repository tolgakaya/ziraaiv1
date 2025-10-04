using Business.Handlers.Referrals.Commands;
using FluentValidation;
using System.Linq;

namespace Business.Handlers.Referrals.ValidationRules
{
    public class GenerateReferralLinkValidator : AbstractValidator<GenerateReferralLinkCommand>
    {
        public GenerateReferralLinkValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be valid");

            RuleFor(x => x.DeliveryMethod)
                .InclusiveBetween(1, 3)
                .WithMessage("Delivery method must be 1 (SMS), 2 (WhatsApp), or 3 (Both)");

            RuleFor(x => x.PhoneNumbers)
                .NotNull()
                .WithMessage("Phone numbers list is required")
                .NotEmpty()
                .WithMessage("At least one phone number is required")
                .Must(numbers => numbers != null && numbers.Count <= 50)
                .WithMessage("Maximum 50 phone numbers allowed per request");

            RuleForEach(x => x.PhoneNumbers)
                .NotEmpty()
                .WithMessage("Phone number cannot be empty")
                .Length(10, 11)
                .WithMessage("Phone number must be 10-11 digits (Turkish format: 05321234567)")
                .Matches(@"^0[0-9]{9,10}$")
                .WithMessage("Phone number must start with 0 and contain only digits");

            RuleFor(x => x.CustomMessage)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.CustomMessage))
                .WithMessage("Custom message cannot exceed 500 characters");
        }
    }
}
