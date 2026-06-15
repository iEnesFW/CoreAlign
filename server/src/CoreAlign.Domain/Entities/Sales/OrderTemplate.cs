using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Sales;

public class OrderTemplate : TenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public Guid? PriceListId { get; private set; }
    public OrderFrequency Frequency { get; private set; } = OrderFrequency.None;
    public DateTime? NextRunAtUtc { get; private set; }
    public DateTime? LastRunAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public ICollection<OrderTemplateLine> Lines { get; private set; } = new List<OrderTemplateLine>();

    protected OrderTemplate() { }

    public OrderTemplate(
        string name,
        Guid customerId,
        string currency,
        Guid createdByUserId,
        Guid? priceListId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));

        Name = name.Trim();
        CustomerId = customerId;
        Currency = currency;
        PriceListId = priceListId;
        CreatedByUserId = createdByUserId;
        Notes = notes;
        IsActive = true;
    }

    public void UpdateHeader(string name, Guid customerId, string currency, Guid? priceListId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        Name = name.Trim();
        CustomerId = customerId;
        Currency = currency;
        PriceListId = priceListId;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetSchedule(OrderFrequency frequency, DateTime? firstRunAtUtc, DateTime nowUtc)
    {
        Frequency = frequency;
        if (frequency == OrderFrequency.None)
        {
            NextRunAtUtc = null;
        }
        else
        {
            NextRunAtUtc = firstRunAtUtc ?? AdvanceFrom(nowUtc, frequency);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<OrderTemplateLine> newLines)
    {
        Lines.Clear();
        var index = 1;
        foreach (var line in newLines)
        {
            line.AttachTo(this, index++);
            Lines.Add(line);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsDue(DateTime nowUtc) =>
        IsActive && Frequency != OrderFrequency.None && NextRunAtUtc.HasValue && NextRunAtUtc.Value <= nowUtc;

    public void RecordRun(DateTime runAtUtc)
    {
        LastRunAtUtc = runAtUtc;
        NextRunAtUtc = AdvanceFrom(runAtUtc, Frequency);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static DateTime AdvanceFrom(DateTime fromUtc, OrderFrequency frequency) => frequency switch
    {
        OrderFrequency.Daily => fromUtc.AddDays(1),
        OrderFrequency.Weekly => fromUtc.AddDays(7),
        OrderFrequency.BiWeekly => fromUtc.AddDays(14),
        OrderFrequency.Monthly => fromUtc.AddMonths(1),
        OrderFrequency.Quarterly => fromUtc.AddMonths(3),
        _ => fromUtc
    };
}
