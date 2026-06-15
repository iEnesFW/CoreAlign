using CoreAlign.Domain.Common;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectScene : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
    public string? Label { get; private set; }
    public byte[] SceneJsonCompressed { get; private set; } = Array.Empty<byte>();
    public string? ThumbnailUrl { get; private set; }
    public string? CameraStateJson { get; private set; }
    public Guid SavedByUserId { get; private set; }
    public DateTime SavedAtUtc { get; private set; } = DateTime.UtcNow;
    public bool IsCustomerApproved { get; private set; }
    public string? ApprovalSignatureUrl { get; private set; }

    protected GlassProjectScene() { }

    public GlassProjectScene(
        Guid projectId,
        int version,
        byte[] sceneJsonCompressed,
        Guid savedByUserId,
        string? thumbnailUrl = null,
        string? cameraStateJson = null,
        string? label = null)
    {
        ProjectId = projectId;
        Version = version;
        SceneJsonCompressed = sceneJsonCompressed;
        SavedByUserId = savedByUserId;
        ThumbnailUrl = thumbnailUrl;
        CameraStateJson = cameraStateJson;
        Label = label;
        AddDomainEvent(new GlassSceneVersionSavedEvent(TenantId, projectId, version, savedByUserId, DateTime.UtcNow));
    }

    public void MarkCustomerApproved(string? signatureUrl)
    {
        IsCustomerApproved = true;
        ApprovalSignatureUrl = signatureUrl;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
