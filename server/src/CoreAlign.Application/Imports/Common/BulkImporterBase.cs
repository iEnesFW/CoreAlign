namespace CoreAlign.Application.Imports.Common;

public abstract class BulkImporterBase<TRow> : IBulkImportService<TRow>
{
    private readonly IBulkImportRowReader _reader;
    private readonly IBulkImportSessionStore _sessions;

    protected BulkImporterBase(IBulkImportRowReader reader, IBulkImportSessionStore sessions)
    {
        _reader = reader;
        _sessions = sessions;
    }

    public abstract string EntityKind { get; }
    public abstract IReadOnlyList<string> ColumnHeaders { get; }

    public async Task<BulkImportPreviewResult<TRow>> PreviewAsync(
        Stream fileStream,
        BulkImportFileFormat format,
        CancellationToken cancellationToken = default)
    {
        var raw = _reader.Read(fileStream, format);
        var rows = new List<BulkImportRowPreview<TRow>>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var rowNumber = i + 2;
            var mapped = MapRaw(raw[i]);
            var errors = ValidateRow(mapped, rowNumber);
            rows.Add(new BulkImportRowPreview<TRow>
            {
                RowNumber = rowNumber,
                Row = mapped,
                Errors = errors
            });
        }

        var preview = new BulkImportPreviewResult<TRow>
        {
            SessionId = Guid.NewGuid(),
            EntityKind = EntityKind,
            Headers = ColumnHeaders,
            Rows = rows
        };
        await _sessions.SaveAsync(preview, cancellationToken);
        return preview;
    }

    public async Task<BulkImportCommitResult> CommitAsync(
        Guid sessionId,
        bool skipInvalidRows,
        CancellationToken cancellationToken = default)
    {
        var preview = await _sessions.GetAsync<TRow>(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Bulk import session expired or not found.");
        if (preview.EntityKind != EntityKind)
        {
            throw new InvalidOperationException(
                $"Session entity kind '{preview.EntityKind}' does not match importer '{EntityKind}'.");
        }

        if (!skipInvalidRows && preview.InvalidRowCount > 0)
        {
            return new BulkImportCommitResult
            {
                SessionId = sessionId,
                EntityKind = EntityKind,
                AttemptedCount = 0,
                CommittedCount = 0,
                SkippedCount = preview.InvalidRowCount,
                Errors = preview.Rows.SelectMany(r => r.Errors).ToList()
            };
        }

        var committed = 0;
        var skipped = 0;
        var attempted = 0;
        var errors = new List<BulkImportRowError>();

        foreach (var row in preview.Rows)
        {
            if (!row.IsValid)
            {
                skipped++;
                errors.AddRange(row.Errors);
                continue;
            }
            if (row.Row is null)
            {
                continue;
            }
            attempted++;
            if (skipInvalidRows)
            {
                try
                {
                    if (await CommitRowAsync(row.Row, cancellationToken))
                    {
                        committed++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new BulkImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Field = "_row",
                        Message = ex.Message
                    });
                }
            }
            else
            {
                if (await CommitRowAsync(row.Row, cancellationToken))
                {
                    committed++;
                }
            }
        }

        await _sessions.RemoveAsync(sessionId, cancellationToken);

        return new BulkImportCommitResult
        {
            SessionId = sessionId,
            EntityKind = EntityKind,
            AttemptedCount = attempted,
            CommittedCount = committed,
            SkippedCount = skipped,
            Errors = errors
        };
    }

    protected abstract TRow MapRaw(IReadOnlyDictionary<string, string> raw);
    protected abstract IReadOnlyList<BulkImportRowError> ValidateRow(TRow row, int rowNumber);
    protected abstract Task<bool> CommitRowAsync(TRow row, CancellationToken cancellationToken);
}
