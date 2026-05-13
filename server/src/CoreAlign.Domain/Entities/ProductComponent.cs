using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class ProductComponent : TenantEntity
{
    public Guid ParentProductId { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }

    public Product ParentProduct { get; set; } = null!;
    public Product ComponentProduct { get; set; } = null!;

    protected ProductComponent() { }

    public ProductComponent(Guid parentProductId, Guid componentProductId, decimal quantity, string? notes = null)
    {
        if (parentProductId == componentProductId)
        {
            throw new ArgumentException("A product cannot reference itself as a component.", nameof(componentProductId));
        }
        if (quantity <= 0m)
        {
            throw new ArgumentException("Component quantity must be positive.", nameof(quantity));
        }

        ParentProductId = parentProductId;
        ComponentProductId = componentProductId;
        Quantity = quantity;
        Notes = notes;
    }

    public void Update(decimal quantity, string? notes)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentException("Component quantity must be positive.", nameof(quantity));
        }
        Quantity = quantity;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
