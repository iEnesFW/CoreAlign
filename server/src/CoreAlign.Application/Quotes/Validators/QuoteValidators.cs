using CoreAlign.Application.Quotes.Commands;
using FluentValidation;

namespace CoreAlign.Application.Quotes.Validators;

public class QuoteLineInputValidator : AbstractValidator<QuoteLineInput>
{
    public QuoteLineInputValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Validation.Positive");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.LineDiscountPercent)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.PercentRange");
        RuleFor(x => x.LineDiscountAmount)
            .GreaterThanOrEqualTo(0m).WithMessage("Validation.NonNegative");
        RuleFor(x => x.TaxRatePercent)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.PercentRange");
        RuleFor(x => x.WithholdingRatePercent)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.PercentRange");
        RuleFor(x => x.UomConversionFactor)
            .GreaterThan(0m).WithMessage("Validation.Positive");
        RuleFor(x => x.LineNotes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.LineNotes));
        RuleFor(x => x.UomCode)
            .MaximumLength(16).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.UomCode));
    }
}

public class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.QuoteNumber)
            .MaximumLength(64).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.QuoteNumber));
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.QuoteDate).NotEmpty();
        RuleFor(x => x.ValidUntilUtc)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.QuoteDate)
            .WithMessage("Validation.ValidUntilAfterQuoteDate");
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .Matches("^[A-Z]{3}$").WithMessage("Validation.CurrencyFormat");
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.AtLeastOneLine");
        RuleForEach(x => x.Lines).SetValidator(new QuoteLineInputValidator());
    }
}

public class UpdateQuoteCommandValidator : AbstractValidator<UpdateQuoteCommand>
{
    public UpdateQuoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.QuoteNumber)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.QuoteDate).NotEmpty();
        RuleFor(x => x.ValidUntilUtc)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.QuoteDate)
            .WithMessage("Validation.ValidUntilAfterQuoteDate");
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .Matches("^[A-Z]{3}$").WithMessage("Validation.CurrencyFormat");
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.AtLeastOneLine");
        RuleForEach(x => x.Lines).SetValidator(new QuoteLineInputValidator());
    }
}
