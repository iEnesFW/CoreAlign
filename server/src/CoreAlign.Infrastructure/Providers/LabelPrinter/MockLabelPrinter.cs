using System.Globalization;
using System.Text;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.LabelPrinter;

namespace CoreAlign.Infrastructure.Providers.LabelPrinter.Mock;

public sealed class MockLabelPrinter : ILabelPrinter
{
    public string Name => "mock";

    public string DisplayName => "Mock Label Printer";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["env"] = "dev" });

    public LabelPrinterFormat Format => LabelPrinterFormat.PdfRoll62x100;

    public Task<LabelRenderResult> RenderAsync(
        LabelTemplate template,
        IReadOnlyDictionary<string, object?> variables,
        CancellationToken ct)
    {
        var serialized = string.Join(
            ",",
            variables
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={Convert.ToString(kvp.Value, CultureInfo.InvariantCulture) ?? string.Empty}"));

        var rawBytes = Encoding.ASCII.GetBytes($"MOCK LABEL: {serialized}");

        var result = new LabelRenderResult(
            template.Format,
            rawBytes,
            "application/octet-stream",
            rawBytes.Length);

        return Task.FromResult(result);
    }
}
