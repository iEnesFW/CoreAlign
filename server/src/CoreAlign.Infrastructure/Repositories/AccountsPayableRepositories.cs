using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class VendorBillRepository : IVendorBillRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorBillRepository(CoreAlignDbContext context) => _context = context;

    public Task<VendorBill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.VendorBills
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> BillNumberExistsAsync(Guid vendorId, string billNumber, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.VendorBills.AnyAsync(
            b => b.VendorId == vendorId && b.BillNumber == billNumber && (excludeId == null || b.Id != excludeId),
            cancellationToken);

    public async Task<(IReadOnlyList<VendorBill> Items, int Total)> SearchAsync(
        Guid? vendorId,
        VendorBillStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.VendorBills.AsNoTracking().AsQueryable();
        if (vendorId.HasValue) query = query.Where(b => b.VendorId == vendorId.Value);
        if (status.HasValue) query = query.Where(b => b.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.BillDate)
            .ThenByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(VendorBill bill, CancellationToken cancellationToken = default) =>
        await _context.VendorBills.AddAsync(bill, cancellationToken);

    public void Update(VendorBill bill) => _context.VendorBills.Update(bill);

    public async Task<IReadOnlyList<VendorAgingRow>> GetAgingBucketsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        // In-memory bucketing over a SLIM, server-filtered projection (only open
        // payables, 5 small fields). Kept in-memory deliberately: (1) the open
        // vendor-bill set is bounded — a business owes a bounded number of vendors
        // and bills get paid down, unlike the unbounded customer-invoice set (AR
        // aging is server-side, see ReportRepository.GetAgingBucketsAsync); (2)
        // server-side GroupBy on VendorBill does NOT translate in EF Core 10 /
        // Npgsql — verified via ToQueryString, even a plain g.Sum(b => b.Total)
        // over a VendorBill group throws, while the identical shape on Invoice
        // translates. Don't re-attempt without first fixing that model quirk.
        // AmountDue is a computed property, hence (Total - AmountPaid); due date
        // falls back to the bill date when none is set.
        var open = await _context.VendorBills.AsNoTracking()
            .Where(b => (b.Status == VendorBillStatus.Posted || b.Status == VendorBillStatus.PartiallyPaid)
                && b.Total - b.AmountPaid > 0m)
            .Select(b => new
            {
                b.VendorId,
                b.VendorName,
                b.Currency,
                DueDate = b.DueDate ?? b.BillDate,
                Open = b.Total - b.AmountPaid,
            })
            .ToListAsync(cancellationToken);

        var d30 = asOfUtc.AddDays(-30);
        var d60 = asOfUtc.AddDays(-60);
        var d90 = asOfUtc.AddDays(-90);

        return open
            .GroupBy(b => new { b.VendorId, b.VendorName, b.Currency })
            .Select(g => new VendorAgingRow(
                g.Key.VendorId,
                g.Key.VendorName,
                g.Key.Currency,
                g.Where(x => x.DueDate >= asOfUtc).Sum(x => x.Open),
                g.Where(x => x.DueDate < asOfUtc && x.DueDate >= d30).Sum(x => x.Open),
                g.Where(x => x.DueDate < d30 && x.DueDate >= d60).Sum(x => x.Open),
                g.Where(x => x.DueDate < d60 && x.DueDate >= d90).Sum(x => x.Open),
                g.Where(x => x.DueDate < d90).Sum(x => x.Open)))
            .OrderBy(r => r.VendorName)
            .ToList();
    }
}

