using CoreAlign.Application.AiHelper.Ingestion;
using CoreAlign.Application.AiHelper.Providers;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Tests.AiHelper;

public class KnowledgeIngestionServiceTests
{
    [Fact]
    public async Task ReindexAsync_NewDocument_AddsDocumentWithEmbeddedChunks()
    {
        var source = Substitute.For<IKbSourceProvider>();
        source.GetSourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KbSourceDocument> { Doc("Hello world. This is some content.") });

        var embed = Substitute.For<IAiEmbeddingProvider>();
        embed.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ci => MakeEmbeddings(ci.Arg<IReadOnlyList<string>>().Count));

        var repo = Substitute.For<IAiKbRepository>();
        repo.FindAsync(Arg.Any<AiKbSourceType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AiKbDocument?)null);

        AiKbDocument? added = null;
        repo.When(r => r.AddAsync(Arg.Any<AiKbDocument>(), Arg.Any<CancellationToken>()))
            .Do(ci => added = ci.Arg<AiKbDocument>());

        var result = await Build(source, embed, repo).ReindexAsync(CancellationToken.None);

        result.DocumentCount.Should().Be(1);
        result.ChunkCount.Should().BeGreaterThan(0);
        result.SkippedCount.Should().Be(0);
        added.Should().NotBeNull();
        added!.Chunks.Should().NotBeEmpty();
        added.Chunks.Should().OnlyContain(c => c.Embedding.Length == 4);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReindexAsync_UnchangedHash_SkipsWithoutEmbeddingOrAdd()
    {
        const string content = "Identical content.";
        var source = Substitute.For<IKbSourceProvider>();
        source.GetSourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KbSourceDocument> { Doc(content) });

        var embed = Substitute.For<IAiEmbeddingProvider>();
        var repo = Substitute.For<IAiKbRepository>();
        repo.FindAsync(Arg.Any<AiKbSourceType>(), "help/en/x", "en", Arg.Any<CancellationToken>())
            .Returns(new AiKbDocument
            {
                SourceType = AiKbSourceType.Article,
                SourceRef = "help/en/x",
                Locale = "en",
                ContentHash = KnowledgeIngestionService.ComputeContentHash(content),
            });

        var result = await Build(source, embed, repo).ReindexAsync(CancellationToken.None);

        result.SkippedCount.Should().Be(1);
        result.DocumentCount.Should().Be(0);
        await embed.DidNotReceive().EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddAsync(Arg.Any<AiKbDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReindexAsync_EmbeddingCountMismatch_SkipsDocument()
    {
        var source = Substitute.For<IKbSourceProvider>();
        source.GetSourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KbSourceDocument> { Doc("Some content here.") });

        var embed = Substitute.For<IAiEmbeddingProvider>();
        embed.EmbedAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ci => (IReadOnlyList<float[]>)new List<float[]>());

        var repo = Substitute.For<IAiKbRepository>();
        repo.FindAsync(Arg.Any<AiKbSourceType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AiKbDocument?)null);

        var result = await Build(source, embed, repo).ReindexAsync(CancellationToken.None);

        result.SkippedCount.Should().Be(1);
        result.DocumentCount.Should().Be(0);
        await repo.DidNotReceive().AddAsync(Arg.Any<AiKbDocument>(), Arg.Any<CancellationToken>());
    }

    private static KnowledgeIngestionService Build(
        IKbSourceProvider source,
        IAiEmbeddingProvider embed,
        IAiKbRepository repo) =>
        new(new[] { source }, embed, repo, Substitute.For<ILogger<KnowledgeIngestionService>>());

    private static KbSourceDocument Doc(string content) =>
        new(AiKbSourceType.Article, "help/en/x", "Title", "en", AiKbScope.Public, null, content);

    private static IReadOnlyList<float[]> MakeEmbeddings(int count)
    {
        var list = new List<float[]>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new[] { 0.1f, 0.2f, 0.3f, 0.4f });
        }

        return list;
    }
}
