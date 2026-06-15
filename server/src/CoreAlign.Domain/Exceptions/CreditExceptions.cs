namespace CoreAlign.Domain.Exceptions;

public class CreditSoftLimitExceededException : DomainException
{
    public decimal Limit { get; }
    public decimal Outstanding { get; }
    public decimal UsagePercent { get; }

    public CreditSoftLimitExceededException(decimal limit, decimal outstanding, decimal usagePercent)
        : base($"Customer credit usage is at {usagePercent}% of limit ({outstanding}/{limit}).")
    {
        Limit = limit;
        Outstanding = outstanding;
        UsagePercent = usagePercent;
    }
}
