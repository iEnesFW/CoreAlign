using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Accounting.Mapping;

public static class AccountingMapper
{
    public static AccountingPeriodDto ToDto(AccountingPeriod p) => new()
    {
        Id = p.Id,
        Year = p.Year,
        Month = p.Month,
        Code = p.Code,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Status = p.Status,
        ClosedAtUtc = p.ClosedAtUtc,
        ClosedByUserId = p.ClosedByUserId,
        ReopenedAtUtc = p.ReopenedAtUtc,
        Notes = p.Notes,
    };

    public static CustomerProductPriceDto ToDto(CustomerProductPrice p) => new()
    {
        Id = p.Id,
        CustomerId = p.CustomerId,
        CustomerName = p.Customer?.Name ?? string.Empty,
        ProductId = p.ProductId,
        ProductSku = p.Product?.Sku ?? string.Empty,
        ProductName = p.Product?.Name ?? string.Empty,
        Currency = p.Currency,
        Price = p.Price,
        DiscountPercent = p.DiscountPercent,
        MinQuantity = p.MinQuantity,
        MaxQuantity = p.MaxQuantity,
        ValidFromUtc = p.ValidFromUtc,
        ValidUntilUtc = p.ValidUntilUtc,
        Notes = p.Notes,
        IsActive = p.IsActive,
    };

    public static ResolvedPriceDto ToDto(PriceResolutionResult r) => new()
    {
        UnitPrice = r.UnitPrice,
        Currency = r.Currency,
        DiscountPercent = r.DiscountPercent,
        Source = r.Source,
        SourceLabel = r.SourceLabel,
        ReferenceListPrice = r.ReferenceListPrice,
        TaxRatePercent = r.TaxRatePercent,
        IsTaxInclusive = r.IsTaxInclusive,
        TaxRateId = r.TaxRateId,
        AppliedRecordId = r.AppliedRecordId,
    };
}
