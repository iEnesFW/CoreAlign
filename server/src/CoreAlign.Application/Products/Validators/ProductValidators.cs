using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Application.Products.Commands;
using FluentValidation;

namespace CoreAlign.Application.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(20).WithMessage("Validation.TooLong")
            .Must(unit => GibUnitCodeMap.TryResolve(unit, out _)).WithMessage("Validation.UnitNotRecognized");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .Matches("^[A-Z]{3}$").WithMessage("Validation.CurrencyFormat")
            .MustBeAKnownCurrency(currencyGuard);
    }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(20).WithMessage("Validation.TooLong")
            .Must(unit => GibUnitCodeMap.TryResolve(unit, out _)).WithMessage("Validation.UnitNotRecognized");
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .Matches("^[A-Z]{3}$").WithMessage("Validation.CurrencyFormat")
            .MustBeAKnownCurrency(currencyGuard);
    }
}
