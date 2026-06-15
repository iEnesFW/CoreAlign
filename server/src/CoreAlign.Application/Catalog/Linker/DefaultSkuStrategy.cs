using System.Text.RegularExpressions;

namespace CoreAlign.Application.Catalog.Linker;

public class DefaultSkuStrategy : ISkuStrategy
{
    private readonly ISkuTemplateProvider _templateProvider;
    private readonly ISkuTemplateCache _cache;

    public DefaultSkuStrategy(ISkuTemplateProvider templateProvider, ISkuTemplateCache cache)
    {
        _templateProvider = templateProvider;
        _cache = cache;
    }

    public string BuildSku(SkuContext context)
    {
        var templates = _cache.GetOrCreate(context.TenantId, () =>
            _templateProvider.GetForTenantAsync(context.TenantId).GetAwaiter().GetResult());

        var template = context.Kind switch
        {
            CatalogItemKind.Glass => templates.GlassTemplate,
            CatalogItemKind.Hardware => templates.HardwareTemplate,
            CatalogItemKind.Profile => templates.ProfileTemplate,
            CatalogItemKind.Mounting => templates.MountingTemplate,
            CatalogItemKind.Color => templates.ColorTemplate,
            CatalogItemKind.Connector => templates.ConnectorTemplate,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.Kind, "Unsupported catalog item kind.")
        };

        var normalizedCode = NormalizeCode(context.CatalogCode);
        var sku = template
            .Replace("{code}", normalizedCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{brand}", NormalizeCode(context.Brand ?? ""), StringComparison.OrdinalIgnoreCase);

        return sku.ToUpperInvariant();
    }

    private static string NormalizeCode(string code)
    {
        var stripped = Regex.Replace(code ?? "", @"[^A-Za-z0-9-_]", "");
        return stripped.Length > 32 ? stripped[..32] : stripped;
    }
}

public interface ISkuTemplateCache
{
    SkuTemplateSet GetOrCreate(Guid tenantId, Func<SkuTemplateSet> factory);
    void Invalidate(Guid tenantId);
}

public class InMemorySkuTemplateCache : ISkuTemplateCache
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SkuTemplateSet> _store = new();

    public SkuTemplateSet GetOrCreate(Guid tenantId, Func<SkuTemplateSet> factory)
        => _store.GetOrAdd(tenantId, _ => factory());

    public void Invalidate(Guid tenantId) => _store.TryRemove(tenantId, out _);
}
