namespace CoreAlign.Domain.Entities;

/// <summary>
/// Global ISO 4217 currency reference. Shared across all tenants (no tenant
/// filter) — seed/extend with the full ISO set as needed.
/// </summary>
public class Currency
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Symbol { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Currency() { }

    public Currency(string code, string name, string? symbol = null, bool isActive = true)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        IsActive = isActive;
    }
}
