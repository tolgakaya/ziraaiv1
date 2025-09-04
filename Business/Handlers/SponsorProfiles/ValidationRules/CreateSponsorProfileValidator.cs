using Business.Handlers.SponsorProfiles.Commands;
using FluentValidation;

namespace Business.Handlers.SponsorProfiles.ValidationRules
{
    public class CreateSponsorProfileValidator : AbstractValidator<CreateSponsorProfileCommand>
    {
        public CreateSponsorProfileValidator()
        {
            RuleFor(x => x.SponsorId).GreaterThan(0);
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CompanyDescription).MaximumLength(2000);
            RuleFor(x => x.SponsorLogoUrl).MaximumLength(500);
            RuleFor(x => x.WebsiteUrl).MaximumLength(500);
            RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(100);
            RuleFor(x => x.ContactPhone).MaximumLength(20);
            RuleFor(x => x.ContactPerson).MaximumLength(100);
            // CurrentSubscriptionTierId artık yok - tier bilgisi purchase'larda tutulacak
        }
    }
}