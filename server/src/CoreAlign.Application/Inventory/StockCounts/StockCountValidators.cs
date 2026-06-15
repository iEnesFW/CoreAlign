using FluentValidation;

namespace CoreAlign.Application.Inventory.StockCounts;

public class PlanStockCountCommandValidator : AbstractValidator<PlanStockCountCommand>
{
    public PlanStockCountCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.CountNumber).MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class RecordCountCommandValidator : AbstractValidator<RecordCountCommand>
{
    public RecordCountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.LineId).NotEmpty();
            l.RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0m);
            l.RuleFor(x => x.LineNotes).MaximumLength(500);
        });
    }
}

public class ReconcileStockCountCommandValidator : AbstractValidator<ReconcileStockCountCommand>
{
    public ReconcileStockCountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
