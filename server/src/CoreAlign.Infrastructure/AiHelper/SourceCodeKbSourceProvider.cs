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

public sealed class SourceCodeKbSourceProvider : IKbSourceProvider
{
    private const string NeutralLocale = "*";

    private readonly AiHelperOptions _options;
    private readonly ILogger<SourceCodeKbSourceProvider> _logger;

    public SourceCodeKbSourceProvider(IOptions<AiHelperOptions> options, ILogger<SourceCodeKbSourceProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<KbSourceDocument>> GetSourcesAsync(CancellationToken ct)
    {
        if (!_options.IngestSourceCode)
        {
            return Array.Empty<KbSourceDocument>();
        }

        var root = ResolveRoot();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            _logger.LogWarning("AI Helper source code root not found: {Root}", root);
            return Array.Empty<KbSourceDocument>();
        }

        var extensions = _options.SourceCodeExtensions;
        var excludes = _options.SourceCodeExcludes.Select(e => e.ToLowerInvariant()).ToArray();
        var documents = new List<KbSourceDocument>();

        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            if (Array.IndexOf(extensions, Path.GetExtension(file)) < 0)
            {
                continue;
            }

            var normalized = file.Replace('/', '\\').ToLowerInvariant();
            if (excludes.Any(normalized.Contains))
            {
                continue;
            }

            if (new FileInfo(file).Length > _options.MaxIngestFileBytes)
            {
                continue;
            }

            var raw = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            documents.Add(new KbSourceDocument(
                AiKbSourceType.SourceCode,
                $"code/{relative}",
                relative,
                NeutralLocale,
                AiKbScope.Public,
                null,
                raw));
        }

        _logger.LogInformation("AI Helper source code provider collected {Count} files from {Root}", documents.Count, root);
        return documents;
    }

    private string ResolveRoot()
    {
        if (!string.IsNullOrWhiteSpace(_options.SourceCodeRoot))
        {
            return _options.SourceCodeRoot;
        }

        if (string.IsNullOrWhiteSpace(_options.ContentRoot))
        {
            return string.Empty;
        }

        return Path.GetFullPath(Path.Combine(_options.ContentRoot, "..", ".."));
    }
}
