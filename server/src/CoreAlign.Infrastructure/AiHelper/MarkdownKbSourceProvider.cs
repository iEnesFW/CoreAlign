using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Ingestion;
using CoreAlign.Domain.Entities.AiHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class MarkdownKbSourceProvider : IKbSourceProvider
{
    private const string NeutralLocale = "*";
    private static readonly string[] Locales = { "tr", "en" };

    private readonly AiHelperOptions _options;
    private readonly ILogger<MarkdownKbSourceProvider> _logger;

    public MarkdownKbSourceProvider(IOptions<AiHelperOptions> options, ILogger<MarkdownKbSourceProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<KbSourceDocument>> GetSourcesAsync(CancellationToken ct)
    {
        var root = _options.ContentRoot;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            _logger.LogWarning("AI Helper content root not configured or missing: {Root}", root);
            return Array.Empty<KbSourceDocument>();
        }

        var documents = new List<KbSourceDocument>();
        foreach (var locale in Locales)
        {
            await ScanDirectoryAsync(Path.Combine(root, locale), locale, documents, ct).ConfigureAwait(false);
        }

        await ScanDirectoryAsync(Path.Combine(root, "shared"), NeutralLocale, documents, ct).ConfigureAwait(false);
        return documents;
    }

    private static async Task ScanDirectoryAsync(
        string dir,
        string locale,
        List<KbSourceDocument> documents,
        CancellationToken ct)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var raw = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var fallbackTitle = Path.GetFileNameWithoutExtension(file);
            var (route, title, body) = Parse(raw, fallbackTitle);
            // WHY: SourceRef is the unique doc key (SourceType, SourceRef, Locale); multiple curated docs share a route,
            // so suffix the file slug to keep keys unique while the leading route still drives in-app navigation.
            var sourceRef = route is not null ? $"{route}#{fallbackTitle}" : $"help/{locale}/{fallbackTitle}";

            documents.Add(new KbSourceDocument(
                AiKbSourceType.Article,
                sourceRef,
                title,
                locale,
                AiKbScope.Public,
                null,
                body));
        }
    }

    private static (string? Route, string Title, string Body) Parse(string raw, string fallbackTitle)
    {
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        string? route = null;
        string? title = null;
        var bodyStart = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                bodyStart = i + 1;
                continue;
            }

            if (route is null && line.StartsWith("Route:", StringComparison.OrdinalIgnoreCase))
            {
                route = line["Route:".Length..].Trim();
                bodyStart = i + 1;
                continue;
            }

            if (title is null && line.StartsWith('#'))
            {
                title = line.TrimStart('#').Trim();
                bodyStart = i + 1;
                continue;
            }

            break;
        }

        var body = string.Join('\n', lines.Skip(bodyStart)).Trim();
        if (body.Length == 0)
        {
            body = raw.Trim();
        }

        return (route, title ?? fallbackTitle, body);
    }
}
