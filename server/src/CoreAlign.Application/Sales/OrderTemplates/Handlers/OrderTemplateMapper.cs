using CoreAlign.Application.Sales.OrderTemplates.DTOs;
using CoreAlign.Domain.Entities.Sales;

namespace CoreAlign.Application.Sales.OrderTemplates.Handlers;

public static class OrderTemplateMapper
{
    public static OrderTemplateDto ToDto(OrderTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        CustomerId = template.CustomerId,
        Currency = template.Currency,
        PriceListId = template.PriceListId,
        Frequency = template.Frequency,
        NextRunAtUtc = template.NextRunAtUtc,
        LastRunAtUtc = template.LastRunAtUtc,
        IsActive = template.IsActive,
        CreatedByUserId = template.CreatedByUserId,
        Notes = template.Notes,
        CreatedAtUtc = template.CreatedAtUtc,
        UpdatedAtUtc = template.UpdatedAtUtc,
        Lines = template.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new OrderTemplateLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductSku = l.ProductSku,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                Notes = l.Notes
            })
            .ToList()
    };
}
