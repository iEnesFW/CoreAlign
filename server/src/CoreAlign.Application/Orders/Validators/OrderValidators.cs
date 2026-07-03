using CoreAlign.Application.Orders.Commands;
using FluentValidation;

namespace CoreAlign.Application.Orders.Validators;

public class OrderLineInputValidator : AbstractValidator<OrderLineInput>
{
    public OrderLineInputValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Validation.Positive");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");

        // Percent values are stored as numeric(6,3) — keep them in [0, 100].
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

        // Free-text fields must fit DB columns and avoid payload-bomb DoS.
        RuleFor(x => x.LineNotes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.LineNotes));
        RuleFor(x => x.UomCode)
            .MaximumLength(16).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.UomCode));
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .MaximumLength(64).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.OrderNumber));
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .Matches("^[A-Z]{3}$").WithMessage("Validation.CurrencyFormat");
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.AtLeastOneLine");
        RuleForEach(x => x.Lines).SetValidator(new OrderLineInputValidator());
    }
}

public class RevertOrderToDraftCommandValidator : AbstractValidator<RevertOrderToDraftCommand>
{
    public RevertOrderToDraftCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyLength")
            .Matches("^[A-Z]{3}$").WithMessage("Validation.CurrencyFormat");
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.AtLeastOneLine");
        RuleForEach(x => x.Lines).SetValidator(new OrderLineInputValidator());
    }
}
