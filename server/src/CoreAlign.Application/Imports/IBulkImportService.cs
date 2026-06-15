using CoreAlign.Application.Common;

namespace CoreAlign.Application.Imports;

public enum BulkImportFileFormat
{
    Csv = 0,
    Xlsx = 1
}

public class BulkImportRowError
{
    public int RowNumber { get; init; }
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class BulkImportRowPreview<TRow>
{
    public int RowNumber { get; init; }
    public TRow? Row { get; init; }
    public IReadOnlyList<BulkImportRowError> Errors { get; init; } = Array.Empty<BulkImportRowError>();
    public bool IsValid => Errors.Count == 0;
}

public class BulkImportPreviewResult<TRow>
{
    public Guid SessionId { get; init; }
    public string EntityKind { get; init; } = string.Empty;
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BulkImportRowPreview<TRow>> Rows { get; init; } = Array.Empty<BulkImportRowPreview<TRow>>();
    public int TotalRowCount => Rows.Count;
    public int ValidRowCount => Rows.Count(r => r.IsValid);
    public int InvalidRowCount => Rows.Count(r => !r.IsValid);
}

public class BulkImportCommitResult
{
    public Guid SessionId { get; init; }
    public string EntityKind { get; init; } = string.Empty;
    public int AttemptedCount { get; init; }
    public int CommittedCount { get; init; }
    public int SkippedCount { get; init; }
    public IReadOnlyList<BulkImportRowError> Errors { get; init; } = Array.Empty<BulkImportRowError>();
}

public interface IBulkImportSessionStore
{
    Task<Guid> SaveAsync<TRow>(BulkImportPreviewResult<TRow> preview, CancellationToken cancellationToken = default);
    Task<BulkImportPreviewResult<TRow>?> GetAsync<TRow>(Guid sessionId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IBulkImportService<TRow>
{
    string EntityKind { get; }
    IReadOnlyList<string> ColumnHeaders { get; }
    Task<BulkImportPreviewResult<TRow>> PreviewAsync(
        Stream fileStream,
        BulkImportFileFormat format,
        CancellationToken cancellationToken = default);

    Task<BulkImportCommitResult> CommitAsync(
        Guid sessionId,
        bool skipInvalidRows,
        CancellationToken cancellationToken = default);
}
