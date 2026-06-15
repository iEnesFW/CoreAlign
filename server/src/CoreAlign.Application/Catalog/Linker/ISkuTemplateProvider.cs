namespace CoreAlign.Application.Catalog.Linker;

public interface ISkuTemplateProvider
{
    Task<SkuTemplateSet> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record SkuTemplateSet(
    string GlassTemplate,
    string HardwareTemplate,
    string ProfileTemplate,
    string MountingTemplate,
    string ColorTemplate,
    string ConnectorTemplate)
{
    public static SkuTemplateSet Default => new(
        GlassTemplate: "GE-GLASS-{code}",
        HardwareTemplate: "GE-HW-{code}",
        ProfileTemplate: "GE-PROF-{code}",
        MountingTemplate: "GE-MOUNT-{code}",
        ColorTemplate: "GE-COLOR-{code}",
        ConnectorTemplate: "GE-CONN-{code}");
}
