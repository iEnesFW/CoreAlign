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
        Code = Normalize(code);
        Name = string.IsNullOrWhiteSpace(name) ? Code : name.Trim();
        Symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim();
        IsActive = isActive;
    }

    public void Rename(string name, string? symbol)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
        Symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim();
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    // WHY the feed never re-activates: a currency an admin deliberately switched off must stay off
    // even though the daily bulletin keeps publishing a rate for it, otherwise the nightly job
    // silently undoes the admin's decision every morning.
    public void AdoptFeedName(string name, string? symbol)
    {
        if (!string.Equals(Name, Code, StringComparison.Ordinal)) return;
        Rename(name, symbol);
    }

    public static string Normalize(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
