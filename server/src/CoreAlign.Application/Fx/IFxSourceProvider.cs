using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Fx;

public interface IFxSourceProvider
{
    FxSource Source { get; }
    bool SupportsCurrency(string currencyCode);
    Task<FxRateSnapshot?> TryGetRateAsync(string currencyCode, DateTime asOfDate, CancellationToken ct = default);
}

public interface IFxRateResolver
{
    Task<FxRateSnapshot?> ResolveAsync(string currencyCode, DateTime asOfDate, Guid? tenantId, CancellationToken ct = default);
}

public sealed record FxResolutionResult(FxRateSnapshot Snapshot, FxSource Source, bool UsedTenantOverride);

public interface IFxRateResolverDetailed
{
    Task<FxResolutionResult?> ResolveDetailedAsync(string currencyCode, DateTime asOfDate, Guid? tenantId, CancellationToken ct = default);
}
