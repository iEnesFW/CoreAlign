using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Products.Images;

public sealed record UploadProductImageCommand(
    Guid ProductId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    string? AltText,
    bool MakePrimary) : IRequest<ProductImageDto>, ITransactionalRequest;

public sealed record UpdateProductImageCommand(
    Guid ProductId,
    Guid ImageId,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary) : IRequest<ProductImageDto>, ITransactionalRequest;

public sealed record DeleteProductImageCommand(
    Guid ProductId,
    Guid ImageId) : IRequest<bool>, ITransactionalRequest;

public sealed record ListProductImagesQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductImageDto>>;
