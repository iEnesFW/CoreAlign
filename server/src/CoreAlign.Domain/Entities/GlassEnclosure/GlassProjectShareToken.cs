using CoreAlign.Domain.Common;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectShareToken : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public int SceneVersion { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public int ViewCount { get; private set; }
    public DateTime? LastViewedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? SignatureImageUrl { get; private set; }

    protected GlassProjectShareToken() { }

    public GlassProjectShareToken(
        Guid projectId,
        int sceneVersion,
        string token,
        DateTime expiresAtUtc,
        Guid createdByUserId)
    {
        ProjectId = projectId;
        SceneVersion = sceneVersion;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public void RegisterView()
    {
        ViewCount += 1;
        LastViewedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Accept(string? signatureImageUrl)
    {
        AcceptedAtUtc = DateTime.UtcNow;
        SignatureImageUrl = signatureImageUrl;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new GlassProjectQuoteAcceptedEvent(TenantId, ProjectId, Token, signatureImageUrl, DateTime.UtcNow));
    }

    public void Reject(string? reason)
    {
        RejectedAtUtc = DateTime.UtcNow;
        RejectionReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new GlassProjectQuoteRejectedEvent(TenantId, ProjectId, Token, reason, DateTime.UtcNow));
    }
}
