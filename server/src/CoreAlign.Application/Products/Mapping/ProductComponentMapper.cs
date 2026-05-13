using CoreAlign.Application.Products.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Products.Mapping;

public static class ProductComponentMapper
{
    public static ProductComponentDto ToDto(ProductComponent component) => new()
    {
        Id = component.Id,
        ParentProductId = component.ParentProductId,
        ComponentProductId = component.ComponentProductId,
        ComponentSku = component.ComponentProduct?.Sku ?? string.Empty,
        ComponentName = component.ComponentProduct?.Name ?? string.Empty,
        ComponentUnit = component.ComponentProduct?.Unit ?? string.Empty,
        Quantity = component.Quantity,
        Notes = component.Notes,
        CreatedAtUtc = component.CreatedAtUtc,
        UpdatedAtUtc = component.UpdatedAtUtc
    };
}
