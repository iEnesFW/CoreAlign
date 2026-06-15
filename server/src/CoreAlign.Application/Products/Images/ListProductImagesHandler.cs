using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Images;

public sealed class ListProductImagesHandler : IRequestHandler<ListProductImagesQuery, IReadOnlyList<ProductImageDto>>
{
    private readonly IProductImageRepository _images;
    private readonly IFileStorage _storage;

    public ListProductImagesHandler(IProductImageRepository images, IFileStorage storage)
    {
        _images = images;
        _storage = storage;
    }

    public async Task<IReadOnlyList<ProductImageDto>> Handle(ListProductImagesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _images.GetByProductAsync(request.ProductId, cancellationToken);
        return rows
            .Select(image => new ProductImageDto(
                image.Id,
                image.ProductId,
                image.StorageKey,
                _storage.ResolvePublicUrl(image.StorageKey),
                image.ContentType,
                image.SizeBytes,
                image.AltText,
                image.DisplayOrder,
                image.IsPrimary,
                image.UploadedAtUtc))
            .ToList();
    }
}
