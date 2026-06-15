using CoreAlign.Application.B2B;
using CoreAlign.Application.Imports;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CoreAlign.Infrastructure.Services;

public class BulkImportSessionStore : IBulkImportSessionStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly IMemoryCache _cache;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserAccessor _currentUser;

    public BulkImportSessionStore(IMemoryCache cache, ITenantContext tenant, ICurrentUserAccessor currentUser)
    {
        _cache = cache;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public Task<Guid> SaveAsync<TRow>(BulkImportPreviewResult<TRow> preview, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(preview.SessionId);
        _cache.Set(key, preview, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl,
            Size = 1
        });
        return Task.FromResult(preview.SessionId);
    }

    public Task<BulkImportPreviewResult<TRow>?> GetAsync<TRow>(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(sessionId);
        _cache.TryGetValue(key, out var stored);
        return Task.FromResult(stored as BulkImportPreviewResult<TRow>);
    }

    public Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(BuildKey(sessionId));
        return Task.CompletedTask;
    }

    private string BuildKey(Guid sessionId)
    {
        var tenantId = _tenant.CurrentTenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        return $"bulk-import:{tenantId:N}:{userId:N}:{sessionId:N}";
    }
}
