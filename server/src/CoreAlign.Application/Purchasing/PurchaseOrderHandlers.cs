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
    private readonly IAllocationService _allocation;
    private readonly IWarehouseRepository _warehouses;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public ReceivePurchaseOrderHandler(
        IPurchaseOrderRepository orders,
        IAllocationService allocation,
        IWarehouseRepository warehouses,
        IGLPostingOutbox outbox,
        IUnitOfWork uow)
    {
        _orders = orders;
        _allocation = allocation;
        _warehouses = warehouses;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<PurchaseOrderDto> Handle(ReceivePurchaseOrderCommand c, CancellationToken ct)
    {
        var po = await _orders.GetByIdAsync(c.Id, ct) ?? throw new PurchaseOrderNotFoundException();

        var warehouseId = c.WarehouseId ?? po.WarehouseId
            ?? (await _warehouses.GetDefaultAsync(ct))?.Id
            ?? throw new NoWarehouseConfiguredException();

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
                SourceReference: po.PoNumber,
                LotId: null,
                SerialNumber: null,
                ReasonCodeId: null,
                Notes: c.Notes ?? $"Mal kabul (PO {po.PoNumber})"), ct);

            // Inventory recognition: goods enter stock against the GR/IR clearing
            // account, which the vendor bill later settles. One entry per movement
            // (idempotency key = movement id).
            await _outbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.GoodsReceipt, movement.Id, po.PoNumber, DateTime.UtcNow.Date,
                JournalEntryType.Mahsup, $"Mal kabul (PO {po.PoNumber})",
                new[]
                {
                    new GLPostingLine(GLPostingKey.Inventory, movement.TotalCost, 0m),
                    new GLPostingLine(GLPostingKey.GoodsReceiptClearing, 0m, movement.TotalCost),
                },
                po.Currency, po.ExchangeRate), ct);
        }

        _orders.Update(po);
        await _uow.SaveChangesAsync(ct);
        return PurchaseOrderMapper.ToDto(po);
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
