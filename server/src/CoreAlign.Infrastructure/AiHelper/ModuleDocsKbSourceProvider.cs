using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Ingestion;
using CoreAlign.Domain.Entities.AiHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class ModuleDocsKbSourceProvider : IKbSourceProvider
{
    private const string NeutralLocale = "*";

    private readonly AiHelperOptions _options;
    private readonly ILogger<ModuleDocsKbSourceProvider> _logger;

    public ModuleDocsKbSourceProvider(IOptions<AiHelperOptions> options, ILogger<ModuleDocsKbSourceProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<KbSourceDocument>> GetSourcesAsync(CancellationToken ct)
    {
        if (!_options.IngestModuleDocs)
        {
            return Array.Empty<KbSourceDocument>();
        }

        var root = ResolveRoot();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            _logger.LogWarning("AI Helper module docs root not found: {Root}", root);
            return Array.Empty<KbSourceDocument>();
        }

        var documents = new List<KbSourceDocument>();
        foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var raw = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(file);
            documents.Add(new KbSourceDocument(
                AiKbSourceType.ModuleDoc,
                $"doc/{name}",
                ExtractTitle(raw, name),
                NeutralLocale,
                AiKbScope.Public,
                null,
                raw.Trim()));
        }

        return documents;
    }

    private string ResolveRoot()
    {
        if (!string.IsNullOrWhiteSpace(_options.ModuleDocsRoot))
        {
            return _options.ModuleDocsRoot;
        }

        if (string.IsNullOrWhiteSpace(_options.ContentRoot))
        {
            return string.Empty;
        }

        return Path.GetFullPath(Path.Combine(_options.ContentRoot, "..", "modules"));
    }

    private static string ExtractTitle(string raw, string fallback)
    {
        foreach (var line in raw.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#'))
            {
                return trimmed.TrimStart('#').Trim();
            }

            if (trimmed.Length > 0)
            {
                break;
            }
        }

        return fallback;
    }
}
