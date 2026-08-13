using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Treasury.Fx;
using FluentValidation;

namespace CoreAlign.Application.Customers.Validators;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.Code).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.LegalName).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.LegalName));
        RuleFor(x => x.TradeName).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.TradeName));
        RuleFor(x => x.NationalId).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.NationalId));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Validation.InvalidEmail")
            .MaximumLength(256).WithMessage("Validation.EmailTooLong")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TaxNumber));
        RuleFor(x => x.TaxOffice).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.TaxOffice));
        RuleFor(x => x.Website).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.DefaultCurrency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .MustBeAKnownCurrency(currencyGuard);

        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.DefaultDiscountPercent)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.PercentRange");
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.LegalName).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.LegalName));
        RuleFor(x => x.TradeName).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.TradeName));
        RuleFor(x => x.NationalId).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.NationalId));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Validation.InvalidEmail")
            .MaximumLength(256).WithMessage("Validation.EmailTooLong")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TaxNumber));
        RuleFor(x => x.TaxOffice).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.TaxOffice));
        RuleFor(x => x.Website).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.DefaultCurrency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .MustBeAKnownCurrency(currencyGuard);

        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.DefaultDiscountPercent)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.PercentRange");
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
