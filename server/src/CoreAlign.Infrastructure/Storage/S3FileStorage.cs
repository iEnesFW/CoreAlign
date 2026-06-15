using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Storage;

public sealed class S3FileStorage : IFileStorage
{
    public const string ProviderName = "S3";

    private const string PackageMissingMessage =
        "AWSSDK.S3 is not referenced by CoreAlign.Infrastructure. " +
        "Set Storage:Provider=Local (default) to fall back to LocalFileSystemStorage, " +
        "or add the package per docs/sprint9-blockers.md.";

    private readonly S3StorageOptions _options;
    private readonly ITenantContext _tenantContext;

    public S3FileStorage(IOptions<StorageProviderOptions> options, ITenantContext tenantContext)
    {
        _options = options?.Value?.S3 ?? new S3StorageOptions();
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public Task<StoredFile> SaveAsync(
        string scope,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public Task<FileMetadata?> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(PackageMissingMessage);

    public string ResolvePublicUrl(string relativePath)
        => BuildPublicUrl(_options, _tenantContext.CurrentTenantId ?? Guid.Empty, relativePath);

    public string BuildStorageKey(string scope, string fileName)
    {
        if (string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Scope is required.", nameof(scope));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        var tenantSegment = (_tenantContext.CurrentTenantId ?? Guid.Empty).ToString("N");
        var safeScope = StorageKeySanitizer.SanitizeSegment(scope, nameof(scope));
        var safeName = StorageKeySanitizer.SanitizeSegment(fileName, nameof(fileName));
        return string.Join('/', tenantSegment, safeScope, safeName);
    }

    internal static string BuildPublicUrl(S3StorageOptions options, Guid tenantId, string relativePath)
    {
        var safePath = StorageKeySanitizer.SanitizeRelativePath(relativePath);
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return $"{options.PublicBaseUrl.TrimEnd('/')}/{safePath}";
        }
        var bucket = string.IsNullOrWhiteSpace(options.Bucket) ? "bucket" : options.Bucket;
        return $"s3://{bucket}/{safePath}";
    }
}
