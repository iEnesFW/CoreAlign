using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class ProductCategory : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ProductCategory? ParentCategory { get; set; }
    public ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();

    protected ProductCategory() { }

    public ProductCategory(string code, string name, Guid? parentCategoryId = null, string? description = null)
    {
        Code = code;
        Name = name;
        ParentCategoryId = parentCategoryId;
        Description = description;
    }

    public void Update(string code, string name, Guid? parentCategoryId, string? description, bool isActive)
    {
        if (parentCategoryId == Id)
        {
            throw new ArgumentException("A category cannot reference itself as a parent.", nameof(parentCategoryId));
        }
        Code = code;
        Name = name;
        ParentCategoryId = parentCategoryId;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
