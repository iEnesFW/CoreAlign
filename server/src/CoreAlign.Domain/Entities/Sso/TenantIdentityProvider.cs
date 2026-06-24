using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Sso;

public enum SsoProtocol
{
    Saml = 0,
    Oidc = 1,
}

public class TenantIdentityProvider : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public SsoProtocol Protocol { get; private set; }
    public string EntityIdOrClientId { get; private set; } = string.Empty;
    public string? MetadataUrl { get; private set; }
    public string? DiscoveryDocumentUrl { get; private set; }
    public string? ClientSecretEncrypted { get; private set; }
    public string AttributeMappingsJson { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;
    public DateTime? LastUsedAtUtc { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    protected TenantIdentityProvider() { }

    public static TenantIdentityProvider Create(
        Guid tenantId,
        string name,
        SsoProtocol protocol,
        string entityIdOrClientId,
        string? metadataUrl,
        string? discoveryDocumentUrl,
        string? clientSecretEncrypted,
        string? attributeMappingsJson)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));
        if (string.IsNullOrWhiteSpace(entityIdOrClientId)) throw new ArgumentException("EntityId/ClientId required.", nameof(entityIdOrClientId));

        if (protocol == SsoProtocol.Saml && string.IsNullOrWhiteSpace(metadataUrl))
        {
            throw new ArgumentException("MetadataUrl required for SAML.", nameof(metadataUrl));
        }
        if (protocol == SsoProtocol.Oidc && string.IsNullOrWhiteSpace(discoveryDocumentUrl))
        {
            throw new ArgumentException("DiscoveryDocumentUrl required for OIDC.", nameof(discoveryDocumentUrl));
        }

        return new TenantIdentityProvider
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Name = name.Trim(),
            Protocol = protocol,
            EntityIdOrClientId = entityIdOrClientId.Trim(),
            MetadataUrl = metadataUrl?.Trim(),
            DiscoveryDocumentUrl = discoveryDocumentUrl?.Trim(),
            ClientSecretEncrypted = clientSecretEncrypted,
            AttributeMappingsJson = string.IsNullOrWhiteSpace(attributeMappingsJson) ? "{}" : attributeMappingsJson,
            IsActive = true,
        };
    }

    public void Update(
        string name,
        string entityIdOrClientId,
        string? metadataUrl,
        string? discoveryDocumentUrl,
        string? clientSecretEncrypted,
        string? attributeMappingsJson,
        bool isActive,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));
        if (string.IsNullOrWhiteSpace(entityIdOrClientId)) throw new ArgumentException("EntityId/ClientId required.", nameof(entityIdOrClientId));

        Name = name.Trim();
        EntityIdOrClientId = entityIdOrClientId.Trim();
        MetadataUrl = metadataUrl?.Trim();
        DiscoveryDocumentUrl = discoveryDocumentUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(clientSecretEncrypted))
        {
            ClientSecretEncrypted = clientSecretEncrypted;
        }
        AttributeMappingsJson = string.IsNullOrWhiteSpace(attributeMappingsJson) ? "{}" : attributeMappingsJson;
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }

    public void RecordUsage(DateTime utcNow)
    {
        LastUsedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
