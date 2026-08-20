using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

// Phase111 deferred stock + GL recognition to QC-approve but still advanced QuantityReceived at
// receive time, so two places that read it as "how much has credited GR/IR (322)" — the PO-close
// write-off and the three-way-match ceiling — acted on a credit that was never booked, and QC
// reject could un-receive quantity that had already been billed. These tests pin the corrected
// meaning: QuantityReceived is recognised quantity, QuantityAwaitingInspection is the QC hold.
public class QcHoldGrIrTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PoId = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IGoodsReceiptRepository _grns = Substitute.For<IGoodsReceiptRepository>();
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public QcHoldGrIrTests()
    {
        _allocation.ApplyReceiptAsync(Arg.Any<StockReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var r = ci.Arg<StockReceiptRequest>();
                return new StockMovement(r.ProductId, r.WarehouseId, StockMovementType.Receipt, r.Quantity,
                    r.UnitCost, r.Quantity, r.UnitCost, DateTime.UtcNow, r.SourceDocumentType,
                    r.SourceDocumentId, r.SourceLineId, r.SourceReference) { Id = Guid.NewGuid() };
            });
        _grns.GetByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((GoodsReceipt?)null);
        _sequences.GetAsync(DocumentSequenceType.GoodsReceiptNumber, Arg.Any<CancellationToken>())
            .Returns(_ => new DocumentSequence(DocumentSequenceType.GoodsReceiptNumber, "GRN", 2026, 1, 5));
        _sequences.ConsumeAsync(DocumentSequenceType.GoodsReceiptNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("GRN-2026-00001");
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());
    }

    private ReceivePurchaseOrderHandler ReceiveSut() =>
        new(_orders, _grns, _allocation, _warehouses, _sequences, _outbox, _products, _uow);

    private ApproveGoodsReceiptQcHandler ApproveSut() =>
        new(_grns, _orders, _allocation, _outbox, _uow);

    private RejectGoodsReceiptQcHandler RejectSut() =>
        new(_grns, _orders, _uow);

    private static PurchaseOrder ApprovedPo(decimal quantity = 10m, decimal unitCost = 5m)
    {
        var po = new PurchaseOrder("PO-1", Guid.NewGuid(), "Acme", DateTime.UtcNow, "TRY")
        {
            Id = PoId,
            TenantId = TenantId,
        };
        var line = new PurchaseOrderLine(ProductId, "SKU-A", "Widget", quantity, unitCost) { Id = LineId, TenantId = TenantId };
        po.ReplaceLines(new[] { line });
        po.Submit();
        po.Approve(Guid.NewGuid());
        return po;
    }

    private async Task<GoodsReceipt> ReceiveUnderQcAsync(PurchaseOrder po, decimal quantity)
    {
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);
        GoodsReceipt? captured = null;
        await _grns.AddAsync(Arg.Do<GoodsReceipt>(g => captured = g), Arg.Any<CancellationToken>());

        await ReceiveSut().Handle(
            new ReceivePurchaseOrderCommand(PoId, new List<ReceiptLineInput> { new(LineId, quantity) },
                Guid.NewGuid().ToString("N"), WarehouseId, RequiresQcInspection: true),
            default);

        captured.Should().NotBeNull();
        return captured!;
    }

    [Fact]
    public async Task A_qc_held_receipt_claims_the_line_without_counting_as_received()
    {
        var po = ApprovedPo();

        await ReceiveUnderQcAsync(po, 6m);

        var line = po.Lines.Single();
        line.QuantityReceived.Should().Be(0m);
        line.QuantityAwaitingInspection.Should().Be(6m);
        line.QuantityRemainingToReceive.Should().Be(4m);
        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        await _allocation.DidNotReceive().ApplyReceiptAsync(Arg.Any<StockReceiptRequest>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_qc_held_receipt_still_blocks_re_receiving_the_same_quantity()
    {
        var po = ApprovedPo();
        await ReceiveUnderQcAsync(po, 6m);

        var again = () => ReceiveSut().Handle(
            new ReceivePurchaseOrderCommand(PoId, new List<ReceiptLineInput> { new(LineId, 5m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        await again.Should().ThrowAsync<InvalidOrderLineException>();
    }

    [Fact]
    public async Task Approving_qc_moves_the_hold_into_received_and_posts_stock_and_gl()
    {
        var po = ApprovedPo();
        var grn = await ReceiveUnderQcAsync(po, 6m);
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);
        _orders.GetByIdAsync(grn.PurchaseOrderId, Arg.Any<CancellationToken>()).Returns(po);

        await ApproveSut().Handle(new ApproveGoodsReceiptQcCommand(grn.Id, Guid.NewGuid()), default);

        var line = po.Lines.Single();
        line.QuantityReceived.Should().Be(6m);
        line.QuantityAwaitingInspection.Should().Be(0m);
        await _allocation.Received(1).ApplyReceiptAsync(
            Arg.Is<StockReceiptRequest>(r => r.Quantity == 6m), Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<GLPostingRequest>(r => r.SourceType == JournalSourceType.GoodsReceipt &&
                r.Lines.Any(l => l.Key == GLPostingKey.GoodsReceiptClearing && l.Credit == 30m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejecting_qc_releases_the_hold_without_touching_received_quantity()
    {
        var po = ApprovedPo();
        po.RecordLineReceipt(LineId, 4m);
        var grn = await ReceiveUnderQcAsync(po, 6m);
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);

        await RejectSut().Handle(new RejectGoodsReceiptQcCommand(grn.Id, "damaged"), default);

        var line = po.Lines.Single();
        line.QuantityReceived.Should().Be(4m);
        line.QuantityAwaitingInspection.Should().Be(0m);
        line.QuantityRemainingToReceive.Should().Be(6m);
        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
    }

    // The finding: closing the PO wrote off (QuantityReceived - QuantityBilled) * UnitCost as an
    // orphaned 322 credit. For QC-held goods no credit was ever booked, so the write-off invented
    // a debit against 322 and a matching PPV credit out of nothing.
    [Fact]
    public async Task Closing_a_po_does_not_write_off_clearing_for_goods_still_held_in_qc()
    {
        var po = ApprovedPo();
        await ReceiveUnderQcAsync(po, 6m);
        _orders.GetByIdAsync(po.Id, Arg.Any<CancellationToken>()).Returns(po);
        _outbox.ClearReceivedCalls();

        await new ClosePurchaseOrderHandler(_orders, _outbox, _uow)
            .Handle(new ClosePurchaseOrderCommand(po.Id), default);

        po.Status.Should().Be(PurchaseOrderStatus.Closed);
        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Closing_a_po_still_writes_off_clearing_for_goods_that_passed_qc()
    {
        var po = ApprovedPo();
        var grn = await ReceiveUnderQcAsync(po, 6m);
        _grns.GetByIdAsync(grn.Id, Arg.Any<CancellationToken>()).Returns(grn);
        _orders.GetByIdAsync(po.Id, Arg.Any<CancellationToken>()).Returns(po);
        await ApproveSut().Handle(new ApproveGoodsReceiptQcCommand(grn.Id, Guid.NewGuid()), default);
        _outbox.ClearReceivedCalls();

        await new ClosePurchaseOrderHandler(_orders, _outbox, _uow)
            .Handle(new ClosePurchaseOrderCommand(po.Id), default);

        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<GLPostingRequest>(r =>
                r.SourceType == JournalSourceType.PurchaseOrderClose &&
                r.Lines.Any(l => l.Key == GLPostingKey.GoodsReceiptClearing && l.Debit == 30m)),
            Arg.Any<CancellationToken>());
    }

    // Defence in depth for the QC-reject path: nothing may drive received quantity below what has
    // already been billed, because the bill debited 322 against that quantity.
    [Fact]
    public void Un_receiving_below_the_billed_quantity_is_refused()
    {
        var po = ApprovedPo();
        po.RecordLineReceipt(LineId, 10m);
        po.RecordLineBill(LineId, 7m);

        var act = () => po.ReverseLineReceipt(LineId, 5m);

        act.Should().Throw<ReceiptReversalBelowBilledException>();
        po.Lines.Single().QuantityReceived.Should().Be(10m);
    }

    [Fact]
    public void Un_receiving_down_to_the_billed_quantity_is_allowed()
    {
        var po = ApprovedPo();
        po.RecordLineReceipt(LineId, 10m);
        po.RecordLineBill(LineId, 7m);

        po.ReverseLineReceipt(LineId, 3m);

        po.Lines.Single().QuantityReceived.Should().Be(7m);
    }
}
