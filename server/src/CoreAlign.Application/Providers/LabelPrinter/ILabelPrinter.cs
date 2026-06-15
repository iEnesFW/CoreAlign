namespace CoreAlign.Application.Providers.LabelPrinter;

public enum LabelPrinterFormat
{
    Zpl,
    EscPos,
    PdfRoll62x100,
    PdfA4WorkshopSheet
}

public sealed record LabelTemplate(
    string Code,
    LabelPrinterFormat Format,
    string BodyTemplate,
    int Width,
    int Height);

public sealed record LabelRenderResult(
    LabelPrinterFormat Format,
    byte[] RawBytes,
    string ContentType,
    int Bytes);

public interface ILabelPrinter : IExternalProvider
{
    LabelPrinterFormat Format { get; }

    Task<LabelRenderResult> RenderAsync(
        LabelTemplate template,
        IReadOnlyDictionary<string, object?> variables,
        CancellationToken ct);
}
