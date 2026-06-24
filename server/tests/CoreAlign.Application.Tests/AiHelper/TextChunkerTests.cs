using CoreAlign.Application.AiHelper.Ingestion;

namespace CoreAlign.Application.Tests.AiHelper;

public class TextChunkerTests
{
    [Fact]
    public void Chunk_EmptyOrWhitespace_ReturnsEmpty()
    {
        TextChunker.Chunk("").Should().BeEmpty();
        TextChunker.Chunk("   ").Should().BeEmpty();
    }

    [Fact]
    public void Chunk_ShortContent_ReturnsSingleChunk()
    {
        var result = TextChunker.Chunk("Hello world.");

        result.Should().ContainSingle();
        result[0].Should().Be("Hello world.");
    }

    [Fact]
    public void Chunk_ManyParagraphs_SplitsAndStaysWithinBound()
    {
        var content = string.Join(
            "\n\n",
            Enumerable.Repeat("This is a sentence with several words in it.", 200));

        var result = TextChunker.Chunk(content, maxChars: 500, overlapChars: 50);

        result.Count.Should().BeGreaterThan(1);
        result.Should().OnlyContain(c => c.Length <= 500 + 50 + 4);
        result.Should().OnlyContain(c => c.Length > 0);
    }

    [Fact]
    public void Chunk_OversizedSingleParagraph_HardSplitsWithinMax()
    {
        var huge = new string('a', 2500);

        var result = TextChunker.Chunk(huge, maxChars: 1000, overlapChars: 0);

        result.Count.Should().BeGreaterThanOrEqualTo(3);
        result.Should().OnlyContain(c => c.Length <= 1000);
    }

    [Fact]
    public void ChunkWithContext_NoHeadings_HasEmptyHeadingPath()
    {
        var result = TextChunker.ChunkWithContext("Just a plain paragraph with no headings.");

        result.Should().ContainSingle();
        result[0].HeadingPath.Should().BeEmpty();
        result[0].Body.Should().Contain("plain paragraph");
    }

    [Fact]
    public void ChunkWithContext_NestedHeadings_BuildsHeadingTrail()
    {
        var content = "# Invoices\n\nIntro text.\n\n## Create\n\nStep one body.\n\n### Details\n\nDeep body.";

        var result = TextChunker.ChunkWithContext(content);

        result.Should().Contain(c => c.HeadingPath == "Invoices" && c.Body.Contains("Intro"));
        result.Should().Contain(c => c.HeadingPath == "Invoices › Create" && c.Body.Contains("Step one"));
        result.Should().Contain(c => c.HeadingPath == "Invoices › Create › Details" && c.Body.Contains("Deep body"));
    }

    [Fact]
    public void ChunkWithContext_SiblingHeading_PopsTrail()
    {
        var content = "## A\n\nbody a\n\n## B\n\nbody b";

        var result = TextChunker.ChunkWithContext(content);

        result.Should().Contain(c => c.HeadingPath == "A" && c.Body.Contains("body a"));
        result.Should().Contain(c => c.HeadingPath == "B" && c.Body.Contains("body b"));
    }
}
