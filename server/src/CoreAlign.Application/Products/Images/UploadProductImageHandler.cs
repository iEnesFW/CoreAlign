using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Images;

public sealed class UploadProductImageHandler : IRequestHandler<UploadProductImageCommand, ProductImageDto>
{
    private readonly IProductRepository _products;
    private readonly IProductImageRepository _images;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public UploadProductImageHandler(
        IProductRepository products,
        IProductImageRepository images,
        IFileStorage storage,
        IUnitOfWork uow)
    {
        _products = products;
        _images = images;
        _storage = storage;
        _uow = uow;
    }

    public async Task<ProductImageDto> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        if (!ProductImagePolicy.IsAllowedContentType(request.ContentType))
        {
            throw new ArgumentException("Only JPG, PNG, or WebP images are allowed.", nameof(request));
        }

        if (!ProductImagePolicy.IsAllowedExtension(request.FileName))
        {
            throw new ArgumentException("File name must end with .jpg, .jpeg, .png, or .webp.", nameof(request));
        }

        if (!ProductImagePolicy.MatchesContentTypeAndExtension(request.ContentType, request.FileName))
        {
            throw new ArgumentException("File extension does not match the declared image type.", nameof(request));
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > ProductImagePolicy.MaxBytesPerImage)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"Image must be between 1 byte and {ProductImagePolicy.MaxBytesPerImage} bytes.");
        }

        if (!await ProductImagePolicy.LooksLikeImageAsync(request.Content, cancellationToken))
        {
            throw new ArgumentException("File content does not match a supported image format.", nameof(request));
        }

        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException();

        var existing = await _images.GetByProductAsync(product.Id, cancellationToken);
        if (existing.Count >= ProductImagePolicy.MaxImagesPerProduct)
        {
            throw new ProductImageLimitExceededException(ProductImagePolicy.MaxImagesPerProduct);
        }

        var stored = await _storage.SaveAsync(
            ProductImagePolicy.StorageScope,
            request.FileName,
            request.Content,
            request.ContentType,
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
            stored.RelativePath,
            stored.ContentType,
            stored.SizeBytes,
            request.AltText,
            nextOrder,
            makePrimary);

        await _images.AddAsync(image, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ProductImageDto(
            image.Id,
            image.ProductId,
            image.StorageKey,
            _storage.ResolvePublicUrl(image.StorageKey),
            image.ContentType,
            image.SizeBytes,
            image.AltText,
            image.DisplayOrder,
            image.IsPrimary,
            image.UploadedAtUtc);
    }
}
