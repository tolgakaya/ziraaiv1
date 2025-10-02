using Business.Constants;
using Business.Helpers;
using Business.Services.Authentication.Model;
using Core.Entities.Concrete;
using FluentValidation;

namespace Business.Handlers.Authorizations.ValidationRules
{
    public class LoginUserValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserValidator()
        {
            // Phone-based login: Only MobilePhone required
            RuleFor(m => m.MobilePhone)
                .NotEmpty()
                .When(i => i.Provider == AuthenticationProviderType.Phone)
                .WithMessage("Phone number is required for phone-based login");

            // Password validation: Not required for Person and Phone (OTP-based)
            RuleFor(m => m.Password)
                .NotEmpty()
                .When(i => i.Provider != AuthenticationProviderType.Person
                        && i.Provider != AuthenticationProviderType.Phone);

            // ExternalUserId validation: Not required for Phone (uses MobilePhone instead)
            RuleFor(m => m.ExternalUserId)
                .NotEmpty()
                .When(i => i.Provider != AuthenticationProviderType.Phone)
                .Must((instance, value) =>
                {
                    switch (instance.Provider)
                    {
                        case AuthenticationProviderType.Person:
                            return value.IsCidValid();
                        case AuthenticationProviderType.Staff:
                            return true;
                        case AuthenticationProviderType.Agent:
                            break;
                        case AuthenticationProviderType.Phone:
                            return true; // Not validated here, MobilePhone is used
                        case AuthenticationProviderType.Unknown:
                            break;
                        default:
                            break;
                    }

                    return false;
                })
                .WithMessage(Messages.InvalidCode)
                .OverridePropertyName(Messages.CitizenNumber);
        }
    }
}