using Business.Handlers.SponsorProfiles.Commands;
using FluentValidation;

namespace Business.Handlers.SponsorProfiles.ValidationRules
{
    public class UpdateSponsorProfileValidator : AbstractValidator<UpdateSponsorProfileCommand>
    {
        public UpdateSponsorProfileValidator()
        {
            RuleFor(x => x.SponsorId).GreaterThan(0);
            
            // All fields are optional, but if provided, must meet requirements
            RuleFor(x => x.CompanyName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.CompanyName));
            
            RuleFor(x => x.CompanyDescription)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.CompanyDescription));
            
            RuleFor(x => x.SponsorLogoUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.SponsorLogoUrl));
            
            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl));
            
            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
            
            RuleFor(x => x.ContactPhone)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
            
            RuleFor(x => x.ContactPerson)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.ContactPerson));
            
            RuleFor(x => x.CompanyType)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.CompanyType));
            
            RuleFor(x => x.BusinessModel)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.BusinessModel));
            
            // Social Media Links
            RuleFor(x => x.LinkedInUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl));
            
            RuleFor(x => x.TwitterUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.TwitterUrl));
            
            RuleFor(x => x.FacebookUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.FacebookUrl));
            
            RuleFor(x => x.InstagramUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.InstagramUrl));
            
            // Business Information
            RuleFor(x => x.TaxNumber)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));
            
            RuleFor(x => x.TradeRegistryNumber)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.TradeRegistryNumber));
            
            RuleFor(x => x.Address)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Address));
            
            RuleFor(x => x.City)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.City));
            
            RuleFor(x => x.Country)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Country));
            
            RuleFor(x => x.PostalCode)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
            
            // Password
            RuleFor(x => x.Password)
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Password));
        }
    }
}
