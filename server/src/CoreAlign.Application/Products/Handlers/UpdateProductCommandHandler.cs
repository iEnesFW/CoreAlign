using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Products.Mapping;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ProductNotFoundException();

        if (!string.Equals(product.Sku, request.Sku, StringComparison.OrdinalIgnoreCase) &&
            await _productRepository.SkuExistsAsync(request.Sku, request.Id, cancellationToken))
        {
            throw new DuplicateProductSkuException();
        }

        product.Update(
            sku: request.Sku,
            barcode: request.Barcode,
            mpn: request.Mpn,
            name: request.Name,
            shortDescription: request.ShortDescription,
            description: request.Description,
            slug: request.Slug,
            brandId: request.BrandId,
            categoryId: request.CategoryId,
            parentProductId: request.ParentProductId,
            variantAttributesJson: request.VariantAttributesJson,
            tagsJson: request.TagsJson,
            unit: request.Unit,
            baseUomId: request.BaseUomId,
            purchaseUomId: request.PurchaseUomId,
            salesUomId: request.SalesUomId,
            listPrice: request.ListPrice,
            price: request.Price,
            minSellingPrice: request.MinSellingPrice,
            standardCost: request.StandardCost,
            currency: request.Currency,
            taxRateId: request.TaxRateId,
            isPriceTaxInclusive: request.IsPriceTaxInclusive,
            isStockTracked: request.IsStockTracked,
            isLotTracked: request.IsLotTracked,
            isSerialTracked: request.IsSerialTracked,
            minStock: request.MinStock,
            maxStock: request.MaxStock,
            reorderPoint: request.ReorderPoint,
            safetyStock: request.SafetyStock,
            leadTimeDays: request.LeadTimeDays,
            weightKg: request.WeightKg,
            widthCm: request.WidthCm,
            heightCm: request.HeightCm,
            depthCm: request.DepthCm,
            volumeM3: request.VolumeM3,
            status: request.Status,
            launchDate: request.LaunchDate,
            endOfLifeDate: request.EndOfLifeDate);

        product.SetProcurementType(request.ProcurementType);
        if (request.RequiresInspection.HasValue)
        {
            product.SetRequiresInspection(request.RequiresInspection.Value);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToDto(product);
    }
}
