using Business.Handlers.SmartLinks.Commands;
using FluentValidation;

namespace Business.Handlers.SmartLinks.ValidationRules
{
    public class CreateSmartLinkValidator : AbstractValidator<CreateSmartLinkCommand>
    {
        public CreateSmartLinkValidator()
        {
            RuleFor(x => x.SponsorId).GreaterThan(0);
            RuleFor(x => x.LinkUrl).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.LinkText).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LinkDescription).MaximumLength(1000);
            RuleFor(x => x.LinkType).MaximumLength(50);
            RuleFor(x => x.ProductCategory).MaximumLength(100);
            RuleFor(x => x.Priority).InclusiveBetween(1, 100);
            RuleFor(x => x.DisplayPosition).MaximumLength(50);
            RuleFor(x => x.DisplayStyle).MaximumLength(50);
            RuleFor(x => x.ProductName).MaximumLength(200);
            RuleFor(x => x.ProductCurrency).MaximumLength(10);
            RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100).When(x => x.DiscountPercentage.HasValue);
        }
    }
}