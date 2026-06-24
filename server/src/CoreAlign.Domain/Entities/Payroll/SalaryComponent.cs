using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Payroll;

public class SalaryComponent : TenantEntity
{
    public Guid EmployeeId { get; internal set; }
    public SalaryComponentType ComponentType { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsRecurring { get; private set; }
    public bool TaxExempt { get; private set; }
    public bool SgkExempt { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Employee Employee { get; private set; } = null!;

    protected SalaryComponent() { }

    public SalaryComponent(
        SalaryComponentType componentType,
        decimal amount,
        DateOnly effectiveFrom,
        bool isRecurring = true,
        bool taxExempt = false,
        bool sgkExempt = false,
        DateOnly? effectiveTo = null)
    {
        if (amount < 0m)
        {
            throw new ArgumentException("Component amount cannot be negative.", nameof(amount));
        }
        ComponentType = componentType;
        Amount = Math.Round(amount, 4);
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsRecurring = isRecurring;
        TaxExempt = taxExempt;
        SgkExempt = sgkExempt;
    }

    internal void AttachToEmployee(Guid employeeId) => EmployeeId = employeeId;

    public void Update(
        decimal amount,
        DateOnly effectiveFrom,
        bool isRecurring,
        bool taxExempt,
        bool sgkExempt,
        DateOnly? effectiveTo)
    {
        if (amount < 0m)
        {
            throw new ArgumentException("Component amount cannot be negative.", nameof(amount));
        }
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("Effective-to cannot precede effective-from.", nameof(effectiveTo));
        }
        Amount = Math.Round(amount, 4);
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsRecurring = isRecurring;
        TaxExempt = taxExempt;
        SgkExempt = sgkExempt;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsCurrentlyValid(DateOnly asOf) =>
        IsActive
        && asOf >= EffectiveFrom
        && (EffectiveTo is null || asOf <= EffectiveTo.Value);

    public void Deactivate(DateOnly effectiveTo)
    {
        IsActive = false;
        EffectiveTo = effectiveTo;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
