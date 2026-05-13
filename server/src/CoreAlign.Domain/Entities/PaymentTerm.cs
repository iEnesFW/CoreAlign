using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class PaymentTerm : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int NetDays { get; private set; }
    public int DiscountDays { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public bool EndOfMonth { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected PaymentTerm() { }

    public PaymentTerm(string code, string name, int netDays, int discountDays = 0, decimal discountPercent = 0m, bool endOfMonth = false, string? description = null)
    {
        if (netDays < 0)
        {
            throw new ArgumentException("Net days cannot be negative.", nameof(netDays));
        }
        Code = code;
        Name = name;
        NetDays = netDays;
        DiscountDays = discountDays;
        DiscountPercent = discountPercent;
        EndOfMonth = endOfMonth;
        Description = description;
    }

    public DateTime ResolveDueDate(DateTime baseDate)
    {
        var due = baseDate.AddDays(NetDays);
        if (!EndOfMonth) return due;
        var lastDay = DateTime.DaysInMonth(due.Year, due.Month);
        return new DateTime(due.Year, due.Month, lastDay, 23, 59, 59, DateTimeKind.Utc);
    }

    public void Update(string code, string name, int netDays, int discountDays, decimal discountPercent, bool endOfMonth, string? description, bool isActive)
    {
        if (netDays < 0)
        {
            throw new ArgumentException("Net days cannot be negative.", nameof(netDays));
        }
        Code = code;
        Name = name;
        NetDays = netDays;
        DiscountDays = discountDays;
        DiscountPercent = discountPercent;
        EndOfMonth = endOfMonth;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
