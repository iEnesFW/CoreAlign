namespace CoreAlign.Application.Compliance.Audit;

public enum AuditLogExportFormat
{
    Csv = 1,
    Json = 2,
    Excel = 3,
}

public sealed record AuditLogExportFilter(
    DateTime? FromUtc,
    DateTime? ToUtc,
    IReadOnlyList<string>? EntityTypes,
    IReadOnlyList<string>? Actions,
    Guid? UserId,
    Guid? EntityId);

public sealed record AuditLogExportResult(byte[] Content, string ContentType, string FileName, int RowCount);

public interface IAuditLogExportService
{
    Task<AuditLogExportResult> ExportAsync(
        AuditLogExportFilter filter,
        AuditLogExportFormat format,
        CancellationToken cancellationToken = default);
}
