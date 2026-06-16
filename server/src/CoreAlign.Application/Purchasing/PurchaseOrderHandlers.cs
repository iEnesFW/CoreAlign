using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Purchasing;

internal static class PurchaseOrderMapper
{
    public static PurchaseOrderDto ToDto(PurchaseOrder p) => new(
        p.Id,
        p.PoNumber,
        p.VendorId,
        p.VendorName,
        p.OrderDate,
        p.ExpectedDate,
        p.Currency,
        p.ExchangeRate,
        p.WarehouseId,
        p.Status,
        p.Subtotal,
        p.TaxTotal,
        p.Total,
        p.Notes,
        p.Lines.OrderBy(l => l.LineNumber).Select(ToDto).ToList(),
        p.CreatedAtUtc);

    private static PurchaseOrderLineDto ToDto(PurchaseOrderLine l) => new(
        l.Id, l.ProductId, l.ProductSku, l.ProductName, l.Quantity, l.QuantityReceived, l.QuantityBilled,
        l.QuantityRemainingToReceive, l.UnitCost, l.TaxRatePercent, l.TaxAmount, l.LineSubtotal, l.LineTotal,
        l.UomId, l.UomCode, l.LineNotes);
}

internal static class GoodsReceiptMapper
{
    public static GoodsReceiptDto ToDto(GoodsReceipt g) => new(
        g.Id,
        g.GrnNumber,
        g.VendorId,
        g.VendorName,
        g.PurchaseOrderId,
        g.PoNumber,
        g.ReceiptDateUtc,
        g.WarehouseId,
        g.Status,
        g.ReceivedByUserId,
        g.Notes,
        g.Currency,
        g.ExchangeRate,
        g.TotalCost,
        g.ReversedAtUtc,
        g.ReversedByUserId,
        g.ReversalReason,
        g.Lines.OrderBy(l => l.LineNumber).Select(ToDto).ToList(),
        g.CreatedAtUtc);

    private static GoodsReceiptLineDto ToDto(GoodsReceiptLine l) => new(
        l.Id, l.LineNumber, l.PurchaseOrderLineId, l.ProductId, l.ProductSku, l.ProductName,
        l.QuantityReceived, l.UnitCost, l.LineCost, l.StockMovementId);
}

internal static class PurchaseOrderLineFactory
{
    public static async Task<List<PurchaseOrderLine>> BuildAsync(
        IEnumerable<PurchaseOrderLineInput> inputs,
        IProductRepository products,
        CancellationToken ct)
    {
        var inputList = inputs.ToList();
        var ids = inputList.Select(l => l.ProductId).Distinct().ToList();
        var map = await products.GetByIdsAsync(ids, ct);
        if (map.Count != ids.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }
        return inputList.Select(l =>
        {
            var p = map[l.ProductId];
            return new PurchaseOrderLine(p.Id, p.Sku, p.Name, l.Quantity, l.UnitCost, l.TaxRatePercent,
                l.UomId, l.UomCode, l.LineNotes);
        }).ToList();
    }
}

