using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Accounting;

public sealed record ApAgingReportQuery(DateTime? AsOfUtc) : IRequest<ReportDocument>;

public sealed class ApAgingReportQueryHandler : IRequestHandler<ApAgingReportQuery, ReportDocument>
{
    private readonly IVendorBillRepository _bills;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public ApAgingReportQueryHandler(IVendorBillRepository bills, ITenantRepository tenants, ITenantContext tenantContext)
    {
        _bills = bills;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(ApAgingReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var asOf = request.AsOfUtc ?? DateTime.UtcNow;
        var buckets = await _bills.GetAgingBucketsAsync(asOf, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("vendor", "Vendor", ReportColumnType.Text),
            new("currency", "Currency", ReportColumnType.Text),
            new("current", "Current", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("d1to30", "1-30", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("d31to60", "31-60", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("d61to90", "61-90", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("d90plus", "90+", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("total", "Total outstanding", ReportColumnType.Currency, ReportColumnAlign.Right),
        };

        var rows = buckets
            .Select(b => new
            {
                Row = ReportRow.Of(
                    b.VendorName,
                    b.Currency,
                    b.Current,
                    b.Days1To30,
                    b.Days31To60,
                    b.Days61To90,
                    b.DaysOver90,
                    b.Current + b.Days1To30 + b.Days31To60 + b.Days61To90 + b.DaysOver90),
                Outstanding = b.Current + b.Days1To30 + b.Days31To60 + b.Days61To90 + b.DaysOver90,
                Bucket = b,
            })
            .Where(x => x.Outstanding > 0m)
            .OrderByDescending(x => x.Outstanding)
            .ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.Empty,
            ReportCell.From(rows.Sum(x => x.Bucket.Current)),
            ReportCell.From(rows.Sum(x => x.Bucket.Days1To30)),
            ReportCell.From(rows.Sum(x => x.Bucket.Days31To60)),
            ReportCell.From(rows.Sum(x => x.Bucket.Days61To90)),
            ReportCell.From(rows.Sum(x => x.Bucket.DaysOver90)),
            ReportCell.From(rows.Sum(x => x.Outstanding)),
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "AP aging",
            Subtitle: "Vendor payables by aging bucket",
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodToUtc: asOf,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, rows.Select(x => x.Row).ToList(), FooterTotals: footer);
    }
}
