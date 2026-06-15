using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class Tag : TenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? ColorHex { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Tag() { }

    public Tag(string name, string? colorHex = null)
    {
        Name = name;
        ColorHex = colorHex;
    }

    public void Update(string name, string? colorHex, bool isActive)
    {
        Name = name;
        ColorHex = colorHex;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
