using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Images;

public sealed class DeleteProductImageHandler : IRequestHandler<DeleteProductImageCommand, bool>
{
    private readonly IProductImageRepository _images;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public DeleteProductImageHandler(
        IProductImageRepository images,
        IFileStorage storage,
        IUnitOfWork uow)
    {
        _images = images;
        _storage = storage;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _images.GetByIdAsync(request.ImageId, cancellationToken)
            ?? throw new ProductImageNotFoundException();

        if (image.ProductId != request.ProductId)
        {
            throw new ProductImageNotFoundException();
        }

        var wasPrimary = image.IsPrimary;
        var storageKey = image.StorageKey;

        _images.Remove(image);

        if (wasPrimary)
        {
            var remaining = await _images.GetByProductAsync(request.ProductId, cancellationToken);
            var nextPrimary = remaining
                .Where(r => r.Id != image.Id)
                .OrderBy(r => r.DisplayOrder)
                .ThenBy(r => r.UploadedAtUtc)
                .FirstOrDefault();
            if (nextPrimary is not null)
            {
                nextPrimary.MarkPrimary(true);
                _images.Update(nextPrimary);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(storageKey, cancellationToken);
        return true;
    }
}
