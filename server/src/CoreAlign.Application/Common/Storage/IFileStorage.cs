namespace CoreAlign.Application.Common.Storage;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        string scope,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    string ResolvePublicUrl(string relativePath);

    Task<FileMetadata?> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
}

public record StoredFile(string RelativePath, string ContentType, long SizeBytes, string PublicUrl);

public record FileMetadata(Guid FileId, Guid TenantId, string StorageKey, string ContentType, long SizeBytes);
