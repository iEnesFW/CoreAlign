using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.MasterData.DTOs;

public class BrandDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ProductCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CustomerGroupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DefaultPriceListId { get; set; }
    public decimal DefaultDiscountPercent { get; set; }
    public bool IsActive { get; set; }
}

public class UnitOfMeasureDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public Guid? BaseUomId { get; set; }
    public decimal ConversionFactor { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsBase { get; set; }
    public bool IsActive { get; set; }
}

public class TaxRateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public bool IsWithholding { get; set; }
    public string? CountryCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class PaymentTermDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int NetDays { get; set; }
    public int DiscountDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool EndOfMonth { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class WithholdingTaxCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int Numerator { get; set; }
    public int Denominator { get; set; }
    public bool IsActive { get; set; }
}

public class VatExemptionCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LawReference { get; set; }
    public string Kind { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PriceListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public bool IsTaxInclusive { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public bool IsDefault { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WarehouseType Type { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string? Swift { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal OpeningBalance { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
