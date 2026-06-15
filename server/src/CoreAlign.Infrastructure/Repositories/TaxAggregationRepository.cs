using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class TaxAggregationRepository : ITaxAggregationRepository
{
    private static readonly InvoiceStatus[] EligibleInvoiceStatuses =
    {
        InvoiceStatus.Issued,
        InvoiceStatus.Sent,
        InvoiceStatus.PartiallyPaid,
        InvoiceStatus.Paid,
        InvoiceStatus.Overdue
    };

    private static readonly VendorBillStatus[] EligibleVendorBillStatuses =
    {
        VendorBillStatus.Posted,
        VendorBillStatus.PartiallyPaid,
        VendorBillStatus.Paid
    };

    private readonly CoreAlignDbContext _context;

    public TaxAggregationRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InvoiceTaxAggregateRow>> GetInvoiceTaxRowsForPeriodAsync(
        DateTime startUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.IssueDate >= startUtc
                        && i.IssueDate < endExclusiveUtc
                        && EligibleInvoiceStatuses.Contains(i.Status))
            .Select(i => new InvoiceTaxAggregateRow(
                i.Id,
                i.TaxableTotal,
                i.TaxTotal,
                i.WithholdingTotal,
                i.TaxBreakdownJson))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<IReadOnlyList<CustomerInvoiceAggregateRow>> GetCustomerInvoiceAggregatesAsync(
        DateTime startUtc,
        DateTime endExclusiveUtc,
        decimal minThreshold,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.IssueDate >= startUtc
                        && i.IssueDate < endExclusiveUtc
                        && EligibleInvoiceStatuses.Contains(i.Status))
            .GroupBy(i => new { i.CustomerId, CustomerName = i.Customer.Name, i.Customer.TaxNumber })
            .Select(g => new CustomerInvoiceAggregateRow(
                g.Key.CustomerId,
                g.Key.CustomerName,
                g.Key.TaxNumber,
                g.Count(),
                g.Sum(i => i.Total),
                g.Sum(i => i.TaxTotal)))
            .Where(r => r.TotalAmount >= minThreshold)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<IReadOnlyList<VendorBillAggregateRow>> GetVendorBillAggregatesAsync(
        DateTime startUtc,
        DateTime endExclusiveUtc,
        decimal minThreshold,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.VendorBills
            .AsNoTracking()
            .Where(b => b.BillDate >= startUtc
                        && b.BillDate < endExclusiveUtc
                        && EligibleVendorBillStatuses.Contains(b.Status))
            .GroupBy(b => new { b.VendorId, VendorName = b.Vendor.Name, b.Vendor.TaxNumber })
            .Select(g => new VendorBillAggregateRow(
                g.Key.VendorId,
                g.Key.VendorName,
                g.Key.TaxNumber,
                g.Count(),
                g.Sum(b => b.Total),
                g.Sum(b => b.TaxAmount)))
            .Where(r => r.TotalAmount >= minThreshold)
            .ToListAsync(cancellationToken);
        return rows;
    }
}
