using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Payroll;

public class PayrollTaxBracket : TenantEntity, IGlobalReadable
{
    public Guid PayrollParametersId { get; internal set; }
    public decimal? UpperBound { get; private set; }
    public decimal RatePercent { get; private set; }
    public int SortOrder { get; private set; }

    public PayrollParameters Parameters { get; private set; } = null!;

    protected PayrollTaxBracket() { }

    public PayrollTaxBracket(decimal ratePercent, int sortOrder, decimal? upperBound = null)
    {
        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentException("Bracket rate must be between 0 and 100.", nameof(ratePercent));
        }
        RatePercent = ratePercent;
        SortOrder = sortOrder;
        UpperBound = upperBound;
    }

    internal void AttachToParameters(Guid payrollParametersId, Guid tenantId)
    {
        PayrollParametersId = payrollParametersId;
        TenantId = tenantId;
    }
}
