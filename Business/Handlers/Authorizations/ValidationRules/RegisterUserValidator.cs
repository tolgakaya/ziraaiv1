using Business.Handlers.Authorizations.Commands;
using FluentValidation;

namespace Business.Handlers.Authorizations.ValidationRules
{
    public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserValidator()
        {
            RuleFor(p => p.Password).Password();
            
            RuleFor(p => p.UserRole)
                .Must(role => string.IsNullOrEmpty(role) || 
                             role == "Farmer" || 
                             role == "Sponsor" || 
                             role == "Admin")
                .WithMessage("UserRole must be one of: Farmer, Sponsor, Admin (or empty for default Farmer)");
        }
    }
}