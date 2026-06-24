using CoreAlign.Application.Common.Upload;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Images;

public sealed class UploadProductImageHandler : IRequestHandler<UploadProductImageCommand, ProductImageDto>
{
    private readonly IProductRepository _products;
    private readonly IProductImageRepository _images;
    private readonly IFileUploadService _uploads;
    private readonly IUnitOfWork _uow;

    public UploadProductImageHandler(
        IProductRepository products,
        IProductImageRepository images,
        IFileUploadService uploads,
        IUnitOfWork uow)
    {
        _products = products;
        _images = images;
        _uploads = uploads;
        _uow = uow;
    }

    public async Task<ProductImageDto> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException();

        var existing = await _images.GetByProductAsync(product.Id, cancellationToken);
        if (existing.Count >= ProductImagePolicy.MaxImagesPerProduct)
        {
            throw new ProductImageLimitExceededException(ProductImagePolicy.MaxImagesPerProduct);
        }

        var uploaded = await _uploads.UploadAsync(
            new FileUploadRequest(
                request.Content,
                request.FileName,
                request.ContentType,
                FileUploadProfiles.ProductImage.Name,
                ProductImagePolicy.StorageScope),
            cancellationToken);

        var nextOrder = existing.Count == 0 ? 0 : existing.Max(i => i.DisplayOrder) + 1;
        var isFirst = existing.Count == 0;
        var makePrimary = request.MakePrimary || isFirst;

        if (makePrimary)
        {
            foreach (var current in existing.Where(i => i.IsPrimary))
            {
                current.MarkPrimary(false);
                _images.Update(current);
            }
        }

        var image = new ProductImage(
            product.Id,
            uploaded.RelativePath,
            uploaded.ContentType,
            uploaded.SizeBytes,
            request.AltText,
            nextOrder,
            makePrimary);

        await _images.AddAsync(image, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ProductImageDto(
            image.Id,
            image.ProductId,
            image.StorageKey,
            uploaded.PublicUrl,
            image.ContentType,
            image.SizeBytes,
            image.AltText,
            image.DisplayOrder,
            image.IsPrimary,
            image.UploadedAtUtc);
    }
}
