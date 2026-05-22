namespace CoreAlign.Domain.Entities;

/// <summary>
/// District / county (TR: ilçe), belonging to a <see cref="Province"/>. Global
/// reference data (no tenant filter).
/// </summary>
public class District
{
    public int Id { get; private set; }
    public int ProvinceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    protected District() { }

    public District(int id, int provinceId, string name, bool isActive = true)
    {
        Id = id;
        ProvinceId = provinceId;
        Name = name;
        IsActive = isActive;
    }
}
