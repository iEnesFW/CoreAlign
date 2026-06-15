using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Catalog.Linker;

public class CatalogProductLinker : ICatalogProductLinker
{
    private readonly ISkuStrategy _skuStrategy;
    private readonly IProductRepository _products;
    private readonly IGlassTypeRepository _glassTypes;
    private readonly IHardwareItemRepository _hardware;
    private readonly IProfileItemRepository _profiles;
    private readonly IProfileSystemRepository _profileSystems;
    private readonly ITenantContext _tenantContext;
    private readonly Dictionary<string, Product> _createdThisScope = new(StringComparer.OrdinalIgnoreCase);

    public CatalogProductLinker(
        ISkuStrategy skuStrategy,
        IProductRepository products,
        IGlassTypeRepository glassTypes,
        IHardwareItemRepository hardware,
        IProfileItemRepository profiles,
        IProfileSystemRepository profileSystems,
        ITenantContext tenantContext)
    {
        _skuStrategy = skuStrategy;
        _products = products;
        _glassTypes = glassTypes;
        _hardware = hardware;
        _profiles = profiles;
        _profileSystems = profileSystems;
        _tenantContext = tenantContext;
    }

    public async Task<LinkageResult> EnsureLinkedAsync(ICatalogLinkable catalogItem, CatalogItemKind kind, CancellationToken cancellationToken = default)
    {
        if (catalogItem is null) throw new ArgumentNullException(nameof(catalogItem));

        if (catalogItem.LinkedProductId is { } existingProductId && existingProductId != Guid.Empty)
        {
            var current = await _products.GetByIdAsync(existingProductId, cancellationToken);
            if (current is not null)
            {
                return new LinkageResult(catalogItem.Id, current.Id, current.Sku, ProductCreated: false, LinkUpdated: false);
            }
        }

        var tenantId = _tenantContext.RequireTenantId();
        var sku = _skuStrategy.BuildSku(new SkuContext(kind, catalogItem.Code, Brand: null, TenantId: tenantId));

        if (_createdThisScope.TryGetValue(sku, out var pending))
        {
            catalogItem.LinkedProductId = pending.Id;
            return new LinkageResult(catalogItem.Id, pending.Id, pending.Sku, ProductCreated: false, LinkUpdated: true);
        }

        var existingBySku = await _products.GetBySkuAsync(sku, cancellationToken);
        if (existingBySku is not null)
        {
            catalogItem.LinkedProductId = existingBySku.Id;
            return new LinkageResult(catalogItem.Id, existingBySku.Id, existingBySku.Sku, ProductCreated: false, LinkUpdated: true);
        }

        var newProduct = new Product(
            sku: sku,
            name: catalogItem.Name,
            unit: catalogItem.Unit,
            price: catalogItem.UnitCost);
        await _products.AddAsync(newProduct, cancellationToken);
        _createdThisScope[sku] = newProduct;
        catalogItem.LinkedProductId = newProduct.Id;
        return new LinkageResult(catalogItem.Id, newProduct.Id, newProduct.Sku, ProductCreated: true, LinkUpdated: true);
    }

    public async Task<LinkageReport> RunDryRunAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var unlinked = await CollectUnlinkedAsync(cancellationToken);
        var alreadyLinked = unlinked.AlreadyLinkedCount;
        var pending = unlinked.PendingItems;

        var proposed = pending
            .Select(p => new
            {
                Item = p.Item,
                Kind = p.Kind,
                Sku = _skuStrategy.BuildSku(new SkuContext(p.Kind, p.Item.Code, Brand: null, TenantId: tenantId))
            })
            .ToList();

        var skuLookup = await _products.GetBySkusAsync(proposed.Select(p => p.Sku), cancellationToken);

        var conflicts = new List<LinkageConflict>();
        var toBeLinked = 0;
        foreach (var p in proposed)
        {
            if (skuLookup.TryGetValue(p.Sku, out var existing))
            {
                conflicts.Add(new LinkageConflict(
                    CatalogItemId: p.Item.Id,
                    CatalogCode: p.Item.Code,
                    Kind: p.Kind,
                    ProposedSku: p.Sku,
                    ConflictingProductId: existing.Id,
                    ReasonKey: "sku.existing-product"));
            }
            else
            {
                toBeLinked++;
            }
        }

        return new LinkageReport(
            TotalCatalogItems: alreadyLinked + pending.Count,
            AlreadyLinked: alreadyLinked,
            ToBeLinked: toBeLinked,
            SkuConflicts: conflicts.Count,
            Conflicts: conflicts);
    }

    public async Task<int> BackfillAllAsync(CancellationToken cancellationToken = default)
    {
        var unlinked = await CollectUnlinkedAsync(cancellationToken);
        var linked = 0;
        foreach (var entry in unlinked.PendingItems)
        {
            var result = await EnsureLinkedAsync(entry.Item, entry.Kind, cancellationToken);
            if (result.LinkUpdated)
            {
                linked++;
            }
        }
        return linked;
    }

    private async Task<UnlinkedSnapshot> CollectUnlinkedAsync(CancellationToken cancellationToken)
    {
        var pending = new List<(ICatalogLinkable Item, CatalogItemKind Kind)>();
        var alreadyLinked = 0;

        var glassTypes = await _glassTypes.ListAsync(isActive: null, structure: null, cancellationToken: cancellationToken);
        foreach (var g in glassTypes)
        {
            if (g.LinkedProductId.HasValue && g.LinkedProductId.Value != Guid.Empty) alreadyLinked++;
            else pending.Add((g, CatalogItemKind.Glass));
        }

        var hardware = await _hardware.ListAsync(isActive: null, category: null, compatibleSystemId: null, cancellationToken: cancellationToken);
        foreach (var h in hardware)
        {
            if (h.LinkedProductId.HasValue && h.LinkedProductId.Value != Guid.Empty) alreadyLinked++;
            else pending.Add((h, CatalogItemKind.Hardware));
        }

        var systems = await _profileSystems.ListAsync(cancellationToken: cancellationToken);
        foreach (var system in systems)
        {
            var profileItems = await _profiles.ListBySystemAsync(system.Id, isActive: null, cancellationToken: cancellationToken);
            foreach (var pi in profileItems)
            {
                if (pi.LinkedProductId.HasValue && pi.LinkedProductId.Value != Guid.Empty) alreadyLinked++;
                else pending.Add((pi, CatalogItemKind.Profile));
            }
        }

        return new UnlinkedSnapshot(pending, alreadyLinked);
    }

    private sealed record UnlinkedSnapshot(
        IReadOnlyList<(ICatalogLinkable Item, CatalogItemKind Kind)> PendingItems,
        int AlreadyLinkedCount);
}
