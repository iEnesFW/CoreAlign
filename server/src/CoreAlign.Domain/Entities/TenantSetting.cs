using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Flexible per-tenant configuration store. The Settings panel groups settings
/// by <see cref="Category"/> (Sales / Inventory / Finance / Security / EInvoice /
/// Smtp / Notifications). <see cref="Value"/> is stored as JSON so callers can
/// project to typed views without schema migrations every time a flag is added.
/// </summary>
public class TenantSetting : TenantEntity
{
    public string Category { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Value { get; private set; }
    public string? Description { get; private set; }
    /// <summary>Hint for the UI: string / number / boolean / json / select / color.</summary>
    public string DataType { get; private set; } = "string";
    public bool IsSensitive { get; private set; }

    protected TenantSetting() { }

    public TenantSetting(string category, string key, string? value, string dataType = "string", string? description = null, bool isSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        Category = category.Trim();
        Key = key.Trim();
        Value = value;
        DataType = string.IsNullOrWhiteSpace(dataType) ? "string" : dataType.Trim();
        Description = description?.Trim();
        IsSensitive = isSensitive;
    }

    public void SetValue(string? value)
    {
        Value = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Describe(string? description, string? dataType = null, bool? isSensitive = null)
    {
        Description = description?.Trim();
        if (!string.IsNullOrWhiteSpace(dataType)) DataType = dataType.Trim();
        if (isSensitive.HasValue) IsSensitive = isSensitive.Value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
