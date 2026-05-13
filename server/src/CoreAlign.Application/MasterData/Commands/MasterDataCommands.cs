using CoreAlign.Application.Common;
using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.MasterData.Commands;

public record CreateBrandCommand(string Code, string Name, string? Description = null)
    : IRequest<BrandDto>, ITransactionalRequest;
public record UpdateBrandCommand(Guid Id, string Code, string Name, string? Description, bool IsActive)
    : IRequest<BrandDto>, ITransactionalRequest;
public record DeleteBrandCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateProductCategoryCommand(string Code, string Name, Guid? ParentCategoryId = null, string? Description = null)
    : IRequest<ProductCategoryDto>, ITransactionalRequest;
public record UpdateProductCategoryCommand(Guid Id, string Code, string Name, Guid? ParentCategoryId, string? Description, bool IsActive)
    : IRequest<ProductCategoryDto>, ITransactionalRequest;
public record DeleteProductCategoryCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateCustomerGroupCommand(string Code, string Name, string? Description = null, Guid? DefaultPriceListId = null, decimal DefaultDiscountPercent = 0m)
    : IRequest<CustomerGroupDto>, ITransactionalRequest;
public record UpdateCustomerGroupCommand(Guid Id, string Code, string Name, string? Description, Guid? DefaultPriceListId, decimal DefaultDiscountPercent, bool IsActive)
    : IRequest<CustomerGroupDto>, ITransactionalRequest;
public record DeleteCustomerGroupCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateUnitOfMeasureCommand(string Code, string Name, string? Symbol = null, Guid? BaseUomId = null, decimal ConversionFactor = 1m, int DecimalPlaces = 2)
    : IRequest<UnitOfMeasureDto>, ITransactionalRequest;
public record UpdateUnitOfMeasureCommand(Guid Id, string Code, string Name, string? Symbol, Guid? BaseUomId, decimal ConversionFactor, int DecimalPlaces, bool IsActive)
    : IRequest<UnitOfMeasureDto>, ITransactionalRequest;
public record DeleteUnitOfMeasureCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateTaxRateCommand(string Code, string Name, decimal RatePercent, bool IsWithholding = false, string? CountryCode = null, string? Description = null)
    : IRequest<TaxRateDto>, ITransactionalRequest;
public record UpdateTaxRateCommand(Guid Id, string Code, string Name, decimal RatePercent, bool IsWithholding, string? CountryCode, string? Description, bool IsActive)
    : IRequest<TaxRateDto>, ITransactionalRequest;
public record DeleteTaxRateCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreatePaymentTermCommand(string Code, string Name, int NetDays, int DiscountDays = 0, decimal DiscountPercent = 0m, bool EndOfMonth = false, string? Description = null)
    : IRequest<PaymentTermDto>, ITransactionalRequest;
public record UpdatePaymentTermCommand(Guid Id, string Code, string Name, int NetDays, int DiscountDays, decimal DiscountPercent, bool EndOfMonth, string? Description, bool IsActive)
    : IRequest<PaymentTermDto>, ITransactionalRequest;
public record DeletePaymentTermCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreatePriceListCommand(string Code, string Name, string Currency, bool IsTaxInclusive = false, DateTime? ValidFromUtc = null, DateTime? ValidUntilUtc = null, bool IsDefault = false, string? Description = null)
    : IRequest<PriceListDto>, ITransactionalRequest;
public record UpdatePriceListCommand(Guid Id, string Code, string Name, string Currency, bool IsTaxInclusive, DateTime? ValidFromUtc, DateTime? ValidUntilUtc, bool IsDefault, string? Description, bool IsActive)
    : IRequest<PriceListDto>, ITransactionalRequest;
public record DeletePriceListCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateWarehouseCommand(string Code, string Name, WarehouseType Type = WarehouseType.Main, bool IsDefault = false)
    : IRequest<WarehouseDto>, ITransactionalRequest;
public record UpdateWarehouseCommand(Guid Id, string Code, string Name, WarehouseType Type, string? AddressLine1, string? AddressLine2, string? City, string? State, string? PostalCode, string? Country, string? Phone, Guid? ManagerUserId, bool IsDefault, bool IsActive)
    : IRequest<WarehouseDto>, ITransactionalRequest;
public record DeleteWarehouseCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
