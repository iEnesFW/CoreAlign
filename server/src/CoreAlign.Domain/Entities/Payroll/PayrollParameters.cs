using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Payroll;

public class PayrollParameters : TenantEntity, IGlobalReadable
{
    public int EffectiveYear { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }

    public decimal SgkEmployeeRate { get; private set; }
    public decimal SgkEmployerRate { get; private set; }
    public decimal SgkEmployer5PointIncentiveRate { get; private set; }
    public decimal UnemploymentEmployeeRate { get; private set; }
    public decimal UnemploymentEmployerRate { get; private set; }
    public decimal SgkFloorMonthly { get; private set; }
    public decimal SgkCeilingMultiplier { get; private set; }
    public decimal SgkCeilingMonthly { get; private set; }
    public decimal StampTaxRate { get; private set; }
    public decimal GrossMinimumWage { get; private set; }
    public bool MinWageExemptionEnabled { get; private set; }
    public decimal Disability1Amount { get; private set; }
    public decimal Disability2Amount { get; private set; }
    public decimal Disability3Amount { get; private set; }

    public ICollection<PayrollTaxBracket> TaxBrackets { get; private set; } = new List<PayrollTaxBracket>();

    protected PayrollParameters() { }

    public PayrollParameters(
        int effectiveYear,
        DateOnly effectiveFrom,
        decimal sgkEmployeeRate,
        decimal sgkEmployerRate,
        decimal sgkEmployer5PointIncentiveRate,
        decimal unemploymentEmployeeRate,
        decimal unemploymentEmployerRate,
        decimal sgkFloorMonthly,
        decimal sgkCeilingMultiplier,
        decimal sgkCeilingMonthly,
        decimal stampTaxRate,
        decimal grossMinimumWage,
        decimal disability1Amount,
        decimal disability2Amount,
        decimal disability3Amount,
        bool minWageExemptionEnabled = true,
        DateOnly? effectiveTo = null,
        string? description = null)
    {
        EffectiveYear = effectiveYear;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description;
        SgkEmployeeRate = sgkEmployeeRate;
        SgkEmployerRate = sgkEmployerRate;
        SgkEmployer5PointIncentiveRate = sgkEmployer5PointIncentiveRate;
        UnemploymentEmployeeRate = unemploymentEmployeeRate;
        UnemploymentEmployerRate = unemploymentEmployerRate;
        SgkFloorMonthly = sgkFloorMonthly;
        SgkCeilingMultiplier = sgkCeilingMultiplier;
        SgkCeilingMonthly = sgkCeilingMonthly;
        StampTaxRate = stampTaxRate;
        GrossMinimumWage = grossMinimumWage;
        MinWageExemptionEnabled = minWageExemptionEnabled;
        Disability1Amount = disability1Amount;
        Disability2Amount = disability2Amount;
        Disability3Amount = disability3Amount;
    }

    public void Update(
        decimal sgkEmployeeRate,
        decimal sgkEmployerRate,
        decimal sgkEmployer5PointIncentiveRate,
        decimal unemploymentEmployeeRate,
        decimal unemploymentEmployerRate,
        decimal sgkFloorMonthly,
        decimal sgkCeilingMultiplier,
        decimal sgkCeilingMonthly,
        decimal stampTaxRate,
        decimal grossMinimumWage,
        decimal disability1Amount,
        decimal disability2Amount,
        decimal disability3Amount,
        bool minWageExemptionEnabled,
        bool isActive,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? description)
    {
        SgkEmployeeRate = sgkEmployeeRate;
        SgkEmployerRate = sgkEmployerRate;
        SgkEmployer5PointIncentiveRate = sgkEmployer5PointIncentiveRate;
        UnemploymentEmployeeRate = unemploymentEmployeeRate;
        UnemploymentEmployerRate = unemploymentEmployerRate;
        SgkFloorMonthly = sgkFloorMonthly;
        SgkCeilingMultiplier = sgkCeilingMultiplier;
        SgkCeilingMonthly = sgkCeilingMonthly;
        StampTaxRate = stampTaxRate;
        GrossMinimumWage = grossMinimumWage;
        Disability1Amount = disability1Amount;
        Disability2Amount = disability2Amount;
        Disability3Amount = disability3Amount;
        MinWageExemptionEnabled = minWageExemptionEnabled;
        IsActive = isActive;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddTaxBracket(PayrollTaxBracket bracket)
    {
        bracket.AttachToParameters(Id, TenantId);
        TaxBrackets.Add(bracket);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsCurrentlyValid(DateOnly asOf) =>
        IsActive
        && asOf >= EffectiveFrom
        && (EffectiveTo is null || asOf <= EffectiveTo.Value);

    public decimal TaxOnCumulative(decimal cumulativeBase)
    {
        if (cumulativeBase <= 0m)
        {
            return 0m;
        }

        decimal tax = 0m;
        decimal lowerBound = 0m;
        foreach (var bracket in TaxBrackets.OrderBy(b => b.SortOrder))
        {
            if (cumulativeBase <= lowerBound)
            {
                break;
            }
            var upper = bracket.UpperBound ?? cumulativeBase;
            var segmentTop = Math.Min(cumulativeBase, upper);
            tax += (segmentTop - lowerBound) * (bracket.RatePercent / 100m);
            lowerBound = upper;
        }
        return tax;
    }

    public decimal TaxOnCumulative(decimal cumulativeBaseBefore, decimal periodBase) =>
        TaxOnCumulative(cumulativeBaseBefore + periodBase) - TaxOnCumulative(cumulativeBaseBefore);
}
