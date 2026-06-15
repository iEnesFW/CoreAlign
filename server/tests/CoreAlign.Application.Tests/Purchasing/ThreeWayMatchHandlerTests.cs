using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

public class ThreeWayMatchHandlerTests
{
    private readonly IThreeWayMatchReader _reader = Substitute.For<IThreeWayMatchReader>();
    private readonly GetThreeWayMatchHandler _sut;

    public ThreeWayMatchHandlerTests()
    {
        _sut = new GetThreeWayMatchHandler(_reader);
    }

    [Fact]
    public async Task Reports_under_received_when_receipt_qty_below_po_qty()
    {
        _reader.GetMismatchesAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ThreeWayMatchRow>
            {
                new(Guid.NewGuid(), "PO-1", Guid.NewGuid(), "Acme", "TRY",
                    Guid.NewGuid(), "SKU-1", "Widget",
                    ExpectedQty: 100m, ReceivedQty: 95m, BilledQty: 100m,
                    ExpectedAmount: 1000m, BilledAmount: 1000m,
                    Discrepancies: new[] { "UnderReceived", "OverBilled" }),
            });

        var result = await _sut.Handle(new GetThreeWayMatchQuery(), default);

        result.Should().HaveCount(1);
        result[0].Discrepancies.Should().Contain("UnderReceived");
        result[0].Discrepancies.Should().Contain("OverBilled");
        result[0].ExpectedQty.Should().Be(100m);
        result[0].ReceivedQty.Should().Be(95m);
    }

    [Fact]
    public async Task Empty_when_no_mismatches()
    {
        _reader.GetMismatchesAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ThreeWayMatchRow>());

        var result = await _sut.Handle(new GetThreeWayMatchQuery(), default);

        result.Should().BeEmpty();
    }
}
