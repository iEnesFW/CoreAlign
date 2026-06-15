using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Variants;

internal static class ProductVariantMapper
{
    public static ProductVariantDto ToDto(ProductVariant v) => new(
        v.Id,
        v.ParentProductId,
        v.Sku,
        v.Barcode,
        v.VariantAttributesJson,
        v.PriceOverride,
        v.StockQuantity,
        v.IsActive,
        v.ConcurrencyToken,
        v.CreatedAtUtc,
        v.UpdatedAtUtc);
}

public sealed class ListProductVariantsHandler : IRequestHandler<ListProductVariantsQuery, IReadOnlyList<ProductVariantDto>>
{
    private readonly IProductRepository _products;
    private readonly IProductVariantRepository _variants;

    public ListProductVariantsHandler(IProductRepository products, IProductVariantRepository variants)
    {
        _products = products;
        _variants = variants;
    }

    public async Task<IReadOnlyList<ProductVariantDto>> Handle(ListProductVariantsQuery request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException();
        var list = await _variants.ListByProductAsync(product.Id, cancellationToken);
        return list.Select(ProductVariantMapper.ToDto).ToList();
    }
}

public sealed class CreateProductVariantHandler : IRequestHandler<CreateProductVariantCommand, ProductVariantDto>
{
    private readonly IProductRepository _products;
    private readonly IProductVariantRepository _variants;
    private readonly IUnitOfWork _uow;

    public CreateProductVariantHandler(
        IProductRepository products,
        IProductVariantRepository variants,
        IUnitOfWork uow)
    {
        _products = products;
        _variants = variants;
        _uow = uow;
    }

    public async Task<ProductVariantDto> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException();

        if (await _variants.SkuExistsAsync(product.Id, request.Sku, null, cancellationToken))
        {
            throw new DuplicateProductSkuException();
        }

        var variant = new ProductVariant(
            product.Id,
            request.Sku,
            request.VariantAttributesJson,
            request.Barcode,
            request.PriceOverride,
            request.StockQuantity,
            request.IsActive)
        {
            TenantId = product.TenantId,
        };

        await _variants.AddAsync(variant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ProductVariantMapper.ToDto(variant);
    }
}

public sealed class UpdateProductVariantHandler : IRequestHandler<UpdateProductVariantCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variants;
    private readonly IUnitOfWork _uow;

    public UpdateProductVariantHandler(IProductVariantRepository variants, IUnitOfWork uow)
    {
        _variants = variants;
        _uow = uow;
    }

    public async Task<ProductVariantDto> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variants.GetByIdAsync(request.VariantId, cancellationToken)
            ?? throw new ProductNotFoundException();

        if (variant.ParentProductId != request.ProductId)
        {
            throw new ProductNotFoundException();
        }

        if (!string.Equals(variant.Sku, request.Sku, StringComparison.Ordinal)
            && await _variants.SkuExistsAsync(variant.ParentProductId, request.Sku, variant.Id, cancellationToken))
        {
            throw new DuplicateProductSkuException();
        }

        variant.UpdateDetails(
            request.Sku,
            request.Barcode,
            request.VariantAttributesJson,
            request.PriceOverride,
            request.IsActive);

        _variants.Update(variant);
        await _uow.SaveChangesAsync(cancellationToken);
        return ProductVariantMapper.ToDto(variant);
    }
}

public sealed class DeleteProductVariantHandler : IRequestHandler<DeleteProductVariantCommand, bool>
{
    private readonly IProductVariantRepository _variants;
    private readonly IUnitOfWork _uow;

    public DeleteProductVariantHandler(IProductVariantRepository variants, IUnitOfWork uow)
    {
        _variants = variants;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variants.GetByIdAsync(request.VariantId, cancellationToken)
            ?? throw new ProductNotFoundException();

        if (variant.ParentProductId != request.ProductId)
        {
            throw new ProductNotFoundException();
        }

        _variants.Remove(variant);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
