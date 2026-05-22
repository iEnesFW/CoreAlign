namespace CoreAlign.Domain.Entities;

/// <summary>
/// Province / state (TR: il). Global reference data (no tenant filter). For
/// Turkey the <see cref="Id"/> is the official plate code (1-81).
/// </summary>
public class Province
{
    public int Id { get; private set; }
    public string CountryCode { get; private set; } = "TR";
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    protected Province() { }

    public Province(int id, string name, string countryCode = "TR", bool isActive = true)
    {
        Id = id;
        Name = name;
        CountryCode = countryCode;
        IsActive = isActive;
    }
}
