using CoreAlign.Application.Catalog.Linker;

namespace CoreAlign.Application.Tests.Catalog.Linker;

public class CatalogLinkageBackfillHandlerTests
{
    private readonly ICatalogProductLinker _linker = Substitute.For<ICatalogProductLinker>();

    [Fact]
    public async Task Handle_returns_zero_for_empty_catalog()
    {
        _linker.BackfillAllAsync(Arg.Any<CancellationToken>()).Returns(0);
        var sut = new CatalogLinkageBackfillHandler(_linker);

        var count = await sut.Handle(new CatalogLinkageBackfillCommand(), default);

        count.Should().Be(0);
        await _linker.Received(1).BackfillAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_count_reported_by_linker()
    {
        _linker.BackfillAllAsync(Arg.Any<CancellationToken>()).Returns(3);
        var sut = new CatalogLinkageBackfillHandler(_linker);

        var count = await sut.Handle(new CatalogLinkageBackfillCommand(), default);

        count.Should().Be(3);
        await _linker.Received(1).BackfillAllAsync(Arg.Any<CancellationToken>());
    }
}