public class VendorPaymentRepository : IVendorPaymentRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorPaymentRepository(CoreAlignDbContext context) => _context = context;

    public Task<VendorPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.VendorPayments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<VendorPayment> Items, int Total)> SearchAsync(
        Guid? vendorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.VendorPayments.AsNoTracking().AsQueryable();
        if (vendorId.HasValue) query = query.Where(p => p.VendorId == vendorId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(VendorPayment payment, CancellationToken cancellationToken = default) =>
        await _context.VendorPayments.AddAsync(payment, cancellationToken);

    public void Update(VendorPayment payment) => _context.VendorPayments.Update(payment);
}

public class VendorPaymentApplicationRepository : IVendorPaymentApplicationRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorPaymentApplicationRepository(CoreAlignDbContext context) => _context = context;

    public Task<VendorPaymentApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.VendorPaymentApplications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VendorPaymentApplication>> GetByVendorBillAsync(Guid vendorBillId, CancellationToken cancellationToken = default) =>
        await _context.VendorPaymentApplications
            .AsNoTracking()
            .Where(a => a.VendorBillId == vendorBillId)
            .OrderByDescending(a => a.AppliedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VendorPaymentApplication>> GetByVendorPaymentAsync(Guid vendorPaymentId, CancellationToken cancellationToken = default) =>
        await _context.VendorPaymentApplications
            .AsNoTracking()
            .Where(a => a.VendorPaymentId == vendorPaymentId)
            .OrderByDescending(a => a.AppliedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<VendorPaymentApplication?> GetByPaymentAndBillAsync(Guid vendorPaymentId, Guid vendorBillId, CancellationToken cancellationToken = default) =>
        _context.VendorPaymentApplications
            .FirstOrDefaultAsync(a => a.VendorPaymentId == vendorPaymentId && a.VendorBillId == vendorBillId, cancellationToken);

    public async Task AddAsync(VendorPaymentApplication application, CancellationToken cancellationToken = default) =>
        await _context.VendorPaymentApplications.AddAsync(application, cancellationToken);

    public void Remove(VendorPaymentApplication application) => _context.VendorPaymentApplications.Remove(application);
}

public class ThreeWayMatchReader : IThreeWayMatchReader
{
    private readonly CoreAlignDbContext _context;
    public ThreeWayMatchReader(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<ThreeWayMatchRow>> GetMismatchesAsync(
        Guid? vendorId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var posQuery = _context.PurchaseOrders
            .Include(p => p.Lines)
            .AsNoTracking()
            .AsQueryable();
        if (vendorId.HasValue) posQuery = posQuery.Where(p => p.VendorId == vendorId.Value);
        if (fromUtc.HasValue) posQuery = posQuery.Where(p => p.OrderDate >= fromUtc.Value);
        if (toUtc.HasValue) posQuery = posQuery.Where(p => p.OrderDate <= toUtc.Value);

        var pos = await posQuery.ToListAsync(cancellationToken);
        if (pos.Count == 0) return Array.Empty<ThreeWayMatchRow>();

        var poIds = pos.Select(p => p.Id).ToList();
        var bills = await _context.VendorBills.AsNoTracking()
            .Where(b => b.PurchaseOrderId != null && poIds.Contains(b.PurchaseOrderId.Value)
                && b.Status != Domain.Enums.VendorBillStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var billedByPo = bills
            .GroupBy(b => b.PurchaseOrderId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Subtotal));

        // Prefer authoritative per-line billed quantity when the bills carry
        // VendorBillLine rows; legacy / PO-less bills have none and fall back to
        // the subtotal-ratio proration below.
        var billIds = bills.Select(b => b.Id).ToList();
        var billedQtyByPoLine = billIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : (await _context.VendorBillLines.AsNoTracking()
                .Where(l => l.PurchaseOrderLineId != null && billIds.Contains(l.VendorBillId))
                .GroupBy(l => l.PurchaseOrderLineId!.Value)
                .Select(g => new { PoLineId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.PoLineId, x => x.Qty);

        var result = new List<ThreeWayMatchRow>();
        foreach (var po in pos)
        {
            var billedAmount = billedByPo.TryGetValue(po.Id, out var amt) ? amt : 0m;
            var billedRatio = po.Subtotal == 0m ? 0m : billedAmount / po.Subtotal;
            foreach (var line in po.Lines)
            {
                var lineExpected = line.Quantity;
                var lineReceived = line.QuantityReceived;
                var lineBilledQty = billedQtyByPoLine.TryGetValue(line.Id, out var realQty)
                    ? Math.Round(realQty, 4)
                    : Math.Round(lineExpected * billedRatio, 4);

                var discrepancies = new List<string>();
                if (lineReceived < lineExpected) discrepancies.Add("UnderReceived");
                if (lineReceived > lineExpected) discrepancies.Add("OverReceived");
                if (lineBilledQty < lineReceived - 0.0001m) discrepancies.Add("UnderBilled");
                if (lineBilledQty > lineReceived + 0.0001m) discrepancies.Add("OverBilled");
                if (billedAmount > po.Subtotal + 0.0001m) discrepancies.Add("BillExceedsPo");

                if (discrepancies.Count == 0) continue;

                result.Add(new ThreeWayMatchRow(
                    po.Id,
                    po.PoNumber,
                    po.VendorId,
                    po.VendorName,
                    po.Currency,
                    line.ProductId,
                    line.ProductSku,
                    line.ProductName,
                    lineExpected,
                    lineReceived,
                    lineBilledQty,
                    Math.Round(line.LineSubtotal, 4),
                    Math.Round(line.LineSubtotal * billedRatio, 4),
                    discrepancies));
            }
        }
        return result;
    }
}

public class StockCountRepository : IStockCountRepository
{
    private readonly CoreAlignDbContext _context;
    public StockCountRepository(CoreAlignDbContext context) => _context = context;

    public Task<StockCount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.StockCounts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<StockCount?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> CountNumberExistsAsync(string countNumber, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.StockCounts.AnyAsync(
            c => c.CountNumber == countNumber && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    public async Task<(IReadOnlyList<StockCountSearchRow> Items, int Total)> SearchAsync(
        Guid? warehouseId,
        Domain.Enums.StockCountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockCounts.AsNoTracking().AsQueryable();
        if (warehouseId.HasValue) query = query.Where(c => c.WarehouseId == warehouseId.Value);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        // Slim projection: totals via correlated SUM subqueries on the lines, so the
        // list never joins/materializes stock_count_lines (warehouse-wide count ~20k
        // lines × pageSize would be a cartesian blow-up). TotalVariance* are computed
        // properties on the entity (Lines.Sum), inlined here so they translate.
        var items = await query
            .OrderByDescending(c => c.PlannedAtUtc)
            .ThenByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new StockCountSearchRow(
                c.Id,
                c.CountNumber,
                c.WarehouseId,
                c.WarehouseCode,
                c.WarehouseName,
                c.Status,
                c.PlannedAtUtc,
                c.CountingStartedAtUtc,
                c.ReconciledAtUtc,
                c.PostedAtUtc,
                c.PlannedByUserId,
                c.PostedByUserId,
                c.Notes,
                c.Lines.Sum(l => l.VarianceQuantity),
                c.Lines.Sum(l => l.VarianceCost),
                c.Lines.Count,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(StockCount stockCount, CancellationToken cancellationToken = default) =>
        await _context.StockCounts.AddAsync(stockCount, cancellationToken);

    public void Update(StockCount stockCount) => _context.StockCounts.Update(stockCount);
    public void Remove(StockCount stockCount) => _context.StockCounts.Remove(stockCount);
}
