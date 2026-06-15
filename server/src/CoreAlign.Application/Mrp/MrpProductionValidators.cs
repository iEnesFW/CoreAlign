using FluentValidation;

namespace CoreAlign.Application.Mrp;

public class ListPlannedProductionOrdersQueryValidator : AbstractValidator<ListPlannedProductionOrdersQuery>
{
    public ListPlannedProductionOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetProductionPeggingChainQueryValidator : AbstractValidator<GetProductionPeggingChainQuery>
{
    public GetProductionPeggingChainQueryValidator()
    {
        RuleFor(x => x.PlanRunId).NotEmpty();
        RuleFor(x => x.ComponentProductId).NotEmpty();
    }
}

public class GetChangeImpactQueryValidator : AbstractValidator<GetChangeImpactQuery>
{
    public GetChangeImpactQueryValidator()
    {
        RuleFor(x => x.PlanRunId).NotEmpty();
        RuleFor(x => x.SourceOrderLineId).NotEmpty();
    }
}

public class FirmPlannedProductionOrderCommandValidator : AbstractValidator<FirmPlannedProductionOrderCommand>
{
    public FirmPlannedProductionOrderCommandValidator()
    {
        RuleFor(x => x.PlannedProductionOrderId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.OverrideQuantity)
            .GreaterThan(0m)
            .When(x => x.OverrideQuantity.HasValue);
    }
}

public class ReleasePlannedProductionOrderCommandValidator : AbstractValidator<ReleasePlannedProductionOrderCommand>
{
    public ReleasePlannedProductionOrderCommandValidator()
    {
        RuleFor(x => x.PlannedProductionOrderId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty();
    }
}

public class CompletePlannedProductionOrderCommandValidator : AbstractValidator<CompletePlannedProductionOrderCommand>
{
    public CompletePlannedProductionOrderCommandValidator()
    {
        RuleFor(x => x.PlannedProductionOrderId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.WarehouseId)
            .NotEqual(Guid.Empty)
            .When(x => x.WarehouseId.HasValue);
    }
}
