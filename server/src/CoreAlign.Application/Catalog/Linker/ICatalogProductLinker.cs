using CoreAlign.Domain.Common;

namespace CoreAlign.Application.Catalog.Linker;

public interface ICatalogProductLinker
{
    Task<LinkageResult> EnsureLinkedAsync(ICatalogLinkable catalogItem, CatalogItemKind kind, CancellationToken cancellationToken = default);
    Task<LinkageReport> RunDryRunAsync(CancellationToken cancellationToken = default);
    Task<int> BackfillAllAsync(CancellationToken cancellationToken = default);
}

public sealed record LinkageResult(
    Guid CatalogItemId,
    Guid ProductId,
    string Sku,
    bool ProductCreated,
    bool LinkUpdated);

public sealed record LinkageReport(
    int TotalCatalogItems,
    int AlreadyLinked,
    int ToBeLinked,
    int SkuConflicts,
    IReadOnlyList<LinkageConflict> Conflicts);

public sealed record LinkageConflict(
    Guid CatalogItemId,
    string CatalogCode,
    CatalogItemKind Kind,
    string ProposedSku,
    Guid ConflictingProductId,
    string ReasonKey);
