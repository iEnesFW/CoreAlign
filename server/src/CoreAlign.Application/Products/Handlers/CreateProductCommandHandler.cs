using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Products.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await _productRepository.SkuExistsAsync(request.Sku, null, cancellationToken))
        {
            throw new DuplicateProductSkuException();
        }

        var product = new Product(
            sku: request.Sku,
            name: request.Name,
            unit: request.Unit,
            price: request.Price,
            currency: request.Currency,
            initialStock: request.StockQuantity,
            description: request.Description);

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
            listPrice: request.ListPrice == 0m ? request.Price : request.ListPrice,
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

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToDto(product);
    }
}
