using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ProjectTemplateInstall : TenantEntity
{
    public Guid MarketplaceTemplateId { get; private set; }
    public Guid InstalledByUserId { get; private set; }
    public Guid InstalledTemplateId { get; private set; }
    public DateTime InstalledAtUtc { get; private set; } = DateTime.UtcNow;

    protected ProjectTemplateInstall() { }

    public ProjectTemplateInstall(
        Guid marketplaceTemplateId,
        Guid installedByUserId,
        Guid installedTemplateId)
    {
        MarketplaceTemplateId = marketplaceTemplateId;
        InstalledByUserId = installedByUserId;
        InstalledTemplateId = installedTemplateId;
        InstalledAtUtc = DateTime.UtcNow;
    }
}
