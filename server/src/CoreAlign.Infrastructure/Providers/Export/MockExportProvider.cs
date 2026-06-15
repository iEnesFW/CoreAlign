using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Providers.Export;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Infrastructure.Providers.Export.Mock;

public sealed class ExportFormatRegistry : IExportFormatRegistry
{
    private readonly IServiceProvider _services;

    public ExportFormatRegistry(IServiceProvider services)
    {
        _services = services;
    }

    public IExporter<TDoc>? Find<TDoc>(ExportFormat format)
    {
        var exporters = _services.GetServices<IExporter<TDoc>>();
        foreach (var exporter in exporters)
        {
            if (exporter.Format == format) return exporter;
        }
        return null;
    }

    public IReadOnlyList<ExportFormat> SupportedFormats<TDoc>()
    {
        var exporters = _services.GetServices<IExporter<TDoc>>();
        var list = new List<ExportFormat>();
        foreach (var exporter in exporters)
        {
            if (!list.Contains(exporter.Format)) list.Add(exporter.Format);
        }
        return list;
    }
}

public sealed class MockJsonExporter<TDoc> : IExporter<TDoc>
{
    public ExportFormat Format => ExportFormat.Json;

    public Task<ExportResult> ExportAsync(TDoc doc, ExportOptions opts, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = false });
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = string.IsNullOrWhiteSpace(opts.FileName)
            ? $"mock-{typeof(TDoc).Name}.json"
            : opts.FileName!;
        return Task.FromResult(new ExportResult(ExportFormat.Json, bytes, "application/json", fileName));
    }
}

public sealed class MockCsvExporter<TDoc> : IExporter<TDoc>
{
    public ExportFormat Format => ExportFormat.Csv;

    public Task<ExportResult> ExportAsync(TDoc doc, ExportOptions opts, CancellationToken ct)
    {
        var props = typeof(TDoc).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sb = new StringBuilder();
        if (opts.IncludeHeader)
        {
            sb.AppendLine(string.Join(',', props.Select(p => Escape(p.Name))));
        }
        var values = props.Select(p =>
        {
            var raw = p.GetValue(doc);
            var text = raw switch
            {
                null => string.Empty,
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty
            };
            return Escape(text);
        });
        sb.AppendLine(string.Join(',', values));

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = string.IsNullOrWhiteSpace(opts.FileName)
            ? $"mock-{typeof(TDoc).Name}.csv"
            : opts.FileName!;
        return Task.FromResult(new ExportResult(ExportFormat.Csv, bytes, "text/csv", fileName));
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
