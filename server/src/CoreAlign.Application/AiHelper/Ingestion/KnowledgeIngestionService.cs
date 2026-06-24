using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper.Providers;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.AiHelper.Ingestion;

public sealed class KnowledgeIngestionService : IKnowledgeIngestionService
{
    private const string IngestionVersionSalt = "\n##ingest:v2-context";

    private readonly IEnumerable<IKbSourceProvider> _sourceProviders;
    private readonly IAiEmbeddingProvider _embeddingProvider;
    private readonly IAiKbRepository _repository;
    private readonly ILogger<KnowledgeIngestionService> _logger;

    public KnowledgeIngestionService(
        IEnumerable<IKbSourceProvider> sourceProviders,
        IAiEmbeddingProvider embeddingProvider,
        IAiKbRepository repository,
        ILogger<KnowledgeIngestionService> logger)
    {
        _sourceProviders = sourceProviders;
        _embeddingProvider = embeddingProvider;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IngestionResult> ReindexAsync(CancellationToken ct)
    {
        var sources = new List<KbSourceDocument>();
        foreach (var provider in _sourceProviders)
        {
            sources.AddRange(await provider.GetSourcesAsync(ct).ConfigureAwait(false));
        }

        var documentCount = 0;
        var chunkCount = 0;
        var skippedCount = 0;

        foreach (var source in sources)
        {
            ct.ThrowIfCancellationRequested();

            var hash = ComputeContentHash(source.Content);
            var existing = await _repository
                .FindAsync(source.SourceType, source.SourceRef, source.Locale, ct)
                .ConfigureAwait(false);
            if (existing is not null && existing.ContentHash == hash)
            {
                skippedCount++;
                continue;
            }

            var contextualChunks = TextChunker.ChunkWithContext(source.Content);
            if (contextualChunks.Count == 0)
            {
                skippedCount++;
                continue;
            }

            var embedTexts = new List<string>(contextualChunks.Count);
            foreach (var chunk in contextualChunks)
            {
                embedTexts.Add(ComposeEmbedText(source.Title, chunk.HeadingPath, chunk.Body));
            }

            var embeddings = await _embeddingProvider.EmbedAsync(embedTexts, ct).ConfigureAwait(false);
            if (embeddings.Count != contextualChunks.Count)
            {
                _logger.LogWarning(
                    "Embedding count {Returned} != chunk count {Expected} for {SourceRef}; skipping",
                    embeddings.Count,
                    contextualChunks.Count,
                    source.SourceRef);
                skippedCount++;
                continue;
            }

            if (existing is not null)
            {
                await _repository.RemoveAsync(existing, ct).ConfigureAwait(false);
            }

            var document = new AiKbDocument
            {
                SourceType = source.SourceType,
                SourceRef = source.SourceRef,
                Title = source.Title,
                Locale = source.Locale,
                Scope = source.Scope,
                TenantId = source.TenantId,
                RequiredRole = source.RequiredRole,
                ContentHash = hash,
            };

            for (var i = 0; i < contextualChunks.Count; i++)
            {
                document.Chunks.Add(new AiKbChunk
                {
                    Ordinal = i,
                    Content = ComposeStoredContent(contextualChunks[i].HeadingPath, contextualChunks[i].Body),
                    Embedding = embeddings[i],
                    Locale = source.Locale,
                    Scope = source.Scope,
                    TenantId = source.TenantId,
                    RequiredRole = source.RequiredRole,
                    TokenCount = Math.Max(1, embedTexts[i].Length / 4),
                });
            }

            await _repository.AddAsync(document, ct).ConfigureAwait(false);
            await _repository.SaveChangesAsync(ct).ConfigureAwait(false);
            documentCount++;
            chunkCount += contextualChunks.Count;
        }

        _logger.LogInformation(
            "AI Helper reindex complete: {Docs} docs, {Chunks} chunks, {Skipped} skipped",
            documentCount,
            chunkCount,
            skippedCount);

        return new IngestionResult(documentCount, chunkCount, skippedCount);
    }

    public static string ComputeContentHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content + IngestionVersionSalt));
        return Convert.ToHexString(bytes);
    }

    private static string ComposeEmbedText(string title, string headingPath, string body)
    {
        var header = ComposeContextHeader(title, headingPath);
        return header.Length > 0 ? header + "\n\n" + body : body;
    }

    private static string ComposeStoredContent(string headingPath, string body) =>
        headingPath.Length > 0 ? headingPath + "\n\n" + body : body;

    private static string ComposeContextHeader(string title, string headingPath)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return headingPath;
        }

        if (headingPath.Length == 0
            || headingPath.Equals(title, StringComparison.OrdinalIgnoreCase)
            || headingPath.StartsWith(title + " › ", StringComparison.OrdinalIgnoreCase))
        {
            return headingPath.Length == 0 ? title : headingPath;
        }

        return title + " › " + headingPath;
    }
}
