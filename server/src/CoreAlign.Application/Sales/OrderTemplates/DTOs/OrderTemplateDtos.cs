using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Sales.OrderTemplates.DTOs;

public class OrderTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = "TRY";
    public Guid? PriceListId { get; set; }
    public OrderFrequency Frequency { get; set; }
    public DateTime? NextRunAtUtc { get; set; }
    public DateTime? LastRunAtUtc { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public IReadOnlyList<OrderTemplateLineDto> Lines { get; set; } = Array.Empty<OrderTemplateLineDto>();
}

public class OrderTemplateLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public record OrderTemplateLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? Notes = null);
