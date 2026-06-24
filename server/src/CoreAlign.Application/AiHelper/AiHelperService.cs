using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper.Providers;
using CoreAlign.Application.AiHelper.Retrieval;
using CoreAlign.Application.AiHelper.Tools;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.AiHelper;

public sealed class AiHelperService : IAiHelperService
{
    private readonly IAiEmbeddingProvider _embeddingProvider;
    private readonly IKnowledgeRetriever _retriever;
    private readonly IAiChatProvider _chatProvider;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly IAiHelperConversationReader _conversation;
    private readonly IAiHelperTraceWriter _trace;
    private readonly AiHelperOptions _options;

    public AiHelperService(
        IAiEmbeddingProvider embeddingProvider,
        IKnowledgeRetriever retriever,
        IAiChatProvider chatProvider,
        IAiToolRegistry toolRegistry,
        IAiHelperConversationReader conversation,
        IAiHelperTraceWriter trace,
        IOptions<AiHelperOptions> options)
    {
        _embeddingProvider = embeddingProvider;
        _retriever = retriever;
        _chatProvider = chatProvider;
        _toolRegistry = toolRegistry;
        _conversation = conversation;
        _trace = trace;
        _options = options.Value;
    }

    public IAsyncEnumerable<AiHelperEvent> AskAsync(AiHelperQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return AskInternalAsync(query, ct);
    }

    private async IAsyncEnumerable<AiHelperEvent> AskInternalAsync(
        AiHelperQuery query,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var answerId = Guid.CreateVersion7();
        var embeddings = await _embeddingProvider
            .EmbedAsync(new[] { query.Question }, ct)
            .ConfigureAwait(false);
        var queryEmbedding = embeddings.Count > 0 ? embeddings[0] : Array.Empty<float>();

        IReadOnlyList<KnowledgeChunk> chunks = Array.Empty<KnowledgeChunk>();
        if (queryEmbedding.Length > 0)
        {
            var tenantScope = query.IsPublic ? (Guid?)null : query.TenantId;
            var roles = query.IsPublic ? (IReadOnlyList<string>)Array.Empty<string>() : query.Roles;
            chunks = await _retriever
                .RetrieveAsync(
                    new RetrievalQuery(
                        queryEmbedding,
                        query.Locale,
                        tenantScope,
                        roles,
                        _options.MaxContextChunks,
                        _options.MinRelevanceScore),
                    ct)
                .ConfigureAwait(false);
        }

        var sources = chunks
            .GroupBy(c => c.SourceRef)
            .Select(g => g.First())
            .Select(c => new AiHelperSource(c.Title, c.SourceRef, c.SourceType.ToString()))
            .ToList();

        yield return new AiHelperSourcesEvent(sources);

        var toolContext = new AiToolContext(
            query.TenantId, query.UserId, query.Roles, query.Locale, query.PageEntityType, query.PageEntityId, query.CustomerId);
        var toolDefinitions = !query.IsPublic && _chatProvider.SupportsTools
            ? _toolRegistry.GetDefinitions(toolContext)
            : Array.Empty<AiToolDefinition>();

        var history = query.IsPublic
            ? Array.Empty<AiHelperConversationTurn>()
            : (await _conversation
                .GetRecentTurnsAsync(query.ConversationId, query.TenantId, _options.MaxHistoryTurns, ct)
                .ConfigureAwait(false)) ?? Array.Empty<AiHelperConversationTurn>();

        var messages = BuildPrompt(query, chunks, toolDefinitions.Count > 0, history);
        var answer = new StringBuilder();

        if (toolDefinitions.Count > 0)
        {
            var finalText = await RunToolLoopAsync(messages, toolDefinitions, toolContext, ct).ConfigureAwait(false);
            if (finalText.Length > 0)
            {
                answer.Append(finalText);
                yield return new AiHelperTokenEvent(finalText);
            }
        }
        else
        {
            var request = new AiChatRequest(messages, _options.Temperature, _options.MaxOutputTokens);
            await foreach (var delta in _chatProvider.StreamAsync(request, ct).ConfigureAwait(false))
            {
                if (delta.Done)
                {
                    break;
                }

                if (delta.Content.Length > 0)
                {
                    answer.Append(delta.Content);
                    yield return new AiHelperTokenEvent(delta.Content);
                }
            }
        }

        await _trace.WriteAsync(BuildTrace(query, chunks, answerId, answer.ToString()), ct).ConfigureAwait(false);

        yield return new AiHelperDoneEvent(answerId);
    }

    private async Task<string> RunToolLoopAsync(
        IReadOnlyList<AiChatMessage> baseMessages,
        IReadOnlyList<AiToolDefinition> tools,
        AiToolContext context,
        CancellationToken ct)
    {
        var conversation = new List<AiChatMessage>(baseMessages);

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var completion = await _chatProvider
                .CompleteAsync(new AiChatRequest(conversation, _options.Temperature, _options.MaxOutputTokens, tools), ct)
                .ConfigureAwait(false);

            if (completion.ToolCalls.Count == 0)
            {
                return completion.Text;
            }

            conversation.Add(new AiChatMessage(AiChatRole.Assistant, completion.Text, completion.ToolCalls));
            foreach (var call in completion.ToolCalls)
            {
                var result = await _toolRegistry.ExecuteAsync(call, context, ct).ConfigureAwait(false);
                conversation.Add(new AiChatMessage(AiChatRole.Tool, result.ResultJson, ToolCallId: call.Id));
            }
        }

