using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Compliance;

public sealed class AuditLogExportService : IAuditLogExportService
{
    private const int StreamBatchSize = 500;
    private const int MaxExportRows = 100_000;
    private const string CsvContentType = "text/csv; charset=utf-8";
    private const string JsonContentType = "application/json";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IEntityAuditLogRepository _repository;
    private readonly ITenantContext _tenant;
    private readonly IAuditFieldRedactor _redactor;

    public AuditLogExportService(IEntityAuditLogRepository repository, ITenantContext tenant, IAuditFieldRedactor redactor)
    {
        _repository = repository;
        _tenant = tenant;
        _redactor = redactor;
    }

    public async Task<AuditLogExportResult> ExportAsync(
        AuditLogExportFilter filter,
        AuditLogExportFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var tenantId = _tenant.RequireTenantId();
        var criteria = BuildCriteria(filter);
        var generatedAt = DateTime.UtcNow;

        return format switch
        {
            AuditLogExportFormat.Csv => await ExportCsvAsync(tenantId, criteria, generatedAt, cancellationToken),
            AuditLogExportFormat.Json => await ExportJsonAsync(tenantId, criteria, generatedAt, cancellationToken),
            AuditLogExportFormat.Excel => await ExportExcelAsync(tenantId, criteria, generatedAt, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported audit export format."),
        };
    }

    private static AuditLogSearchCriteria BuildCriteria(AuditLogExportFilter filter)
    {
        IReadOnlyList<EntityAuditAction>? actions = null;
        if (filter.Actions is { Count: > 0 })
        {
            var parsed = new List<EntityAuditAction>(filter.Actions.Count);
            foreach (var raw in filter.Actions)
            {
                if (Enum.TryParse<EntityAuditAction>(raw, ignoreCase: true, out var value))
                {
                    parsed.Add(value);
                }
            }
            if (parsed.Count > 0) actions = parsed;
        }
        return new AuditLogSearchCriteria(
            filter.FromUtc,
            filter.ToUtc,
            filter.EntityTypes,
            actions,
            filter.UserId,
            filter.EntityId);
    }

    private async Task<AuditLogExportResult> ExportCsvAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        DateTime generatedAt,
        CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, new UTF8Encoding(true), leaveOpen: true))
        {
            await writer.WriteLineAsync("Id,EntityType,EntityId,Action,UserId,ChangedAtUtc,Sequence,CorrelationId,RollingHash");
            var rowCount = 0;
            await foreach (var row in _repository.StreamAsync(tenantId, criteria, StreamBatchSize, cancellationToken))
            {
                if (rowCount >= MaxExportRows) break;
                await writer.WriteLineAsync(string.Join(',', new[]
                {
                    EscapeCsv(row.Id.ToString()),
                    EscapeCsv(row.EntityType),
                    EscapeCsv(row.EntityId.ToString()),
                    EscapeCsv(row.Action.ToString()),
                    EscapeCsv(row.UserId?.ToString() ?? string.Empty),
                    EscapeCsv(row.ChangedAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                    EscapeCsv(row.Sequence.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.CorrelationId?.ToString() ?? string.Empty),
                    EscapeCsv(row.RollingHash),
                }));
                rowCount++;
            }
            await writer.FlushAsync(cancellationToken);
            return new AuditLogExportResult(ms.ToArray(), CsvContentType, BuildFileName("csv", generatedAt), rowCount);
        }
    }

    private async Task<AuditLogExportResult> ExportJsonAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        DateTime generatedAt,
        CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartArray();
        var rowCount = 0;
        await foreach (var row in _repository.StreamAsync(tenantId, criteria, StreamBatchSize, cancellationToken))
        {
            if (rowCount >= MaxExportRows) break;
            JsonSerializer.Serialize(writer, EntityAuditLogMapper.ToDto(row, _redactor), JsonOptions);
            rowCount++;
        }
        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
        return new AuditLogExportResult(ms.ToArray(), JsonContentType, BuildFileName("json", generatedAt), rowCount);
    }

    private async Task<AuditLogExportResult> ExportExcelAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        DateTime generatedAt,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("AuditLog");
        WriteExcelHeader(sheet);

        var rowIndex = 2;
        var rowCount = 0;
        await foreach (var row in _repository.StreamAsync(tenantId, criteria, StreamBatchSize, cancellationToken))
        {
            if (rowCount >= MaxExportRows) break;
            sheet.Cell(rowIndex, 1).Value = row.Id.ToString();
            sheet.Cell(rowIndex, 2).Value = NeutralizeFormula(row.EntityType);
            sheet.Cell(rowIndex, 3).Value = row.EntityId.ToString();
            sheet.Cell(rowIndex, 4).Value = NeutralizeFormula(row.Action.ToString());
            sheet.Cell(rowIndex, 5).Value = row.UserId?.ToString() ?? string.Empty;
            sheet.Cell(rowIndex, 6).Value = row.ChangedAtUtc;
            sheet.Cell(rowIndex, 6).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            sheet.Cell(rowIndex, 7).Value = row.Sequence;
            sheet.Cell(rowIndex, 8).Value = row.CorrelationId?.ToString() ?? string.Empty;
            sheet.Cell(rowIndex, 9).Value = NeutralizeFormula(row.RollingHash);
            rowIndex++;
            rowCount++;
        }

        sheet.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return new AuditLogExportResult(ms.ToArray(), ExcelContentType, BuildFileName("xlsx", generatedAt), rowCount);
    }

    private static void WriteExcelHeader(IXLWorksheet sheet)
    {
        string[] headers = { "Id", "EntityType", "EntityId", "Action", "UserId", "ChangedAtUtc", "Sequence", "CorrelationId", "RollingHash" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
    }

    private static string BuildFileName(string extension, DateTime generatedAt)
    {
        var stamp = generatedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"audit-log-{stamp}.{extension}";
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        var needsQuoting = field.Contains(',', StringComparison.Ordinal)
            || field.Contains('"', StringComparison.Ordinal)
            || field.Contains('\n', StringComparison.Ordinal)
            || field.Contains('\r', StringComparison.Ordinal);
        var safe = NeutralizeFormula(field);
        if (!needsQuoting) return safe;
        return "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string NeutralizeFormula(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        var first = value[0];
        if (first == '=' || first == '+' || first == '-' || first == '@' || first == '\t')
        {
            return "'" + value;
        }
        return value;
    }
}
