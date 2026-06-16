using FluentValidation;

namespace CoreAlign.Application.Purchasing;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0m);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l.TaxRatePercent).InclusiveBetween(0m, 100m);
        });
    }
}

public class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Lines).NotEmpty();
    }
}

public class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0m);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l.TaxRatePercent).InclusiveBetween(0m, 100m);
        });
    }
}
