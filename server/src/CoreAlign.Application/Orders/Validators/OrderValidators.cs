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
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");
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
