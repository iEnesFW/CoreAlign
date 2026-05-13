namespace CoreAlign.Application.Products.DTOs;

public class ProductComponentDto
{
    public Guid Id { get; set; }
    public Guid ParentProductId { get; set; }
    public Guid ComponentProductId { get; set; }
    public string ComponentSku { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
