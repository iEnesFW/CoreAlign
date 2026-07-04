using CoreAlign.Application.Invoices.Commands;
using FluentValidation;

namespace CoreAlign.Application.Invoices.Validators;

public class GenerateInvoiceFromOrderCommandValidator : AbstractValidator<GenerateInvoiceFromOrderCommand>
{
    public GenerateInvoiceFromOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DueDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class StandaloneInvoiceLineInputValidator : AbstractValidator<StandaloneInvoiceLineInput>
{
    public StandaloneInvoiceLineInputValidator()
    {
        RuleFor(x => x.ProductSku).NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");
        RuleFor(x => x.ProductName).NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(200).WithMessage("Validation.TooLong");
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Quantity).GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m).WithMessage("Validation.NonNegative");
        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0m, 100m).WithMessage("Validation.OutOfRange");
        RuleFor(x => x.LineDiscountPercent!.Value)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.OutOfRange")
            .When(x => x.LineDiscountPercent.HasValue);
        RuleFor(x => x.LineDiscountAmount!.Value)
            .GreaterThanOrEqualTo(0m).WithMessage("Validation.NonNegative")
            .When(x => x.LineDiscountAmount.HasValue);
        RuleFor(x => x.WithholdingRatePercent!.Value)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.OutOfRange")
            .When(x => x.WithholdingRatePercent.HasValue);
    }
}

public class CreateStandaloneInvoiceCommandValidator : AbstractValidator<CreateStandaloneInvoiceCommand>
{
    public CreateStandaloneInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.InvalidFormat");
        RuleFor(x => x.DueDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.Required");
        RuleForEach(x => x.Lines).SetValidator(new StandaloneInvoiceLineInputValidator());
        RuleFor(x => x.HeaderDiscountPercent!.Value)
            .InclusiveBetween(0m, 100m).WithMessage("Validation.OutOfRange")
            .When(x => x.HeaderDiscountPercent.HasValue);
        RuleFor(x => x.VatExemptionReason)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.VatExemptionReason));
        RuleFor(x => x.InternalNotes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.InternalNotes));
        RuleFor(x => x.PublicNotes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.PublicNotes));
    }
}
