using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Reports;

public sealed class ReportDataReader : IReportDataReader
{
    private readonly CoreAlignDbContext _context;

    public ReportDataReader(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GlDetailLineRow>> GetGlDetailAsync(
        Guid? accountId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = from line in _context.JournalLines.AsNoTracking()
                    join entry in _context.JournalEntries.AsNoTracking()
                        on line.JournalEntryId equals entry.Id
                    where entry.Status == JournalEntryStatus.Posted
                    select new { line, entry };

        if (accountId.HasValue)
        {
            query = query.Where(x => x.line.AccountId == accountId.Value);
        }
        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.entry.PostingDate >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(x => x.entry.PostingDate <= toUtc.Value);
        }

        return await query
            .OrderBy(x => x.entry.PostingDate)
            .ThenBy(x => x.entry.Number)
            .ThenBy(x => x.line.LineNumber)
            .Select(x => new GlDetailLineRow(
                x.entry.PostingDate,
                x.entry.Number,
                x.entry.Reference,
                x.line.Description ?? x.entry.Description,
                x.entry.SourceDocumentNumber,
                x.line.AccountId,
                x.line.AccountCode,
                x.line.AccountName,
                x.line.Debit,
                x.line.Credit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CashFlowRow>> GetCashFlowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        // Section is driven by the COUNTERPART (non-cash) leg of each movement:
        // a customer receipt relieves Accounts Receivable (TDHP 120), a vendor
        // payment relieves Accounts Payable (TDHP 320) — both day-to-day trade,
        // hence Operating. The sectionizer keeps the rule in one place so other
        // counterparts (loan repayments, capital, asset purchases) classify
        // consistently if those payment streams gain a counterpart code.
        var customerReceiptSection = CashFlowSectionizer.SectionForCounterpart(
            GLPostingDefaults.CodeFor(GLPostingKey.AccountsReceivable));
        var vendorPaymentSection = CashFlowSectionizer.SectionForCounterpart(
            GLPostingDefaults.CodeFor(GLPostingKey.AccountsPayable));

        var customerInflows = (await _context.Payments.AsNoTracking()
            .Where(p => p.Direction == PaymentDirection.CustomerReceipt
                && p.Status != PaymentStatus.Draft
                && p.Status != PaymentStatus.Void
                && p.PaymentDate >= fromUtc
                && p.PaymentDate <= toUtc)
            .Select(p => new
            {
                p.PaymentDate,
                p.CustomerNameSnapshot,
                p.PaymentNumber,
                p.Amount,
                p.Currency,
            })
            .ToListAsync(cancellationToken))
            .Select(p => new CashFlowRow(
                p.PaymentDate,
                customerReceiptSection,
                "Customer receipts",
                p.CustomerNameSnapshot,
                p.PaymentNumber,
                p.Amount,
                p.Currency))
            .ToList();

        var vendorOutflows = (await _context.VendorPayments.AsNoTracking()
            .Where(p => !p.IsVoided
                && p.PaymentDate >= fromUtc
                && p.PaymentDate <= toUtc)
            .Select(p => new
            {
                p.PaymentDate,
                p.VendorName,
                p.PaymentNumber,
                p.Amount,
                p.Currency,
            })
            .ToListAsync(cancellationToken))
            .Select(p => new CashFlowRow(
                p.PaymentDate,
                vendorPaymentSection,
                "Vendor payments",
                p.VendorName,
                p.PaymentNumber,
                -p.Amount,
                p.Currency))
            .ToList();

        return customerInflows.Concat(vendorOutflows)
            .OrderBy(r => r.OccurredAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<PurchaseByVendorRow>> GetPurchaseByVendorAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders.AsNoTracking()
            .Where(po => po.OrderDate >= fromUtc
                && po.OrderDate <= toUtc
                && po.Status != PurchaseOrderStatus.Cancelled)
            .GroupBy(po => new { po.VendorId, po.VendorName, po.Currency })
            .Select(g => new PurchaseByVendorRow(
                g.Key.VendorId,
                g.Key.VendorName,
                g.Key.Currency,
                g.Count(),
                g.Sum(po => po.Subtotal),
                g.Sum(po => po.TaxTotal),
                g.Sum(po => po.Total)))
            .OrderByDescending(r => r.Total)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseByProductRow>> GetPurchaseByProductAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = from line in _context.PurchaseOrderLines.AsNoTracking()
                    join po in _context.PurchaseOrders.AsNoTracking() on line.PurchaseOrderId equals po.Id
                    where po.OrderDate >= fromUtc
                        && po.OrderDate <= toUtc
                        && po.Status != PurchaseOrderStatus.Cancelled
                    select new { line, po };

        return await query
            .GroupBy(x => new { x.line.ProductId, x.line.ProductSku, x.line.ProductName, x.po.Currency })
            .Select(g => new PurchaseByProductRow(
                g.Key.ProductId,
                g.Key.ProductSku,
                g.Key.ProductName,
                g.Key.Currency,
                g.Sum(x => x.line.Quantity),
                g.Sum(x => x.line.LineSubtotal),
                g.Sum(x => x.line.LineTotal)))
            .OrderByDescending(r => r.Total)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpenPoRow>> GetOpenPurchaseOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.PurchaseOrders.AsNoTracking()
            .Where(po => po.Status != PurchaseOrderStatus.Closed
                && po.Status != PurchaseOrderStatus.Cancelled)
            .OrderBy(po => po.OrderDate)
            .Select(po => new
            {
                po.Id,
                po.PoNumber,
                po.OrderDate,
                po.ExpectedDate,
                po.VendorId,
                po.VendorName,
                po.Status,
                po.Currency,
                po.Total,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(po => new OpenPoRow(
            po.Id,
            po.PoNumber,
            po.OrderDate,
            po.ExpectedDate,
            po.VendorId,
            po.VendorName,
            po.Status.ToString(),
            po.Currency,
            po.Total,
            Math.Max(0, (int)(now - po.OrderDate).TotalDays))).ToList();
    }
}
