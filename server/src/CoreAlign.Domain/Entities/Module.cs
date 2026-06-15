namespace CoreAlign.Domain.Entities;

/// <summary>
/// System-wide module catalog entry (NOT tenant-scoped). A module is a feature
/// area (Sales, Purchasing, Inventory, ...) that a tenant subscribes to via
/// <see cref="TenantModule"/>. Core modules (Dashboard, Billing) are auto-granted
/// to every tenant and cannot be unsubscribed.
/// </summary>
public class Module
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? IconKey { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsCore { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public ICollection<ModulePricePlan> PricePlans { get; private set; } = new List<ModulePricePlan>();

    protected Module() { }

    public Module(string code, string name, string? description, string? category, string? iconKey, int sortOrder, bool isActive, bool isCore)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        Code = code.Trim();
        Name = name.Trim();
        Description = description?.Trim();
        Category = category?.Trim();
        IconKey = iconKey?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        IsCore = isCore;
    }

    public void Update(string name, string? description, string? category, string? iconKey, int sortOrder, bool isActive, bool isCore)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        Category = category?.Trim();
        IconKey = iconKey?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        IsCore = isCore;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
