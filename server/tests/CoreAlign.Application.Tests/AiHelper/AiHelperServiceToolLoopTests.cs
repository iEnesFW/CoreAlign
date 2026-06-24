using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Providers;
using CoreAlign.Application.AiHelper.Retrieval;
using CoreAlign.Application.AiHelper.Tools;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.AiHelper;

public class AiHelperServiceToolLoopTests
{
    private readonly IAiEmbeddingProvider _embedding = Substitute.For<IAiEmbeddingProvider>();
    private readonly IKnowledgeRetriever _retriever = Substitute.For<IKnowledgeRetriever>();
    private readonly IAiChatProvider _chat = Substitute.For<IAiChatProvider>();
    private readonly IAiToolRegistry _registry = Substitute.For<IAiToolRegistry>();
    private readonly IAiHelperConversationReader _conversation = Substitute.For<IAiHelperConversationReader>();
    private readonly IAiHelperTraceWriter _trace = Substitute.For<IAiHelperTraceWriter>();

    public AiHelperServiceToolLoopTests()
    {
        _embedding.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<float[]>)new float[][] { Array.Empty<float>() });
        _conversation.GetRecentTurnsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiHelperConversationTurn>());
    }

    private AiHelperService Build() =>
        new(_embedding, _retriever, _chat, _registry, _conversation, _trace, Options.Create(new AiHelperOptions()));

    private static AiHelperQuery Query() =>
        new("siparisim neden 5400?", "tr", null, Guid.NewGuid(), false, new[] { "TenantAdmin" }, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Calls_tool_then_emits_final_answer()
    {
        _chat.SupportsTools.Returns(true);
        _registry.GetDefinitions(Arg.Any<AiToolContext>())
            .Returns(new[] { new AiToolDefinition("get_order_breakdown", "d", "{}") });
        _registry.ExecuteAsync(Arg.Any<AiToolCall>(), Arg.Any<AiToolContext>(), Arg.Any<CancellationToken>())
            .Returns(AiToolResult.Ok("{\"total\":5400}"));
        _chat.CompleteAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new AiChatCompletion(string.Empty, new[] { new AiToolCall("get_order_breakdown", "get_order_breakdown", "{\"orderId\":\"x\"}") }),
                new AiChatCompletion("Toplam 5400 cunku indirim uygulandi.", Array.Empty<AiToolCall>()));

        var events = new List<AiHelperEvent>();
        await foreach (var ev in Build().AskAsync(Query(), CancellationToken.None))
        {
            events.Add(ev);
        }

        events.OfType<AiHelperTokenEvent>().Should().ContainSingle(t => t.Text == "Toplam 5400 cunku indirim uygulandi.");
        events.OfType<AiHelperDoneEvent>().Should().ContainSingle();
        await _registry.Received(1).ExecuteAsync(Arg.Any<AiToolCall>(), Arg.Any<AiToolContext>(), Arg.Any<CancellationToken>());
        await _chat.Received(2).CompleteAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>());
        await _trace.Received(1).WriteAsync(
            Arg.Is<AiHelperTrace>(t => t.AnswerText == "Toplam 5400 cunku indirim uygulandi."),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_to_streaming_when_provider_has_no_tools()
    {
        _chat.SupportsTools.Returns(false);
        _chat.StreamAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(ToStream(new AiChatDelta("merhaba", false), new AiChatDelta(string.Empty, true)));

        var events = new List<AiHelperEvent>();
        await foreach (var ev in Build().AskAsync(Query(), CancellationToken.None))
        {
            events.Add(ev);
        }

        events.OfType<AiHelperTokenEvent>().Should().ContainSingle(t => t.Text == "merhaba");
        await _registry.DidNotReceive().ExecuteAsync(Arg.Any<AiToolCall>(), Arg.Any<AiToolContext>(), Arg.Any<CancellationToken>());
        await _chat.DidNotReceive().CompleteAsync(Arg.Any<AiChatRequest>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<AiChatDelta> ToStream(params AiChatDelta[] deltas)
    {
        foreach (var delta in deltas)
        {
            yield return delta;
        }
        await Task.CompletedTask;
    }
}
