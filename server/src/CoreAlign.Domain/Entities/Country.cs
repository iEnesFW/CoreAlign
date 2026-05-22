namespace CoreAlign.Domain.Entities;

/// <summary>
/// Global ISO 3166-1 alpha-2 country reference with telephone dial code. Shared
/// across all tenants (no tenant filter).
/// </summary>
public class Country
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? DialCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Country() { }

    public Country(string code, string name, string? dialCode = null, bool isActive = true)
    {
        Code = code;
        Name = name;
        DialCode = dialCode;
        IsActive = isActive;
    }
}
