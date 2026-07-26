using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class FeedbackAttachment : TenantEntity
{
    public Guid FeedbackTicketId { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    // WHY: the upload service sanitises the stored name to {Guid:N}{ext}, so the user's own file name
    // has to be carried separately or every download is served as a GUID.
    public string DisplayFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public int DisplayOrder { get; private set; }

    protected FeedbackAttachment() { }

    public FeedbackAttachment(
        Guid feedbackTicketId,
        string storagePath,
        string displayFileName,
        string contentType,
        long sizeBytes,
        Guid? uploadedByUserId,
        int displayOrder)
    {
        FeedbackTicketId = feedbackTicketId;
        StoragePath = storagePath;
        DisplayFileName = displayFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedByUserId = uploadedByUserId;
        DisplayOrder = displayOrder;
    }
}
