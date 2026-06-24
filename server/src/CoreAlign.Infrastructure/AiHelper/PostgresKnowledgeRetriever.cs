using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Retrieval;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class PostgresKnowledgeRetriever : IKnowledgeRetriever
{
    private const string NeutralLocale = "*";

    private readonly CoreAlignDbContext _db;
    private readonly AiHelperOptions _options;

    public PostgresKnowledgeRetriever(CoreAlignDbContext db, IOptions<AiHelperOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> RetrieveAsync(RetrievalQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.QueryEmbedding.Length == 0 || query.MaxChunks <= 0)
        {
            return Array.Empty<KnowledgeChunk>();
        }

        var candidates = await ScopedChunks(query)
            .Select(c => new ChunkCandidate(c.Id, c.DocumentId, c.Document!.SourceType, c.Embedding))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (candidates.Count == 0)
        {
            return Array.Empty<KnowledgeChunk>();
        }

        var scored = new List<ScoredCandidate>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var raw = Cosine(query.QueryEmbedding, candidate.Embedding);
            if (raw <= 0 || raw < query.MinScore)
            {
                continue;
            }

            scored.Add(new ScoredCandidate(
                candidate.Id,
                candidate.DocumentId,
                candidate.Embedding,
                raw,
                raw * Weight(candidate.SourceType)));
        }

        if (scored.Count == 0)
        {
            return Array.Empty<KnowledgeChunk>();
        }

        scored.Sort((a, b) => b.Weighted.CompareTo(a.Weighted));
        var selected = SelectWithDiversity(scored, query.MaxChunks);

        var topIds = selected.Select(s => s.Id).ToList();
        var rows = await ScopedChunks(query)
            .Where(c => topIds.Contains(c.Id))
            .Select(c => new ChunkRow(
                c.Id,
                c.DocumentId,
                c.Document!.Title,
                c.Document.SourceRef,
                c.Document.SourceType,
                c.Content))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var rowById = rows.ToDictionary(r => r.Id);
        var result = new List<KnowledgeChunk>(selected.Count);
        foreach (var candidate in selected)
        {
            if (rowById.TryGetValue(candidate.Id, out var row))
            {
                result.Add(new KnowledgeChunk(
                    row.DocumentId,
                    row.Title,
                    row.SourceRef,
                    row.SourceType,
                    row.Content,
                    candidate.Raw));
            }
        }

        return result;
    }

    private double Weight(AiKbSourceType sourceType) =>
        _options.SourceTypeWeights.TryGetValue(sourceType.ToString(), out var weight) ? weight : 1.0;

    private List<ScoredCandidate> SelectWithDiversity(List<ScoredCandidate> scored, int maxChunks)
    {
        var lambda = _options.DiversityLambda;
        var capPerSource = _options.MaxChunksPerSource;
        var selected = new List<ScoredCandidate>(Math.Min(maxChunks, scored.Count));
        var selectedIds = new HashSet<Guid>();
        var perSource = new Dictionary<Guid, int>();

        while (selected.Count < maxChunks)
        {
            ScoredCandidate? best = null;
            var bestMmr = double.NegativeInfinity;
            foreach (var candidate in scored)
            {
                if (selectedIds.Contains(candidate.Id))
                {
                    continue;
                }

                if (capPerSource > 0
                    && perSource.TryGetValue(candidate.DocumentId, out var used)
                    && used >= capPerSource)
                {
                    continue;
                }

                var maxSim = 0.0;
                foreach (var picked in selected)
                {
                    var sim = Cosine(candidate.Embedding, picked.Embedding);
                    if (sim > maxSim)
                    {
                        maxSim = sim;
                    }
                }

                var mmr = (lambda * candidate.Weighted) - ((1.0 - lambda) * maxSim);
                if (mmr > bestMmr)
                {
                    bestMmr = mmr;
                    best = candidate;
                }
            }

            if (best is null)
            {
                break;
            }

            selected.Add(best);
            selectedIds.Add(best.Id);
            perSource[best.DocumentId] = perSource.GetValueOrDefault(best.DocumentId) + 1;
        }

        return selected;
    }

    private IQueryable<AiKbChunk> ScopedChunks(RetrievalQuery query)
    {
        var chunks = _db.Set<AiKbChunk>().AsNoTracking()
            .Where(c => c.Locale == query.Locale || c.Locale == NeutralLocale);
        if (query.TenantId.HasValue)
        {
            var tenantId = query.TenantId.Value;
            var roles = query.Roles;
            return chunks.Where(c =>
                c.Scope == AiKbScope.Public ||
                (c.Scope == AiKbScope.Tenant && c.TenantId == tenantId) ||
                (c.Scope == AiKbScope.Role && c.TenantId == tenantId
                    && c.RequiredRole != null && roles.Contains(c.RequiredRole)));
        }

        // WHY: anonymous (pre-login) callers must not see internal/technical content (module docs + source code).
        return chunks.Where(c =>
            c.Scope == AiKbScope.Public && c.Document!.SourceType != AiKbSourceType.ModuleDoc);
    }

    private static double Cosine(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0;
        }

        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA <= 0 || normB <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private sealed record ChunkCandidate(Guid Id, Guid DocumentId, AiKbSourceType SourceType, float[] Embedding);

    private sealed record ScoredCandidate(Guid Id, Guid DocumentId, float[] Embedding, double Raw, double Weighted);

    private sealed record ChunkRow(
        Guid Id,
        Guid DocumentId,
        string Title,
        string SourceRef,
        AiKbSourceType SourceType,
        string Content);
}
