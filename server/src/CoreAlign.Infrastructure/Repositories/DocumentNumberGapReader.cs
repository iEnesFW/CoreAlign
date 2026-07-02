using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAlign.Infrastructure.Repositories;

public class DocumentNumberGapReader : IDocumentNumberGapReader
{
    private readonly CoreAlignDbContext _context;

    private static readonly IReadOnlyDictionary<DocumentSequenceType, (string Table, string Column)> SourceMap =
        new Dictionary<DocumentSequenceType, (string, string)>
        {
            [DocumentSequenceType.OrderNumber] = ("orders", "order_number"),
            [DocumentSequenceType.InvoiceNumber] = ("invoices", "invoice_number"),
            [DocumentSequenceType.QuoteNumber] = ("quotes", "quote_number"),
            [DocumentSequenceType.PurchaseOrderNumber] = ("purchase_orders", "po_number"),
            [DocumentSequenceType.PaymentNumber] = ("payments", "payment_number"),
            [DocumentSequenceType.ShipmentNumber] = ("shipments", "shipment_number"),
            [DocumentSequenceType.GoodsReceiptNumber] = ("goods_receipts", "grn_number"),
        };

    public DocumentNumberGapReader(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DocumentNumberGapRow>> GetGapsAsync(
        Guid tenantId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql())
        {
            return Array.Empty<DocumentNumberGapRow>();
        }

        var types = SourceMap.Keys.ToArray();
        var sequences = await _context.Set<DocumentSequence>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && types.Contains(s.Type))
            .ToListAsync(cancellationToken);

        var rows = new List<DocumentNumberGapRow>();

        foreach (var sequence in sequences)
        {
            var effectiveYear = year ?? sequence.CurrentYear;
            var expected = effectiveYear == sequence.CurrentYear ? sequence.NextNumber - 1 : 0;
            if (expected <= 0)
            {
                continue;
            }

            var (table, column) = SourceMap[sequence.Type];
            var yearText = effectiveYear.ToString("D4");

            var aggregateSql = $@"
SELECT count(DISTINCT split_part({column}, '-', 3)::bigint) AS ""Used"",
       COALESCE(max(split_part({column}, '-', 3)::bigint), 0) AS ""MaxUsed""
FROM {table}
WHERE tenant_id = @t
  AND split_part({column}, '-', 2) = @yr
  AND split_part({column}, '-', 3) ~ '^[0-9]+$'";

            var scalar = await _context.Database
                .SqlQueryRaw<GapScalar>(
                    aggregateSql,
                    new NpgsqlParameter("t", tenantId),
                    new NpgsqlParameter("yr", yearText))
                .ToListAsync(cancellationToken);

            var used = scalar.Count > 0 ? scalar[0].Used : 0;
            var maxUsed = scalar.Count > 0 ? scalar[0].MaxUsed : 0;
            var gap = expected - used;

            var missing = new List<long>();
            if (gap > 0)
            {
                var missingSql = $@"
SELECT gs AS ""Value""
FROM generate_series(1, @expected) gs
WHERE NOT EXISTS (
  SELECT 1 FROM {table}
  WHERE tenant_id = @t
    AND split_part({column}, '-', 2) = @yr
    AND split_part({column}, '-', 3) ~ '^[0-9]+$'
    AND split_part({column}, '-', 3)::bigint = gs)
ORDER BY gs
LIMIT 100";

                missing = await _context.Database
                    .SqlQueryRaw<long>(
                        missingSql,
                        new NpgsqlParameter("expected", expected),
                        new NpgsqlParameter("t", tenantId),
                        new NpgsqlParameter("yr", yearText))
                    .ToListAsync(cancellationToken);
            }

            rows.Add(new DocumentNumberGapRow(
                sequence.Type.ToString(),
                sequence.Prefix,
                effectiveYear,
                expected,
                used,
                maxUsed,
                gap < 0 ? 0 : gap,
                missing));
        }

        return rows
            .OrderByDescending(r => r.GapCount)
            .ThenBy(r => r.DocumentType)
            .ToList();
    }

    private sealed record GapScalar(long Used, long MaxUsed);
}
