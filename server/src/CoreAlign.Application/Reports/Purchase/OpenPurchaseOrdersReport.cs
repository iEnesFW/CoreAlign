using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Purchase;

public sealed record OpenPurchaseOrdersReportQuery : IRequest<ReportDocument>;

public sealed class OpenPurchaseOrdersReportQueryHandler : IRequestHandler<OpenPurchaseOrdersReportQuery, ReportDocument>
{
    private readonly IReportDataReader _reader;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public OpenPurchaseOrdersReportQueryHandler(IReportDataReader reader, ITenantRepository tenants, ITenantContext tenantContext)
    {
        _reader = reader;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(OpenPurchaseOrdersReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var rows = await _reader.GetOpenPurchaseOrdersAsync(cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("poNumber", "PO #", ReportColumnType.Text),
            new("orderDate", "Order date", ReportColumnType.Date),
            new("expectedDate", "Expected date", ReportColumnType.Date),
            new("vendor", "Vendor", ReportColumnType.Text),
            new("status", "Status", ReportColumnType.Text),
            new("currency", "Currency", ReportColumnType.Text),
            new("total", "Total", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("ageDays", "Age (days)", ReportColumnType.Integer, ReportColumnAlign.Right),
        };

        var dataRows = rows.Select(r => ReportRow.Of(
            r.PoNumber,
            (object?)r.OrderDate,
            (object?)r.ExpectedDate,
            r.VendorName,
            r.Status,
            r.Currency,
            r.Total,
            r.AgeDays)).ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From($"{rows.Count} open"),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(rows.Sum(r => r.Total)),
            ReportCell.Empty,
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Open purchase orders",
            Subtitle: "Excludes Closed and Cancelled POs",
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodToUtc: DateTime.UtcNow,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, dataRows, FooterTotals: footer);
    }
}
