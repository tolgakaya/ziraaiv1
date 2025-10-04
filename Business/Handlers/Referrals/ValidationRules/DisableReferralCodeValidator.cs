using Business.Handlers.Referrals.Commands;
using FluentValidation;

namespace Business.Handlers.Referrals.ValidationRules
{
    public class DisableReferralCodeValidator : AbstractValidator<DisableReferralCodeCommand>
    {
        public DisableReferralCodeValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be valid");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Referral code is required")
                .Length(11, 11)
                .WithMessage("Referral code must be in ZIRA-XXXXXX format")
                .Matches(@"^ZIRA-[A-Z0-9]{6}$")
                .WithMessage("Invalid referral code format. Expected: ZIRA-XXXXXX");
        }
    }
}
