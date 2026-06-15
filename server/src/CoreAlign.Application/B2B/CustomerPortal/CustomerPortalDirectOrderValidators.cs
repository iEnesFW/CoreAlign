using FluentValidation;

namespace CoreAlign.Application.B2B.CustomerPortal;

public class CreateCustomerDirectOrderCommandValidator : AbstractValidator<CreateCustomerDirectOrderCommand>
{
    public CreateCustomerDirectOrderCommandValidator()
    {
        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(l => l!.Count <= 500)
            .WithMessage("A direct order may not contain more than 500 lines.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEqual(Guid.Empty);
            line.RuleFor(l => l.Quantity).GreaterThan(0m);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.CustomerNotes).MaximumLength(2000);
        RuleFor(x => x.ShippingAddressId)
            .NotEqual(Guid.Empty)
            .When(x => x.ShippingAddressId.HasValue);
        RuleFor(x => x.BillingAddressId)
            .NotEqual(Guid.Empty)
            .When(x => x.BillingAddressId.HasValue);
    }
}
