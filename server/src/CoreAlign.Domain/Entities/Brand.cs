using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class Brand : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Brand() { }

    public Brand(string code, string name, string? description = null)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    public void Update(string code, string name, string? description, bool isActive)
    {
        Code = code;
        Name = name;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
