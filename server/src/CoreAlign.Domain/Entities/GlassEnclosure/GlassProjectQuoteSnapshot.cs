using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectQuoteSnapshot : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public int SceneVersion { get; private set; }
    public string PdfUrl { get; private set; } = string.Empty;
    public decimal GrandTotal { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateTime IssuedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? ValidUntilUtc { get; private set; }
    public Guid IssuedByUserId { get; private set; }

    protected GlassProjectQuoteSnapshot() { }

    public GlassProjectQuoteSnapshot(
        Guid projectId,
        int sceneVersion,
        string pdfUrl,
        decimal grandTotal,
        string currency,
        Guid issuedByUserId,
        DateTime? validUntilUtc = null)
    {
        ProjectId = projectId;
        SceneVersion = sceneVersion;
        PdfUrl = pdfUrl;
        GrandTotal = grandTotal;
        Currency = currency;
        IssuedByUserId = issuedByUserId;
        ValidUntilUtc = validUntilUtc;
    }
}
