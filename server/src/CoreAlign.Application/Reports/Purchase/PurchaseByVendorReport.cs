using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Purchase;

public sealed record PurchaseByVendorReportQuery(DateTime FromUtc, DateTime ToUtc) : IRequest<ReportDocument>;

public sealed class PurchaseByVendorReportQueryHandler : IRequestHandler<PurchaseByVendorReportQuery, ReportDocument>
{
    private readonly IReportDataReader _reader;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public PurchaseByVendorReportQueryHandler(IReportDataReader reader, ITenantRepository tenants, ITenantContext tenantContext)
    {
        _reader = reader;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(PurchaseByVendorReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var rows = await _reader.GetPurchaseByVendorAsync(request.FromUtc, request.ToUtc, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("vendor", "Vendor", ReportColumnType.Text),
            new("currency", "Currency", ReportColumnType.Text),
            new("poCount", "PO count", ReportColumnType.Integer, ReportColumnAlign.Right),
            new("subtotal", "Subtotal", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("tax", "Tax", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("total", "Total", ReportColumnType.Currency, ReportColumnAlign.Right),
        };

        var dataRows = rows.Select(r => ReportRow.Of(
            r.VendorName,
            r.Currency,
            r.PoCount,
            r.Subtotal,
            r.TaxTotal,
            r.Total)).ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.Empty,
            ReportCell.From(rows.Sum(r => r.PoCount)),
            ReportCell.From(rows.Sum(r => r.Subtotal)),
            ReportCell.From(rows.Sum(r => r.TaxTotal)),
            ReportCell.From(rows.Sum(r => r.Total)),
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Purchase by vendor",
            Subtitle: "Aggregated purchase order totals",
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: request.FromUtc,
            PeriodToUtc: request.ToUtc,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, dataRows, FooterTotals: footer);
    }
}
