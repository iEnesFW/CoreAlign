using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.MasterData.Mapping;

public static class MasterDataMapper
{
    public static BrandDto ToDto(Brand b) => new()
    {
        Id = b.Id, Code = b.Code, Name = b.Name, Description = b.Description, IsActive = b.IsActive
    };

    public static ProductCategoryDto ToDto(ProductCategory c) => new()
    {
        Id = c.Id, Code = c.Code, Name = c.Name, ParentCategoryId = c.ParentCategoryId,
        Description = c.Description, IsActive = c.IsActive
    };

    public static CustomerGroupDto ToDto(CustomerGroup g) => new()
    {
        Id = g.Id, Code = g.Code, Name = g.Name, Description = g.Description,
        DefaultPriceListId = g.DefaultPriceListId, DefaultDiscountPercent = g.DefaultDiscountPercent,
        IsActive = g.IsActive
    };

    public static UnitOfMeasureDto ToDto(UnitOfMeasure u) => new()
    {
        Id = u.Id, Code = u.Code, Name = u.Name, Symbol = u.Symbol,
        BaseUomId = u.BaseUomId, ConversionFactor = u.ConversionFactor,
        DecimalPlaces = u.DecimalPlaces, IsBase = u.IsBase, IsActive = u.IsActive
    };

    public static TaxRateDto ToDto(TaxRate t) => new()
    {
        Id = t.Id, Code = t.Code, Name = t.Name, RatePercent = t.RatePercent,
        IsWithholding = t.IsWithholding, CountryCode = t.CountryCode,
        Description = t.Description, IsActive = t.IsActive
    };

    public static PaymentTermDto ToDto(PaymentTerm p) => new()
    {
        Id = p.Id, Code = p.Code, Name = p.Name, NetDays = p.NetDays,
        DiscountDays = p.DiscountDays, DiscountPercent = p.DiscountPercent,
        EndOfMonth = p.EndOfMonth, Description = p.Description, IsActive = p.IsActive
    };

    public static PriceListDto ToDto(PriceList l) => new()
    {
        Id = l.Id, Code = l.Code, Name = l.Name, Currency = l.Currency,
        IsTaxInclusive = l.IsTaxInclusive, ValidFromUtc = l.ValidFromUtc,
        ValidUntilUtc = l.ValidUntilUtc, IsDefault = l.IsDefault,
        Description = l.Description, IsActive = l.IsActive
    };

    public static WarehouseDto ToDto(Warehouse w) => new()
    {
        Id = w.Id, Code = w.Code, Name = w.Name, Type = w.Type,
        AddressLine1 = w.AddressLine1, AddressLine2 = w.AddressLine2,
        City = w.City, State = w.State, PostalCode = w.PostalCode,
        Country = w.Country, Phone = w.Phone, IsDefault = w.IsDefault, IsActive = w.IsActive
    };

    public static BankAccountDto ToDto(BankAccount b) => new()
    {
        Id = b.Id, AccountName = b.AccountName, BankName = b.BankName, BranchName = b.BranchName,
        Iban = b.Iban, Swift = b.Swift, Currency = b.Currency, OpeningBalance = b.OpeningBalance,
        IsPrimary = b.IsPrimary, IsActive = b.IsActive, Notes = b.Notes
    };
}
