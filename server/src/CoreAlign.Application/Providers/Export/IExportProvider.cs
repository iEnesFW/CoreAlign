namespace CoreAlign.Application.Providers.Export;

public enum ExportFormat
{
    Csv,
    Xlsx,
    Pdf,
    Json,
    Xml
}

public sealed record ExportOptions(
    string LocaleCode,
    bool IncludeHeader,
    string? FileName);

public sealed record ExportResult(
    ExportFormat Format,
    byte[] RawBytes,
    string ContentType,
    string FileName);

public interface IExporter<TDoc>
{
    ExportFormat Format { get; }
    Task<ExportResult> ExportAsync(TDoc doc, ExportOptions opts, CancellationToken ct);
}

public interface IExportFormatRegistry
{
    IExporter<TDoc>? Find<TDoc>(ExportFormat format);
    IReadOnlyList<ExportFormat> SupportedFormats<TDoc>();
}
