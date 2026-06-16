using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

public class ClosePurchaseOrderGrIrTests
{
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ClosePurchaseOrderHandler _sut;

    public ClosePurchaseOrderGrIrTests()
    {
        _sut = new ClosePurchaseOrderHandler(_orders, _outbox, _uow);
    }

    private static (PurchaseOrder po, Guid lineId) ReceivedPo(decimal quantity, decimal received, decimal unitCost)
    {
        var po = new PurchaseOrder("PO-1", Guid.NewGuid(), "Acme", DateTime.UtcNow, "TRY") { Id = Guid.NewGuid() };
        var line = new PurchaseOrderLine(Guid.NewGuid(), "SKU-A", "Widget", quantity, unitCost) { Id = Guid.NewGuid() };
        po.ReplaceLines(new[] { line });
        po.Submit();
        po.Approve(Guid.NewGuid());
        if (received > 0m) po.RecordLineReceipt(line.Id, received);
        return (po, line.Id);
    }

    [Fact]
    public async Task Close_writes_off_received_but_unbilled_clearing_balance()
    {
        var (po, _) = ReceivedPo(quantity: 10m, received: 6m, unitCost: 5m);
        _orders.GetByIdAsync(po.Id, Arg.Any<CancellationToken>()).Returns(po);

        await _sut.Handle(new ClosePurchaseOrderCommand(po.Id), default);

        po.Status.Should().Be(PurchaseOrderStatus.Closed);
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<GLPostingRequest>(r =>
                r.SourceType == JournalSourceType.PurchaseOrderClose &&
                r.SourceDocumentId == po.Id &&
                r.Lines.Count == 2 &&
                r.Lines.Any(l => l.Key == GLPostingKey.GoodsReceiptClearing && l.Debit == 30m) &&
                r.Lines.Any(l => l.Key == GLPostingKey.PurchasePriceVariance && l.Credit == 30m)),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_does_not_post_when_nothing_received()
    {
        var (po, _) = ReceivedPo(quantity: 10m, received: 0m, unitCost: 5m);
        _orders.GetByIdAsync(po.Id, Arg.Any<CancellationToken>()).Returns(po);

        await _sut.Handle(new ClosePurchaseOrderCommand(po.Id), default);

        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
