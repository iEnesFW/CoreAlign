using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Snapshot of one line of a <see cref="SubscriptionOrder"/>. Module/plan fields
/// are denormalized at create-time so the order survives catalog changes.
/// </summary>
public class SubscriptionOrderItem : TenantEntity
{
    public Guid SubscriptionOrderId { get; private set; }
    public Guid ModuleId { get; private set; }
    public Guid PlanId { get; private set; }
    public string ModuleCode { get; private set; } = string.Empty;
    public string ModuleName { get; private set; } = string.Empty;
    public string PlanLabel { get; private set; } = string.Empty;
    public int DurationDays { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public SubscriptionOrder Order { get; private set; } = null!;

    protected SubscriptionOrderItem() { }

    public SubscriptionOrderItem(
        Guid moduleId,
        Guid planId,
        string moduleCode,
        string moduleName,
        string planLabel,
        int durationDays,
        decimal unitPrice,
        string currency)
    {
        if (moduleId == Guid.Empty) throw new ArgumentException("ModuleId is required.", nameof(moduleId));
        if (planId == Guid.Empty) throw new ArgumentException("PlanId is required.", nameof(planId));
        if (string.IsNullOrWhiteSpace(moduleCode)) throw new ArgumentException("ModuleCode is required.", nameof(moduleCode));
        if (string.IsNullOrWhiteSpace(moduleName)) throw new ArgumentException("ModuleName is required.", nameof(moduleName));
        if (string.IsNullOrWhiteSpace(planLabel)) throw new ArgumentException("PlanLabel is required.", nameof(planLabel));
        if (durationDays <= 0) throw new ArgumentOutOfRangeException(nameof(durationDays));
        if (unitPrice < 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3) throw new ArgumentException("Currency must be a 1-3 char code.", nameof(currency));

        ModuleId = moduleId;
        PlanId = planId;
        ModuleCode = moduleCode.Trim();
        ModuleName = moduleName.Trim();
        PlanLabel = planLabel.Trim();
        DurationDays = durationDays;
        UnitPrice = unitPrice;
        Currency = currency.Trim().ToUpperInvariant();
    }
}
