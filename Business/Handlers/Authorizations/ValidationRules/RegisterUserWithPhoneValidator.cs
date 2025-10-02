using Business.Handlers.Authorizations.Commands;
using FluentValidation;

namespace Business.Handlers.Authorizations.ValidationRules
{
    /// <summary>
    /// Validation rules for phone-based user registration
    /// </summary>
    public class RegisterUserWithPhoneValidator : AbstractValidator<RegisterUserWithPhoneCommand>
    {
        public RegisterUserWithPhoneValidator()
        {
            RuleFor(p => p.MobilePhone)
                .NotEmpty()
                .WithMessage("Phone number is required")
                .Must(BeValidTurkishPhoneNumber)
                .WithMessage("Invalid phone number format. Use Turkish format: 05XX XXX XX XX");

            RuleFor(p => p.FullName)
                .NotEmpty()
                .WithMessage("Full name is required")
                .MinimumLength(3)
                .WithMessage("Full name must be at least 3 characters")
                .MaximumLength(100)
                .WithMessage("Full name cannot exceed 100 characters");

            RuleFor(p => p.UserRole)
                .Must(role => string.IsNullOrEmpty(role) ||
                             role == "Farmer" ||
                             role == "Sponsor" ||
                             role == "Admin")
                .WithMessage("UserRole must be one of: Farmer, Sponsor, Admin (or empty for default Farmer)");
        }

        /// <summary>
        /// Validate Turkish phone number format
        /// Accepts:
        /// - 05XXXXXXXXX (11 digits)
        /// - +905XXXXXXXXX (13 digits with country code)
        /// - 5XXXXXXXXX (10 digits without leading 0)
        /// - With or without spaces/dashes
        /// </summary>
        private bool BeValidTurkishPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Remove all non-digit characters for validation
            var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", string.Empty);

            // Check different valid formats
            // Format 1: 05XXXXXXXXX (11 digits)
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("05"))
                return true;

            // Format 2: +905XXXXXXXXX → 905XXXXXXXXX (12 digits)
            if (digitsOnly.Length == 12 && digitsOnly.StartsWith("905"))
                return true;

            // Format 3: 5XXXXXXXXX (10 digits, will be prefixed with 0)
            if (digitsOnly.Length == 10 && digitsOnly[0] == '5')
                return true;

            return false;
        }
    }
}
