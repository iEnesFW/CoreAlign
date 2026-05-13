using CoreAlign.Application.MasterData.DTOs;
using MediatR;

namespace CoreAlign.Application.MasterData.Queries;

public record ListBrandsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<BrandDto>>;
public record GetBrandByIdQuery(Guid Id) : IRequest<BrandDto?>;

public record ListProductCategoriesQuery(bool? IsActive = null) : IRequest<IReadOnlyList<ProductCategoryDto>>;
public record GetProductCategoryByIdQuery(Guid Id) : IRequest<ProductCategoryDto?>;

public record ListCustomerGroupsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<CustomerGroupDto>>;
public record GetCustomerGroupByIdQuery(Guid Id) : IRequest<CustomerGroupDto?>;

public record ListUnitsOfMeasureQuery(bool? IsActive = null) : IRequest<IReadOnlyList<UnitOfMeasureDto>>;
public record GetUnitOfMeasureByIdQuery(Guid Id) : IRequest<UnitOfMeasureDto?>;

public record ListTaxRatesQuery(bool? IsActive = null, bool? IsWithholding = null) : IRequest<IReadOnlyList<TaxRateDto>>;
public record GetTaxRateByIdQuery(Guid Id) : IRequest<TaxRateDto?>;

public record ListPaymentTermsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<PaymentTermDto>>;
public record GetPaymentTermByIdQuery(Guid Id) : IRequest<PaymentTermDto?>;

public record ListPriceListsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<PriceListDto>>;
public record GetPriceListByIdQuery(Guid Id) : IRequest<PriceListDto?>;

public record ListWarehousesQuery(bool? IsActive = null) : IRequest<IReadOnlyList<WarehouseDto>>;
public record GetWarehouseByIdQuery(Guid Id) : IRequest<WarehouseDto?>;
