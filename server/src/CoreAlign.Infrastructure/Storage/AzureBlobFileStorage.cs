using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Storage;

public sealed class AzureBlobFileStorage : IFileStorage
{
    public const string ProviderName = "AzureBlob";

    private const string PackageMissingMessage =
        "Azure.Storage.Blobs is not referenced by CoreAlign.Infrastructure. " +
        "Set Storage:Provider=Local (default) to fall back to LocalFileSystemStorage, " +
        "or add the package per docs/sprint9-blockers.md.";

    private readonly AzureBlobStorageOptions _options;
    private readonly ITenantContext _tenantContext;

    public AzureBlobFileStorage(IOptions<StorageProviderOptions> options, ITenantContext tenantContext)
    {
        _options = options?.Value?.AzureBlob ?? new AzureBlobStorageOptions();
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
        => BuildPublicUrl(_options, ResolveContainer(_options, _tenantContext.CurrentTenantId ?? Guid.Empty), relativePath);

    public string ResolveContainerName()
        => ResolveContainer(_options, _tenantContext.CurrentTenantId ?? Guid.Empty);

    internal static string ResolveContainer(AzureBlobStorageOptions options, Guid tenantId)
    {
        if (options.ContainerPerTenant)
        {
            return $"tenant-{tenantId:N}";
        }
        return string.IsNullOrWhiteSpace(options.Container) ? "corealign" : options.Container;
    }

    internal static string BuildPublicUrl(AzureBlobStorageOptions options, string container, string relativePath)
    {
        var safePath = StorageKeySanitizer.SanitizeRelativePath(relativePath);
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return $"{options.PublicBaseUrl.TrimEnd('/')}/{container}/{safePath}";
        }
        return $"azure://{container}/{safePath}";
    }
}
