using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Manufacturing;

public class PlannedProductionOrder : TenantEntity
{
    public Guid SourcePlanRunId { get; private set; }
    public Guid ProductId { get; private set; }
    public int LowLevelCode { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public DateTime ReleaseDateUtc { get; private set; }
    public decimal EstimatedUnitCost { get; private set; }
    public LotSizingPolicy SourcePolicy { get; private set; }
    public Guid? PeggingParentProductId { get; private set; }
    public Guid? PeggingSourceOrderLineId { get; private set; }
    public PlannedProductionOrderStatus Status { get; private set; } = PlannedProductionOrderStatus.Planned;
    public decimal? OriginalQuantity { get; private set; }
    public DateTime? OriginalDueDateUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? ProducedWarehouseId { get; private set; }

    public bool IsQuantityOverridden => OriginalQuantity is not null;
    public bool IsDueDateOverridden => OriginalDueDateUtc is not null;
    public bool IsCompleted => Status == PlannedProductionOrderStatus.Closed;

    protected PlannedProductionOrder() { }

    public PlannedProductionOrder(
        Guid sourcePlanRunId,
        Guid productId,
        int lowLevelCode,
        decimal quantity,
        DateTime dueDateUtc,
        DateTime releaseDateUtc,
        decimal estimatedUnitCost,
        LotSizingPolicy sourcePolicy,
        Guid? peggingParentProductId,
        Guid? peggingSourceOrderLineId)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Planned production order quantity must be positive.");
        }

        SourcePlanRunId = sourcePlanRunId;
        ProductId = productId;
        LowLevelCode = lowLevelCode;
        Quantity = quantity;
        DueDateUtc = DateTime.SpecifyKind(dueDateUtc, DateTimeKind.Utc);
        ReleaseDateUtc = DateTime.SpecifyKind(releaseDateUtc, DateTimeKind.Utc);
        EstimatedUnitCost = estimatedUnitCost;
        SourcePolicy = sourcePolicy;
        PeggingParentProductId = peggingParentProductId;
        PeggingSourceOrderLineId = peggingSourceOrderLineId;
        Status = PlannedProductionOrderStatus.Planned;
    }

    public void Firm(decimal? overrideQuantity, DateTime? overrideDueDateUtc)
    {
        EnsureTransitionAllowed(PlannedProductionOrderStatus.Firm);

        if (overrideQuantity is not null && overrideQuantity.Value != Quantity)
        {
            if (overrideQuantity.Value <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(overrideQuantity), "Override quantity must be positive.");
            }
            OriginalQuantity ??= Quantity;
            Quantity = overrideQuantity.Value;
        }
        if (overrideDueDateUtc is not null)
        {
            var due = DateTime.SpecifyKind(overrideDueDateUtc.Value, DateTimeKind.Utc);
            if (due != DueDateUtc)
            {
                OriginalDueDateUtc ??= DueDateUtc;
                var leadOffset = DueDateUtc - ReleaseDateUtc;
                DueDateUtc = due;
                ReleaseDateUtc = due - leadOffset;
            }
        }

        Status = PlannedProductionOrderStatus.Firm;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public PlannedProductionOrder CloneFirmForRun(Guid newPlanRunId)
    {
        var clone = new PlannedProductionOrder(
            newPlanRunId,
            ProductId,
            LowLevelCode,
            Quantity,
            DueDateUtc,
            ReleaseDateUtc,
            EstimatedUnitCost,
            SourcePolicy,
            PeggingParentProductId,
            PeggingSourceOrderLineId)
        {
            Status = PlannedProductionOrderStatus.Firm,
            OriginalQuantity = OriginalQuantity,
            OriginalDueDateUtc = OriginalDueDateUtc
        };
        return clone;
    }

    public void Release()
    {
        EnsureTransitionAllowed(PlannedProductionOrderStatus.Released);
        Status = PlannedProductionOrderStatus.Released;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Close()
    {
        EnsureTransitionAllowed(PlannedProductionOrderStatus.Closed);
        Status = PlannedProductionOrderStatus.Closed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete(Guid producedWarehouseId)
    {
        if (Status != PlannedProductionOrderStatus.Released)
        {
            throw new InvalidPlannedProductionOrderTransitionException(
                Status.ToString(), PlannedProductionOrderStatus.Closed.ToString());
        }

        var now = DateTime.UtcNow;
        Status = PlannedProductionOrderStatus.Closed;
        CompletedAtUtc = now;
        ProducedWarehouseId = producedWarehouseId;
        UpdatedAtUtc = now;
    }

    public bool IsTransitionAllowed(PlannedProductionOrderStatus target) =>
        Status switch
        {
            PlannedProductionOrderStatus.Planned => target is PlannedProductionOrderStatus.Firm or PlannedProductionOrderStatus.Released or PlannedProductionOrderStatus.Closed,
            PlannedProductionOrderStatus.Firm => target is PlannedProductionOrderStatus.Released or PlannedProductionOrderStatus.Closed,
            PlannedProductionOrderStatus.Released => target is PlannedProductionOrderStatus.Closed,
            _ => false
        };

    private void EnsureTransitionAllowed(PlannedProductionOrderStatus target)
    {
        if (!IsTransitionAllowed(target))
        {
            throw new InvalidPlannedProductionOrderTransitionException(Status.ToString(), target.ToString());
        }
    }
}
