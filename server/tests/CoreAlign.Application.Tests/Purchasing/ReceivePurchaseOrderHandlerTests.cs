using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Purchasing;

public class ReceivePurchaseOrderHandlerTests
{
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IGoodsReceiptRepository _grns = Substitute.For<IGoodsReceiptRepository>();
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ReceivePurchaseOrderHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PoId = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    public ReceivePurchaseOrderHandlerTests()
    {
        // Return a real movement so the handler can read TotalCost for GL posting.
        _allocation.ApplyReceiptAsync(Arg.Any<StockReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var r = ci.Arg<StockReceiptRequest>();
                return new StockMovement(r.ProductId, r.WarehouseId, StockMovementType.Receipt, r.Quantity,
                    r.UnitCost, r.Quantity, r.UnitCost, DateTime.UtcNow, r.SourceDocumentType,
                    r.SourceDocumentId, r.SourceLineId, r.SourceReference) { Id = Guid.NewGuid() };
            });
        _grns.GetByIdempotencyKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GoodsReceipt?)null);
        _sequences.GetAsync(DocumentSequenceType.GoodsReceiptNumber, Arg.Any<CancellationToken>())
            .Returns(_ => new DocumentSequence(DocumentSequenceType.GoodsReceiptNumber, "GRN", 2026, 1, 5));
        _sequences.ConsumeAsync(DocumentSequenceType.GoodsReceiptNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("GRN-2026-00001");
        _sut = new ReceivePurchaseOrderHandler(_orders, _grns, _allocation, _warehouses, _sequences, _outbox, _uow);
    }

    private static PurchaseOrder ApprovedPo()
    {
        var po = new PurchaseOrder("PO-1", Guid.NewGuid(), "Acme", DateTime.UtcNow, "TRY")
        {
            Id = PoId,
            TenantId = TenantId,
        };
        var line = new PurchaseOrderLine(ProductId, "SKU-A", "Widget", 10m, 5m) { Id = LineId, TenantId = TenantId };
        po.ReplaceLines(new[] { line });
        po.Submit();
        po.Approve(Guid.NewGuid());
        return po;
    }

    [Fact]
    public async Task Receiving_full_quantity_posts_stock_and_marks_received()
    {
        var po = ApprovedPo();
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);

        await _sut.Handle(
            new ReceivePurchaseOrderCommand(PoId, new List<ReceiptLineInput> { new(LineId, 10m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        po.Lines.First().QuantityReceived.Should().Be(10m);
        po.Status.Should().Be(PurchaseOrderStatus.Received);
        await _allocation.Received(1).ApplyReceiptAsync(
            Arg.Is<StockReceiptRequest>(r =>
                r.ProductId == ProductId &&
                r.Quantity == 10m &&
                r.WarehouseId == WarehouseId &&
                r.UnitCost == 5m &&
                r.SourceDocumentType == StockSourceDocumentType.Purchase),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Receiving_partial_quantity_marks_partially_received()
    {
        var po = ApprovedPo();
        _orders.GetByIdAsync(PoId, Arg.Any<CancellationToken>()).Returns(po);

        await _sut.Handle(
            new ReceivePurchaseOrderCommand(PoId, new List<ReceiptLineInput> { new(LineId, 4m) },
                Guid.NewGuid().ToString("N"), WarehouseId),
            default);

        po.Lines.First().QuantityReceived.Should().Be(4m);
        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
    }
}
