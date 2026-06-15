using FluentValidation;

namespace CoreAlign.Application.Mrp;

public class CreatePurchaseRequisitionCommandValidator : AbstractValidator<CreatePurchaseRequisitionCommand>
{
    public CreatePurchaseRequisitionCommandValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.QuantityRequested).GreaterThan(0m);
            line.RuleFor(l => l.EstimatedUnitCost).GreaterThanOrEqualTo(0m);
        });
    }
}

public class ConvertRequisitionToPurchaseOrderCommandValidator : AbstractValidator<ConvertRequisitionToPurchaseOrderCommand>
{
    public ConvertRequisitionToPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class GetStockProjectionQueryValidator : AbstractValidator<GetStockProjectionQuery>
{
    public GetStockProjectionQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.DaysAhead).GreaterThan(0).LessThanOrEqualTo(365);
    }
}

public class GetDemandForecastQueryValidator : AbstractValidator<GetDemandForecastQuery>
{
    public GetDemandForecastQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WindowDays).GreaterThan(0).LessThanOrEqualTo(365);
    }
}
