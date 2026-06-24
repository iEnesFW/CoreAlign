using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Sso;

public class ExternalUserBinding : TenantEntity, IHasConcurrencyToken
{
    public Guid LocalUserId { get; private set; }
    public Guid IdentityProviderId { get; private set; }
    public string ExternalUserId { get; private set; } = string.Empty;
    public string? ExternalEmail { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected ExternalUserBinding() { }

    public static ExternalUserBinding Create(
        Guid tenantId,
        Guid localUserId,
        Guid identityProviderId,
        string externalUserId,
        string? externalEmail)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (localUserId == Guid.Empty) throw new ArgumentException("LocalUserId required.", nameof(localUserId));
        if (identityProviderId == Guid.Empty) throw new ArgumentException("IdentityProviderId required.", nameof(identityProviderId));
        if (string.IsNullOrWhiteSpace(externalUserId)) throw new ArgumentException("ExternalUserId required.", nameof(externalUserId));

        return new ExternalUserBinding
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            LocalUserId = localUserId,
            IdentityProviderId = identityProviderId,
            ExternalUserId = externalUserId.Trim(),
            ExternalEmail = externalEmail?.Trim(),
        };
    }

    public void RecordLogin(DateTime utcNow)
    {
        LastLoginAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
