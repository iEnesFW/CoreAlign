namespace CoreAlign.Domain.Entities;

/// <summary>
/// System-wide price plan attached to a <see cref="Module"/>. Each plan defines a
/// purchasable duration + price (e.g. Monthly/30d/99 TRY, Yearly/365d/999 TRY).
/// </summary>
public class ModulePricePlan
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ModuleId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayLabel { get; private set; } = string.Empty;
    public int DurationDays { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public Module Module { get; private set; } = null!;

    protected ModulePricePlan() { }

    public ModulePricePlan(Guid moduleId, string code, string displayLabel, int durationDays, decimal price, string currency, bool isActive, int sortOrder)
    {
        if (moduleId == Guid.Empty) throw new ArgumentException("ModuleId is required.", nameof(moduleId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(displayLabel)) throw new ArgumentException("DisplayLabel is required.", nameof(displayLabel));
        if (durationDays <= 0) throw new ArgumentOutOfRangeException(nameof(durationDays), "DurationDays must be positive.");
        if (price < 0m) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3) throw new ArgumentException("Currency must be a 1-3 char code.", nameof(currency));

        ModuleId = moduleId;
        Code = code.Trim();
        DisplayLabel = displayLabel.Trim();
        DurationDays = durationDays;
        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        IsActive = isActive;
        SortOrder = sortOrder;
    }

    public void Update(string displayLabel, int durationDays, decimal price, string currency, bool isActive, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(displayLabel)) throw new ArgumentException("DisplayLabel is required.", nameof(displayLabel));
        if (durationDays <= 0) throw new ArgumentOutOfRangeException(nameof(durationDays));
        if (price < 0m) throw new ArgumentOutOfRangeException(nameof(price));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3) throw new ArgumentException("Currency must be a 1-3 char code.", nameof(currency));

        DisplayLabel = displayLabel.Trim();
        DurationDays = durationDays;
        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        IsActive = isActive;
        SortOrder = sortOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
