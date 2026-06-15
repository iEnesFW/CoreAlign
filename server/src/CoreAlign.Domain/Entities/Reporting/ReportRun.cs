using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Reporting;

public class ReportRun : TenantEntity
{
    public Guid SavedReportId { get; private set; }
    public Guid RanByUserId { get; private set; }
    public DateTime RanAtUtc { get; private set; }
    public int ResultRowCount { get; private set; }
    public BIExportFormat? ExportFormat { get; private set; }
    public long? DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }

    protected ReportRun() { }

    public ReportRun(
        Guid savedReportId,
        Guid ranByUserId,
        DateTime ranAtUtc,
        int resultRowCount,
        BIExportFormat? exportFormat,
        long? durationMs,
        string? errorMessage = null)
    {
        if (savedReportId == Guid.Empty)
        {
            throw new ArgumentException("SavedReportId is required.", nameof(savedReportId));
        }
        if (ranByUserId == Guid.Empty)
        {
            throw new ArgumentException("RanByUserId is required.", nameof(ranByUserId));
        }
        SavedReportId = savedReportId;
        RanByUserId = ranByUserId;
        RanAtUtc = DateTime.SpecifyKind(ranAtUtc, DateTimeKind.Utc);
        ResultRowCount = resultRowCount;
        ExportFormat = exportFormat;
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
    }
}
