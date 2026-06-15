namespace CoreAlign.Application.Reports.Common;

public enum ReportFormat
{
    Pdf,
    Xlsx,
}

public sealed record ReportFile(byte[] Content, string ContentType, string FileName);

public interface IReportFileFactory
{
    Task<ReportFile> RenderAsync(ReportDocument document, ReportFormat format, string reportKey, CancellationToken cancellationToken = default);
}

public sealed class ReportFileFactory : IReportFileFactory
{
    private readonly IEnumerable<IReportRenderer> _renderers;

    public ReportFileFactory(IEnumerable<IReportRenderer> renderers)
    {
        _renderers = renderers;
    }

    public async Task<ReportFile> RenderAsync(ReportDocument document, ReportFormat format, string reportKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var safeKey = string.IsNullOrWhiteSpace(reportKey) ? "report" : reportKey.Trim().Replace(' ', '-').ToLowerInvariant();
        var stamp = document.Header.GeneratedAtUtc.ToString("yyyyMMdd-HHmm");

        if (format == ReportFormat.Pdf)
        {
            foreach (var r in _renderers)
            {
                try
                {
                    var bytes = await r.RenderPdfAsync(document, cancellationToken);
                    return new ReportFile(bytes, "application/pdf", $"{safeKey}-{stamp}.pdf");
                }
                catch (NotSupportedException) { }
            }
            throw new InvalidOperationException("No registered IReportRenderer supports PDF rendering.");
        }

        foreach (var r in _renderers)
        {
            try
            {
                var bytes = await r.RenderXlsxAsync(document, cancellationToken);
                return new ReportFile(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{safeKey}-{stamp}.xlsx");
            }
            catch (NotSupportedException) { }
        }
        throw new InvalidOperationException("No registered IReportRenderer supports XLSX rendering.");
    }
}
