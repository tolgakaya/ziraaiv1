using Business.Handlers.Farmers.Commands;
using FluentValidation;
using System;

namespace Business.Handlers.Farmers.ValidationRules
{
    public class UpdateFarmerProfileValidator : AbstractValidator<UpdateFarmerProfileCommand>
    {
        public UpdateFarmerProfileValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Ad Soyad boş olamaz.")
                .MinimumLength(2).WithMessage("Ad Soyad en az 2 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Ad Soyad en fazla 100 karakter olabilir.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
                .MaximumLength(100).WithMessage("E-posta adresi en fazla 100 karakter olabilir.");

            RuleFor(x => x.MobilePhones)
                .NotEmpty().WithMessage("Telefon numarası boş olamaz.")
                .Matches(@"^[0-9\s\-\+\(\)]+$").WithMessage("Geçerli bir telefon numarası giriniz.")
                .MinimumLength(10).WithMessage("Telefon numarası en az 10 karakter olmalıdır.")
                .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olabilir.");

            RuleFor(x => x.BirthDate)
                .Must(BeAValidBirthDate).WithMessage("Doğum tarihi geçerli bir tarih olmalıdır ve gelecekte olamaz.")
                .When(x => x.BirthDate.HasValue);

            RuleFor(x => x.Gender)
                .InclusiveBetween(0, 2).WithMessage("Cinsiyet değeri 0 (Belirtilmemiş), 1 (Erkek) veya 2 (Kadın) olmalıdır.")
                .When(x => x.Gender.HasValue);

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir.")
                .When(x => !string.IsNullOrEmpty(x.Address));

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notlar en fazla 1000 karakter olabilir.")
                .When(x => !string.IsNullOrEmpty(x.Notes));
        }

        private bool BeAValidBirthDate(DateTime? birthDate)
        {
            if (!birthDate.HasValue)
                return true;

            // Must be in the past
            if (birthDate.Value >= DateTime.Now)
                return false;

            // Must be reasonable (not more than 150 years ago)
            if (birthDate.Value < DateTime.Now.AddYears(-150))
                return false;

            return true;
        }
    }
}
