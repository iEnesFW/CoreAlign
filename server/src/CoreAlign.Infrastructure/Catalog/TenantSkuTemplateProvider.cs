using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Catalog;

public class TenantSkuTemplateProvider : ISkuTemplateProvider
{
    private readonly ITenantSettingRepository _settings;

    public TenantSkuTemplateProvider(ITenantSettingRepository settings) => _settings = settings;

    public async Task<SkuTemplateSet> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var glass = await ReadAsync("sku.glass.template", SkuTemplateSet.Default.GlassTemplate, cancellationToken);
        var hardware = await ReadAsync("sku.hardware.template", SkuTemplateSet.Default.HardwareTemplate, cancellationToken);
        var profile = await ReadAsync("sku.profile.template", SkuTemplateSet.Default.ProfileTemplate, cancellationToken);
        var mounting = await ReadAsync("sku.mounting.template", SkuTemplateSet.Default.MountingTemplate, cancellationToken);
        var color = await ReadAsync("sku.color.template", SkuTemplateSet.Default.ColorTemplate, cancellationToken);
        var connector = await ReadAsync("sku.connector.template", SkuTemplateSet.Default.ConnectorTemplate, cancellationToken);
        return new SkuTemplateSet(glass, hardware, profile, mounting, color, connector);
    }

    private async Task<string> ReadAsync(string key, string fallback, CancellationToken cancellationToken)
    {
        var setting = await _settings.GetAsync("catalog", key, cancellationToken);
        return setting?.Value ?? fallback;
    }
}
