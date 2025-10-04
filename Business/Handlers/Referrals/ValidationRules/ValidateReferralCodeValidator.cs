using Business.Handlers.Referrals.Queries;
using FluentValidation;

namespace Business.Handlers.Referrals.ValidationRules
{
    public class ValidateReferralCodeValidator : AbstractValidator<ValidateReferralCodeQuery>
    {
        public ValidateReferralCodeValidator()
        {
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
