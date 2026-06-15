using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Purchase;

public sealed record PurchaseByProductReportQuery(DateTime FromUtc, DateTime ToUtc) : IRequest<ReportDocument>;

public sealed class PurchaseByProductReportQueryHandler : IRequestHandler<PurchaseByProductReportQuery, ReportDocument>
{
    private readonly IReportDataReader _reader;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public PurchaseByProductReportQueryHandler(IReportDataReader reader, ITenantRepository tenants, ITenantContext tenantContext)
    {
        _reader = reader;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(PurchaseByProductReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var rows = await _reader.GetPurchaseByProductAsync(request.FromUtc, request.ToUtc, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("sku", "SKU", ReportColumnType.Text),
            new("product", "Product", ReportColumnType.Text),
            new("currency", "Currency", ReportColumnType.Text),
            new("qty", "Quantity", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("subtotal", "Subtotal", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("total", "Total", ReportColumnType.Currency, ReportColumnAlign.Right),
        };

        var dataRows = rows.Select(r => ReportRow.Of(
            r.Sku,
            r.ProductName,
            r.Currency,
            r.QuantityOrdered,
            r.Subtotal,
            r.Total)).ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(rows.Sum(r => r.QuantityOrdered)),
            ReportCell.From(rows.Sum(r => r.Subtotal)),
            ReportCell.From(rows.Sum(r => r.Total)),
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Purchase by product",
            Subtitle: "Aggregated purchase order line totals",
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: request.FromUtc,
            PeriodToUtc: request.ToUtc,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, dataRows, FooterTotals: footer);
    }
}
