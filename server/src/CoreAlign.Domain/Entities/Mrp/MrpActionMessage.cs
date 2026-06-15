using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Mrp;

public class MrpActionMessage : TenantEntity
{
    public Guid PlanRunId { get; private set; }
    public Guid ProductId { get; private set; }
    public MrpActionType ActionType { get; private set; }
    public MrpActionSeverity Severity { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime? CurrentDateUtc { get; private set; }
    public DateTime? SuggestedDateUtc { get; private set; }
    public Guid? RelatedPurchaseOrderId { get; private set; }
    public Guid? RelatedPlannedOrderId { get; private set; }
    public int DaysUntilStockOut { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public bool IsDismissed { get; private set; }
    public Guid? DismissedByUserId { get; private set; }
    public DateTime? DismissedAtUtc { get; private set; }

    public MrpPlanRun? PlanRun { get; private set; }

    protected MrpActionMessage() { }

    public MrpActionMessage(
        Guid productId,
        MrpActionType actionType,
        MrpActionSeverity severity,
        decimal quantity,
        DateTime? currentDateUtc,
        DateTime? suggestedDateUtc,
        Guid? relatedPurchaseOrderId,
        Guid? relatedPlannedOrderId,
        int daysUntilStockOut,
        string message)
    {
        ProductId = productId;
        ActionType = actionType;
        Severity = severity;
        Quantity = quantity;
        CurrentDateUtc = NormalizeUtc(currentDateUtc);
        SuggestedDateUtc = NormalizeUtc(suggestedDateUtc);
        RelatedPurchaseOrderId = relatedPurchaseOrderId;
        RelatedPlannedOrderId = relatedPlannedOrderId;
        DaysUntilStockOut = daysUntilStockOut;
        Message = message ?? string.Empty;
    }

    public void Dismiss(Guid? userId)
    {
        if (IsDismissed)
        {
            return;
        }
        IsDismissed = true;
        DismissedByUserId = userId;
        DismissedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DismissedAtUtc.Value;
    }

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
