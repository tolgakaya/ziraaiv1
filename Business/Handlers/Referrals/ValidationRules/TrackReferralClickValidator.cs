using Business.Handlers.Referrals.Commands;
using FluentValidation;

namespace Business.Handlers.Referrals.ValidationRules
{
    public class TrackReferralClickValidator : AbstractValidator<TrackReferralClickCommand>
    {
        public TrackReferralClickValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Referral code is required")
                .Length(11, 11)
                .WithMessage("Referral code must be in ZIRA-XXXXXX format")
                .Matches(@"^ZIRA-[A-Z0-9]{6}$")
                .WithMessage("Invalid referral code format. Expected: ZIRA-XXXXXX");

            RuleFor(x => x.IpAddress)
                .NotEmpty()
                .WithMessage("IP address is required")
                .Matches(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$")
                .When(x => !string.IsNullOrEmpty(x.IpAddress))
                .WithMessage("Invalid IP address format");

            RuleFor(x => x.DeviceId)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.DeviceId))
                .WithMessage("Device ID cannot exceed 255 characters");
        }
    }
}
