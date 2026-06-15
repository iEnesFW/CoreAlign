using System.Globalization;
using System.Text;
using CoreAlign.Application.BI;
using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Infrastructure.BI.Export;

public sealed class CsvExportProvider : IExportProvider
{
    public BIExportFormat Format => BIExportFormat.Csv;

    public Task<byte[]> ExportAsync(string title, BIResultDto result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', result.Columns.Select(c => EscapeCsv(c.Label))));
        foreach (var row in result.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = result.Columns.Select(c =>
            {
                if (!row.TryGetValue(c.Key, out var v) || v is null)
                {
                    return string.Empty;
                }
                if (v is IFormattable f)
                {
                    return EscapeCsv(f.ToString(null, CultureInfo.InvariantCulture));
                }
                return EscapeCsv(v.ToString() ?? string.Empty);
            });
            sb.AppendLine(string.Join(',', fields));
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(bytes);
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(',', StringComparison.Ordinal) || field.Contains('"', StringComparison.Ordinal) || field.Contains('\n', StringComparison.Ordinal))
        {
            return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }
        return field;
    }
}
