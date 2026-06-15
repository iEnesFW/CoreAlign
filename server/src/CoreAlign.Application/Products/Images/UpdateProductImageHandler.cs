using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Images;

public sealed class UpdateProductImageHandler : IRequestHandler<UpdateProductImageCommand, ProductImageDto>
{
    private readonly IProductImageRepository _images;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public UpdateProductImageHandler(
        IProductImageRepository images,
        IFileStorage storage,
        IUnitOfWork uow)
    {
        _images = images;
        _storage = storage;
        _uow = uow;
    }

    public async Task<ProductImageDto> Handle(UpdateProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _images.GetByIdAsync(request.ImageId, cancellationToken)
            ?? throw new ProductImageNotFoundException();

        if (image.ProductId != request.ProductId)
        {
            throw new ProductImageNotFoundException();
        }

        if (request.IsPrimary && !image.IsPrimary)
        {
            var siblings = await _images.GetByProductAsync(request.ProductId, cancellationToken);
            foreach (var sibling in siblings.Where(s => s.IsPrimary && s.Id != image.Id))
            {
                sibling.MarkPrimary(false);
                _images.Update(sibling);
            }
        }

        image.UpdateMetadata(request.AltText, request.DisplayOrder, request.IsPrimary);
        _images.Update(image);

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
