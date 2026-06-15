using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Products.Variants;

public sealed record ListProductVariantsQuery(Guid ProductId)
    : IRequest<IReadOnlyList<ProductVariantDto>>;

public sealed record CreateProductVariantCommand(
    Guid ProductId,
    string Sku,
    string? Barcode,
    string VariantAttributesJson,
    decimal? PriceOverride,
    decimal StockQuantity,
    bool IsActive) : IRequest<ProductVariantDto>, ITransactionalRequest;

public sealed record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string? Barcode,
    string VariantAttributesJson,
    decimal? PriceOverride,
    bool IsActive) : IRequest<ProductVariantDto>, ITransactionalRequest;

public sealed record DeleteProductVariantCommand(
    Guid ProductId,
    Guid VariantId) : IRequest<bool>, ITransactionalRequest;
