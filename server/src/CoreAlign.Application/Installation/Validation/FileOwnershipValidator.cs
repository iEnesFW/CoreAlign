using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Installation.Validation;

public interface IFileOwnershipValidator
{
    Task<bool> ValidateAcceptanceFileAsync(Guid fileId, Guid acceptanceId, CancellationToken cancellationToken = default);
}

public sealed class FileOwnershipValidator : IFileOwnershipValidator
{
    private readonly IFileStorage _fileStorage;
    private readonly ITenantContext _tenantContext;

    public FileOwnershipValidator(IFileStorage fileStorage, ITenantContext tenantContext)
    {
        _fileStorage = fileStorage;
        _tenantContext = tenantContext;
    }

    public async Task<bool> ValidateAcceptanceFileAsync(Guid fileId, Guid acceptanceId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty || acceptanceId == Guid.Empty) return false;

        var metadata = await _fileStorage.GetMetadataAsync(fileId, cancellationToken);
        if (metadata is null) return false;

        if (metadata.TenantId != _tenantContext.RequireTenantId()) return false;

        var expectedScope = $"installation-acceptance/{acceptanceId:N}";
        var tenantPrefix = $"{metadata.TenantId:N}/";
        var scopedKey = metadata.StorageKey.StartsWith(tenantPrefix, StringComparison.OrdinalIgnoreCase)
            ? metadata.StorageKey[tenantPrefix.Length..]
            : metadata.StorageKey;
        return scopedKey.StartsWith(expectedScope, StringComparison.OrdinalIgnoreCase);
    }
}