        var fallback = await _chatProvider
            .CompleteAsync(new AiChatRequest(conversation, _options.Temperature, _options.MaxOutputTokens), ct)
            .ConfigureAwait(false);
        return fallback.Text;
    }

    private AiHelperTrace BuildTrace(AiHelperQuery query, IReadOnlyList<KnowledgeChunk> chunks, Guid answerId, string answerText)
    {
        var topScore = chunks.Count > 0 ? chunks.Max(c => c.Score) : 0.0;
        var retrieved = chunks.Select(c => new
        {
            documentId = c.DocumentId,
            sourceRef = c.SourceRef,
            sourceType = c.SourceType.ToString(),
            score = Math.Round(c.Score, 4),
        });
        return new AiHelperTrace(
            answerId,
            query.ConversationId,
            query.Question,
            answerText,
            query.Locale,
            query.IsPublic ? null : query.TenantId,
            query.IsPublic,
            query.RoutePath,
            _options.ChatModel,
            chunks.Count,
            topScore,
            JsonSerializer.Serialize(retrieved));
    }

    private static IReadOnlyList<AiChatMessage> BuildPrompt(
        AiHelperQuery query,
        IReadOnlyList<KnowledgeChunk> chunks,
        bool hasTools,
        IReadOnlyList<AiHelperConversationTurn> history)
    {
        var language = query.Locale.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ? "Turkish" : "English";

        var contextBuilder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            contextBuilder.AppendLine($"[{chunk.Title}] ({chunk.SourceRef})");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
        }

        var context = contextBuilder.Length > 0 ? contextBuilder.ToString() : "(no relevant context found)";
        var routeNote = BuildRouteNote(query.RoutePath);
        var pageNote = BuildPageNote(query.PageEntityType, query.PageEntityId);
        var toolNote = hasTools
            ? " You also have tools that read live CoreAlign data (order/invoice breakdowns, order search, recent errors). When the user asks about a specific record " +
              "(why an order/invoice total is a given amount, analyzing line items, diagnosing an error), CALL the appropriate tool. " +
              "Prefer the id from the CURRENT PAGE when the user says 'this order/invoice'. If the user refers to a record without an id, use find_orders first. " +
              "Then explain the REAL figures the tool returns; never invent numbers. If you still need an id you cannot resolve, ASK the user for it. " +
              "You cannot change data; for change requests, explain the steps the user must follow themselves."
            : string.Empty;

        var system =
            "You are CoreAlign's in-app help assistant. CoreAlign is a multi-tenant ERP covering sales, customers, invoicing, " +
            "inventory, purchasing, accounting, payroll, MRP/production, and glass enclosure (CAD/CPQ). " +
            "Answer using ONLY the CONTEXT below for CoreAlign-specific facts (pages, routes, menu items, buttons, field names, and steps). " +
            "Grounding rules you MUST follow: " +
            "(1) State only menu items, button names, field names, and step sequences that actually appear in the CONTEXT; never invent, rename, or guess UI elements or steps. " +
            "(2) If the CONTEXT does not contain the steps for the user's specific task, say clearly that you do not have the exact CoreAlign steps for it, give brief general ERP guidance, and point ONLY to the closest page that actually appears in the CONTEXT (if any) — never substitute an unrelated module. " +
            "(3) Prefer the most relevant CONTEXT entries and ignore entries that are only loosely related to the question. " +
            "(4) If the user's request is ambiguous or missing key details, ask a brief clarifying question instead of guessing. " +
            $"Always answer in {language}. Be concise and practical.{toolNote}{pageNote}{routeNote}\n\nCONTEXT:\n{context}";

        var messages = new List<AiChatMessage> { new(AiChatRole.System, system) };
        foreach (var turn in history)
        {
            if (!string.IsNullOrWhiteSpace(turn.Question))
            {
                messages.Add(new AiChatMessage(AiChatRole.User, turn.Question));
            }
            if (!string.IsNullOrWhiteSpace(turn.Answer))
            {
                messages.Add(new AiChatMessage(AiChatRole.Assistant, turn.Answer));
            }
        }
        messages.Add(new AiChatMessage(AiChatRole.User, query.Question));
        return messages;
    }

    private static string BuildPageNote(string? entityType, Guid? entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType) || !entityId.HasValue || entityId.Value == Guid.Empty)
        {
            return string.Empty;
        }

        var safeType = entityType.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (safeType.Length > 40)
        {
            safeType = safeType[..40];
        }

        return $" The user is currently viewing {safeType} with id {entityId.Value:D}; treat 'this {safeType}' as that id.";
    }

    private static string BuildRouteNote(string? routePath)
    {
        if (string.IsNullOrWhiteSpace(routePath))
        {
            return string.Empty;
        }

        var sanitized = routePath.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (sanitized.Length > 200)
        {
            sanitized = sanitized[..200];
        }

        return $" The user is currently on the page: {sanitized}.";
    }
}
