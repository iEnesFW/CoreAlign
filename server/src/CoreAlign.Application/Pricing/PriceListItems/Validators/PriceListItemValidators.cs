using CoreAlign.Application.Pricing.PriceListItems.Commands;
using FluentValidation;

namespace CoreAlign.Application.Pricing.PriceListItems.Validators;

public class AddPriceListItemCommandValidator : AbstractValidator<AddPriceListItemCommand>
{
    public AddPriceListItemCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m).WithMessage("Validation.PriceMustBeNonNegative");
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0m, 100m).When(x => x.DiscountPercent.HasValue)
            .WithMessage("Validation.DiscountPercentRange");
        RuleFor(x => x.MinQuantity).GreaterThanOrEqualTo(0m).When(x => x.MinQuantity.HasValue);
        RuleFor(x => x.MaxQuantity).GreaterThanOrEqualTo(0m).When(x => x.MaxQuantity.HasValue);
        RuleFor(x => x).Must(c => !(c.MinQuantity.HasValue && c.MaxQuantity.HasValue) || c.MinQuantity!.Value <= c.MaxQuantity!.Value)
            .WithMessage("Validation.MinQuantityMustBeLessThanMaxQuantity");
    }
}

public class UpdatePriceListItemCommandValidator : AbstractValidator<UpdatePriceListItemCommand>
{
    public UpdatePriceListItemCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m).WithMessage("Validation.PriceMustBeNonNegative");
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0m, 100m).When(x => x.DiscountPercent.HasValue)
            .WithMessage("Validation.DiscountPercentRange");
        RuleFor(x => x.MinQuantity).GreaterThanOrEqualTo(0m).When(x => x.MinQuantity.HasValue);
        RuleFor(x => x.MaxQuantity).GreaterThanOrEqualTo(0m).When(x => x.MaxQuantity.HasValue);
        RuleFor(x => x).Must(c => !(c.MinQuantity.HasValue && c.MaxQuantity.HasValue) || c.MinQuantity!.Value <= c.MaxQuantity!.Value)
            .WithMessage("Validation.MinQuantityMustBeLessThanMaxQuantity");
    }
}

public class RemovePriceListItemCommandValidator : AbstractValidator<RemovePriceListItemCommand>
{
    public RemovePriceListItemCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
