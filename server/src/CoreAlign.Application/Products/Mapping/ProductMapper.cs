using CoreAlign.Application.Products.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Products.Mapping;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Barcode = product.Barcode,
        Mpn = product.Mpn,
        Name = product.Name,
        ShortDescription = product.ShortDescription,
        Description = product.Description,
        Slug = product.Slug,
        BrandId = product.BrandId,
        CategoryId = product.CategoryId,
        ParentProductId = product.ParentProductId,
        VariantAttributesJson = product.VariantAttributesJson,
        TagsJson = product.TagsJson,
        Unit = product.Unit,
        BaseUomId = product.BaseUomId,
        PurchaseUomId = product.PurchaseUomId,
        SalesUomId = product.SalesUomId,
        Price = product.Price,
        ListPrice = product.ListPrice,
        MinSellingPrice = product.MinSellingPrice,
        StandardCost = product.StandardCost,
        LastPurchaseCost = product.LastPurchaseCost,
        AverageCost = product.AverageCost,
        Currency = product.Currency,
        TaxRateId = product.TaxRateId,
        IsPriceTaxInclusive = product.IsPriceTaxInclusive,
        StockQuantity = product.StockQuantity,
        IsStockTracked = product.IsStockTracked,
        IsLotTracked = product.IsLotTracked,
        IsSerialTracked = product.IsSerialTracked,
        MinStock = product.MinStock,
        MaxStock = product.MaxStock,
        ReorderPoint = product.ReorderPoint,
        SafetyStock = product.SafetyStock,
        LeadTimeDays = product.LeadTimeDays,
        WeightKg = product.WeightKg,
        WidthCm = product.WidthCm,
        HeightCm = product.HeightCm,
        DepthCm = product.DepthCm,
        VolumeM3 = product.VolumeM3,
        Status = product.Status,
        LaunchDate = product.LaunchDate,
        EndOfLifeDate = product.EndOfLifeDate,
        IsActive = product.IsActive,
        CreatedAtUtc = product.CreatedAtUtc,
        UpdatedAtUtc = product.UpdatedAtUtc
    };
}
