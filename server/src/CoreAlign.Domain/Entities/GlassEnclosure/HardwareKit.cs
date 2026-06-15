using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class HardwareKit : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid SystemId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<HardwareKitItem> _items = new();
    public IReadOnlyCollection<HardwareKitItem> Items => _items;

    protected HardwareKit() { }

    public HardwareKit(
        string code,
        string name,
        Guid systemId,
        string? description = null)
    {
        Code = code;
        Name = name;
        SystemId = systemId;
        Description = description;
    }

    public void Update(string name, Guid systemId, string? description, bool isActive)
    {
        Name = name;
        SystemId = systemId;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddItem(HardwareKitItem item) => _items.Add(item);

    public void RemoveItem(Guid itemId)
    {
        var existing = _items.FirstOrDefault(i => i.Id == itemId);
        if (existing is not null) _items.Remove(existing);
    }
}
