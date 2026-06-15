namespace CoreAlign.Application.Products.Images;

public sealed record ProductImageDto(
    Guid Id,
    Guid ProductId,
    string StorageKey,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary,
    DateTime UploadedAtUtc);
