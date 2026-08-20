using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Payroll;

public class EmployeeDeduction : TenantEntity
{
    public Guid EmployeeId { get; internal set; }
    public DeductionType DeductionType { get; private set; }
    public decimal? Amount { get; private set; }
    public decimal? Percent { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public int Priority { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Employee Employee { get; private set; } = null!;

    protected EmployeeDeduction() { }

    public EmployeeDeduction(
        DeductionType deductionType,
        DateOnly effectiveFrom,
        decimal? amount = null,
        decimal? percent = null,
        decimal remainingBalance = 0m,
        int priority = 0,
        DateOnly? effectiveTo = null)
    {
        var hasAmount = amount.HasValue;
        var hasPercent = percent.HasValue;
        if (hasAmount == hasPercent)
        {
            throw new ArgumentException("Exactly one of amount or percent must be set on a deduction.", nameof(amount));
        }
        if (hasAmount && amount!.Value < 0m)
        {
            throw new ArgumentException("Deduction amount cannot be negative.", nameof(amount));
        }
        if (hasPercent && (percent!.Value < 0m || percent.Value > 100m))
        {
            throw new ArgumentException("Deduction percent must be between 0 and 100.", nameof(percent));
        }
        DeductionType = deductionType;
        Amount = hasAmount ? Math.Round(amount!.Value, 4) : null;
        Percent = hasPercent ? Math.Round(percent!.Value, 4) : null;
        RemainingBalance = Math.Round(remainingBalance, 4);
        Priority = priority;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    internal void AttachToEmployee(Guid employeeId) => EmployeeId = employeeId;

    public void Update(
        DateOnly effectiveFrom,
        decimal? amount,
        decimal? percent,
        decimal remainingBalance,
        int priority,
        DateOnly? effectiveTo)
    {
        var hasAmount = amount.HasValue;
        var hasPercent = percent.HasValue;
        if (hasAmount == hasPercent)
        {
            throw new ArgumentException("Exactly one of amount or percent must be set on a deduction.", nameof(amount));
        }
        if (hasAmount && amount!.Value < 0m)
        {
            throw new ArgumentException("Deduction amount cannot be negative.", nameof(amount));
        }
        if (hasPercent && (percent!.Value < 0m || percent.Value > 100m))
        {
            throw new ArgumentException("Deduction percent must be between 0 and 100.", nameof(percent));
        }
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("Effective-to cannot precede effective-from.", nameof(effectiveTo));
        }
        Amount = hasAmount ? Math.Round(amount!.Value, 4) : null;
        Percent = hasPercent ? Math.Round(percent!.Value, 4) : null;
        RemainingBalance = Math.Round(remainingBalance, 4);
        Priority = priority;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsCurrentlyValid(DateOnly asOf) =>
        IsActive
        && asOf >= EffectiveFrom
        && (EffectiveTo is null || asOf <= EffectiveTo.Value);

    public void ReduceBalance(decimal amount)
    {
        if (amount <= 0m) return;
        // A percentage deduction (union dues and the like) carries no balance to amortise.
        // Falling through would drive it to zero and DEACTIVATE it, silently stopping a
        // deduction that is supposed to recur for as long as the employee is on the payroll.
        if (RemainingBalance <= 0m) return;
        RemainingBalance = Math.Max(0m, Math.Round(RemainingBalance - amount, 4));
        if (RemainingBalance <= 0m)
        {
            IsActive = false;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(DateOnly effectiveTo)
    {
        IsActive = false;
        EffectiveTo = effectiveTo;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
