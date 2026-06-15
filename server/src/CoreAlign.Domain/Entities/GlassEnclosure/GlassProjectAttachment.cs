using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectAttachment : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public GlassAttachmentKind Kind { get; private set; } = GlassAttachmentKind.Other;
    public string Url { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string? Caption { get; private set; }

    protected GlassProjectAttachment() { }

    public GlassProjectAttachment(
        Guid projectId,
        GlassAttachmentKind kind,
        string url,
        Guid uploadedByUserId,
        string? contentType = null,
        long sizeBytes = 0,
        string? caption = null)
    {
        ProjectId = projectId;
        Kind = kind;
        Url = url;
        UploadedByUserId = uploadedByUserId;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Caption = caption;
    }
}