public class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IVendorRepository _vendors;
    private readonly IProductRepository _products;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;

    public CreatePurchaseOrderHandler(
        IPurchaseOrderRepository orders,
        IVendorRepository vendors,
        IProductRepository products,
        IDocumentSequenceRepository sequences,
        IUnitOfWork uow)
    {
        _orders = orders;
        _vendors = vendors;
        _products = products;
        _sequences = sequences;
        _uow = uow;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundForPurchaseException();
        var now = DateTime.UtcNow;

        string poNumber;
        if (!string.IsNullOrWhiteSpace(c.PoNumber))
        {
            if (await _orders.PoNumberExistsAsync(c.PoNumber.Trim(), null, ct))
            {
                throw new DuplicatePurchaseOrderNumberException();
            }
            poNumber = c.PoNumber.Trim();
        }
        else
        {
            var seq = await _sequences.GetAsync(DocumentSequenceType.PurchaseOrderNumber, ct);
            if (seq is null)
            {
                await _sequences.AddAsync(new DocumentSequence(DocumentSequenceType.PurchaseOrderNumber, "PO", now.Year, 1, 5), ct);
                await _uow.SaveChangesAsync(ct);
            }
            poNumber = await _sequences.ConsumeAsync(DocumentSequenceType.PurchaseOrderNumber, now, ct);
        }

        var po = new PurchaseOrder(poNumber, vendor.Id, vendor.Name, c.OrderDate, c.Currency.ToUpperInvariant());
        po.UpdateHeader(vendor.Id, vendor.Name, c.OrderDate, c.ExpectedDate, c.Currency.ToUpperInvariant(),
            c.ExchangeRate, c.WarehouseId, c.Notes);
        po.ReplaceLines(await PurchaseOrderLineFactory.BuildAsync(c.Lines, _products, ct));

        await _orders.AddAsync(po, ct);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

public class UpdatePurchaseOrderHandler : IRequestHandler<UpdatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IVendorRepository _vendors;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public UpdatePurchaseOrderHandler(IPurchaseOrderRepository orders, IVendorRepository vendors, IProductRepository products, IUnitOfWork uow)
    {
        _orders = orders;
        _vendors = vendors;
        _products = products;
        _uow = uow;
    }

    public async Task<PurchaseOrderDto> Handle(UpdatePurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();
        var vendor = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundForPurchaseException();

        po.UpdateHeader(vendor.Id, vendor.Name, c.OrderDate, c.ExpectedDate, c.Currency.ToUpperInvariant(),
            c.ExchangeRate, c.WarehouseId, c.Notes);
        po.ReplaceLines(await PurchaseOrderLineFactory.BuildAsync(c.Lines, _products, ct));

        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

public class DeletePurchaseOrderHandler : IRequestHandler<DeletePurchaseOrderCommand, bool>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public DeletePurchaseOrderHandler(IPurchaseOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<bool> Handle(DeletePurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct);
        if (po is null) return false;
        if (po.Status != PurchaseOrderStatus.Draft)
        {
            throw new OrderImmutableException(po.Status.ToString());
        }
        _orders.Remove(po);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class SubmitPurchaseOrderHandler : IRequestHandler<SubmitPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public SubmitPurchaseOrderHandler(IPurchaseOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<PurchaseOrderDto> Handle(SubmitPurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();
        po.Submit();
        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

public class ApprovePurchaseOrderHandler : IRequestHandler<ApprovePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    public ApprovePurchaseOrderHandler(IPurchaseOrderRepository orders, IUnitOfWork uow) { _orders = orders; _uow = uow; }

    public async Task<PurchaseOrderDto> Handle(ApprovePurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();
        po.Approve(c.ApprovedByUserId);
        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

// Received-but-never-billed quantity leaves an orphaned credit on the GR/IR
// clearing account (322) that the vendor bill would otherwise have settled.
// Closing or cancelling the PO writes that residual off so 322 returns to zero,
// debiting the clearing leg and crediting purchase price variance. The residual
// is exact at the PO line UnitCost — which is the cost the receipt credited (see
// ReceivePurchaseOrderHandler). Per-receipt costing accuracy is deferred to the
// GRN sprint. Idempotency key = po.Id so a double-close cannot double-post.
internal static class GoodsReceiptClearingWriteOff
{
    public static async Task EnqueueAsync(IGLPostingOutbox outbox, PurchaseOrder po, string reason, CancellationToken ct)
    {
        var residual = Math.Round(po.Lines
            .Where(l => l.QuantityReceived > l.QuantityBilled)
            .Sum(l => Math.Round((l.QuantityReceived - l.QuantityBilled) * l.UnitCost, 4)), 4);
        if (residual <= 0m) return;

        await outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.PurchaseOrderClose, po.Id, po.PoNumber, DateTime.UtcNow.Date,
            JournalEntryType.Mahsup, $"{reason} (PO {po.PoNumber})",
            new[]
            {
                new GLPostingLine(GLPostingKey.GoodsReceiptClearing, residual, 0m),
                new GLPostingLine(GLPostingKey.PurchasePriceVariance, 0m, residual),
            },
            po.Currency, po.ExchangeRate), ct);
    }
}

public class CancelPurchaseOrderHandler : IRequestHandler<CancelPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public CancelPurchaseOrderHandler(IPurchaseOrderRepository orders, IGLPostingOutbox outbox, IUnitOfWork uow)
    {
        _orders = orders;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<PurchaseOrderDto> Handle(CancelPurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();
        po.Cancel(c.Reason);
        await GoodsReceiptClearingWriteOff.EnqueueAsync(_outbox, po, "GR/IR iptal mahsubu", ct);
        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

public class ClosePurchaseOrderHandler : IRequestHandler<ClosePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public ClosePurchaseOrderHandler(IPurchaseOrderRepository orders, IGLPostingOutbox outbox, IUnitOfWork uow)
    {
        _orders = orders;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<PurchaseOrderDto> Handle(ClosePurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();
        po.Close();
        await GoodsReceiptClearingWriteOff.EnqueueAsync(_outbox, po, "GR/IR kapanış mahsubu", ct);
        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

public class ReceivePurchaseOrderHandler : IRequestHandler<ReceivePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IGoodsReceiptRepository _grns;
    private readonly IAllocationService _allocation;
    private readonly IWarehouseRepository _warehouses;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public ReceivePurchaseOrderHandler(
        IPurchaseOrderRepository orders,
        IGoodsReceiptRepository grns,
        IAllocationService allocation,
        IWarehouseRepository warehouses,
        IDocumentSequenceRepository sequences,
        IGLPostingOutbox outbox,
        IUnitOfWork uow)
    {
        _orders = orders;
        _grns = grns;
        _allocation = allocation;
        _warehouses = warehouses;
        _sequences = sequences;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<PurchaseOrderDto> Handle(ReceivePurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();

        // P2P-4 dedup: a re-sent receive with the same key is a pure no-op. Runs
        // before any stock/GL mutation so a double-click never double-posts.
        var existing = await _grns.GetByIdempotencyKeyAsync(c.IdempotencyKey, ct);
        if (existing is not null)
        {
            return PurchaseOrderMapper.ToDto(po);
        }

        var warehouseId = c.WarehouseId ?? po.WarehouseId
            ?? (await _warehouses.GetDefaultAsync(ct))?.Id
            ?? throw new NoWarehouseConfiguredException();

        var seq = await _sequences.GetAsync(DocumentSequenceType.GoodsReceiptNumber, ct);
        if (seq is null)
        {
            await _sequences.AddAsync(new DocumentSequence(DocumentSequenceType.GoodsReceiptNumber, "GRN", DateTime.UtcNow.Year, 1, 5), ct);
            await _uow.SaveChangesAsync(ct);
        }
        var grnNumber = await _sequences.ConsumeAsync(DocumentSequenceType.GoodsReceiptNumber, DateTime.UtcNow, ct);
        var grn = new GoodsReceipt(grnNumber, po, warehouseId, DateTime.UtcNow, c.IdempotencyKey,
            receivedByUserId: c.ReceivedByUserId, notes: c.Notes);

        foreach (var receipt in c.Lines.Where(l => l.Quantity > 0m))
        {
            var line = po.RecordLineReceipt(receipt.OrderLineId, receipt.Quantity);
            var movement = await _allocation.ApplyReceiptAsync(new StockReceiptRequest(
                ProductId: line.ProductId,
                WarehouseId: warehouseId,
                Quantity: receipt.Quantity,
                UnitCost: line.UnitCost,
                SourceDocumentType: StockSourceDocumentType.Purchase,
                SourceDocumentId: po.Id,
                SourceLineId: line.Id,
                SourceReference: grn.GrnNumber,
                LotId: null,
                SerialNumber: null,
                ReasonCodeId: null,
                Notes: c.Notes ?? $"Mal kabul ({grn.GrnNumber}, PO {po.PoNumber})"), ct);

            var grnLine = new GoodsReceiptLine(line.Id, line.ProductId, line.ProductSku, line.ProductName,
                receipt.Quantity, line.UnitCost);
            grnLine.SetMovementId(movement.Id);
            grn.AddLine(grnLine);
        }

        // Inventory recognition: goods enter stock against the GR/IR clearing
        // account, which the vendor bill later settles. ONE entry per GRN keyed by
        // grn.Id — the stable idempotency key that closes P2P-4 (movement ids change
        // on retry and defeat GLPostingService dedup). FX preserved: the document
        // rate is still passed so foreign receipts translate to base currency.
        var total = Math.Round(grn.Lines.Sum(l => l.LineCost), 4);
        if (total > 0m)
        {
            await _outbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.GoodsReceipt, grn.Id, grn.GrnNumber, DateTime.UtcNow.Date,
                JournalEntryType.Mahsup, $"Mal kabul ({grn.GrnNumber}, PO {po.PoNumber})",
                new[]
                {
                    new GLPostingLine(GLPostingKey.Inventory, total, 0m),
                    new GLPostingLine(GLPostingKey.GoodsReceiptClearing, 0m, total),
                },
                po.Currency, po.ExchangeRate), ct);
        }

        await _grns.AddAsync(grn, ct);
        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
    }
}

public class ReverseGoodsReceiptHandler : IRequestHandler<ReverseGoodsReceiptCommand, GoodsReceiptDto>
{
    private readonly IGoodsReceiptRepository _grns;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IAllocationService _allocation;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public ReverseGoodsReceiptHandler(
        IGoodsReceiptRepository grns,
        IPurchaseOrderRepository orders,
        IAllocationService allocation,
        IGLPostingOutbox outbox,
        IUnitOfWork uow)
    {
        _grns = grns;
        _orders = orders;
        _allocation = allocation;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<GoodsReceiptDto> Handle(ReverseGoodsReceiptCommand c, CancellationToken ct)
    {
        var grn = await _grns.GetByIdAsync(c.GrnId, ct) ?? throw new GoodsReceiptNotFoundException();
        if (grn.Status == GoodsReceiptStatus.Reversed)
        {
            return GoodsReceiptMapper.ToDto(grn);
        }

        var po = await _orders.GetByIdAsync(grn.PurchaseOrderId, ct);

        // Already-billed guard: a receipt credited GR/IR (322) which the vendor bill
        // later debits against the received qty. Pulling billed goods back out of
        // inventory would strand a 322 debit. 2b is full-reversal only, so any line
        // whose received qty is no longer fully reversible blocks the whole reversal.
        if (po is not null)
        {
            foreach (var grnLine in grn.Lines)
            {
                var poLine = po.Lines.FirstOrDefault(l => l.Id == grnLine.PurchaseOrderLineId);
                var reversibleQty = poLine is null
                    ? 0m
                    : Math.Min(grnLine.QuantityReceived, poLine.QuantityReceived - poLine.QuantityBilled);
                if (reversibleQty < grnLine.QuantityReceived)
                {
                    throw new GoodsReceiptAlreadyBilledException();
                }
            }
        }

        foreach (var grnLine in grn.Lines)
        {
            await _allocation.AdjustAsync(new StockAdjustmentRequest(
                ProductId: grnLine.ProductId,
                WarehouseId: grn.WarehouseId,
                Delta: -grnLine.QuantityReceived,
                UnitCost: grnLine.UnitCost,
                SourceDocumentType: StockSourceDocumentType.Purchase,
                SourceDocumentId: grn.Id,
                ReasonCodeId: null,
                Notes: $"Mal kabul iptali ({grn.GrnNumber})",
                LotId: null,
                NegativeMovementType: StockMovementType.AdjustmentNegative), ct);

            po?.ReverseLineReceipt(grnLine.PurchaseOrderLineId, grnLine.QuantityReceived);
        }

        // Reverse GL — ONE entry, swapped legs, keyed by grn.Id under a distinct
        // source type so it does not collide with the original GoodsReceipt entry's
        // idempotency. Same snapshot currency+rate as the receipt, so the reversal
        // nets to zero in base currency and 153/322 return to pre-receipt balances.
        var total = Math.Round(grn.Lines.Sum(l => l.LineCost), 4);
        if (total > 0m)
        {
            await _outbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.GoodsReceiptReversal, grn.Id, grn.GrnNumber, DateTime.UtcNow.Date,
                JournalEntryType.Mahsup, $"Mal kabul iptali ({grn.GrnNumber})",
                new[]
                {
                    new GLPostingLine(GLPostingKey.GoodsReceiptClearing, total, 0m),
                    new GLPostingLine(GLPostingKey.Inventory, 0m, total),
                },
                grn.Currency, grn.ExchangeRate), ct);
        }

        grn.MarkReversed(c.Reason, c.ReversedByUserId, DateTime.UtcNow);
        _grns.Update(grn);
        if (po is not null) _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return GoodsReceiptMapper.ToDto(grn);
    }
}

public class GetGoodsReceiptByIdHandler : IRequestHandler<GetGoodsReceiptByIdQuery, GoodsReceiptDto?>
{
    private readonly IGoodsReceiptRepository _grns;
    public GetGoodsReceiptByIdHandler(IGoodsReceiptRepository grns) => _grns = grns;

    public async Task<GoodsReceiptDto?> Handle(GetGoodsReceiptByIdQuery q, CancellationToken ct)
    {
        var grn = await _grns.GetByIdAsync(q.Id, ct);
        return grn is null ? null : GoodsReceiptMapper.ToDto(grn);
    }
}

public class SearchGoodsReceiptsHandler : IRequestHandler<SearchGoodsReceiptsQuery, PagedResult<GoodsReceiptDto>>
{
    private readonly IGoodsReceiptRepository _grns;
    public SearchGoodsReceiptsHandler(IGoodsReceiptRepository grns) => _grns = grns;

    public async Task<PagedResult<GoodsReceiptDto>> Handle(SearchGoodsReceiptsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _grns.SearchAsync(q.PurchaseOrderId, q.VendorId, q.Status, page, pageSize, ct);
        return new PagedResult<GoodsReceiptDto>
        {
            Items = items.Select(GoodsReceiptMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetPurchaseOrderByIdHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto?>
{
    private readonly IPurchaseOrderRepository _orders;
    public GetPurchaseOrderByIdHandler(IPurchaseOrderRepository orders) => _orders = orders;

    public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderByIdQuery q, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(q.Id, ct);
        return po is null ? null : PurchaseOrderMapper.ToDto(po);
    }
}

public class SearchPurchaseOrdersHandler : IRequestHandler<SearchPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _orders;
    public SearchPurchaseOrdersHandler(IPurchaseOrderRepository orders) => _orders = orders;

    public async Task<PagedResult<PurchaseOrderDto>> Handle(SearchPurchaseOrdersQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _orders.SearchAsync(q.VendorId, q.Status, page, pageSize, ct);
        return new PagedResult<PurchaseOrderDto>
        {
            Items = items.Select(PurchaseOrderMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
