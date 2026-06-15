using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Accounting;

public sealed record CashFlowReportQuery(DateTime FromUtc, DateTime ToUtc) : IRequest<ReportDocument>;

public sealed class CashFlowReportQueryHandler : IRequestHandler<CashFlowReportQuery, ReportDocument>
{
    private readonly IReportDataReader _reader;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public CashFlowReportQueryHandler(IReportDataReader reader, ITenantRepository tenants, ITenantContext tenantContext)
    {
        _reader = reader;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(CashFlowReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var rows = await _reader.GetCashFlowAsync(request.FromUtc, request.ToUtc, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("date", "Date", ReportColumnType.Date),
            new("category", "Category", ReportColumnType.Text),
            new("description", "Description", ReportColumnType.Text),
            new("reference", "Reference", ReportColumnType.Text),
            new("currency", "Currency", ReportColumnType.Text),
            new("amount", "Amount", ReportColumnType.Currency, ReportColumnAlign.Right),
        };

        var groups = rows
            .GroupBy(r => r.Section)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var sectionRows = g
                    .OrderBy(r => r.OccurredAtUtc)
                    .Select(r => ReportRow.Of(
                        (object?)r.OccurredAtUtc,
                        r.Category,
                        r.Description,
                        r.Reference,
                        r.Currency,
                        r.Amount))
                    .ToList();
                var sectionTotal = g.Sum(r => r.Amount);
                var footer = new List<ReportCell>
                {
                    ReportCell.From($"{g.Key} total"),
                    ReportCell.Empty,
                    ReportCell.Empty,
                    ReportCell.Empty,
                    ReportCell.Empty,
                    ReportCell.From(sectionTotal),
                };
                return new ReportGroup(g.Key, sectionRows, footer);
            })
            .ToList();

        var netTotal = rows.Sum(r => r.Amount);
        var docFooter = new List<ReportCell>
        {
            ReportCell.From("Net change in cash"),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(netTotal),
        };

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Cash flow",
            Subtitle: "Operating / Investing / Financing sections",
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: request.FromUtc,
            PeriodToUtc: request.ToUtc,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, Rows: Array.Empty<ReportRow>(), Groups: groups, FooterTotals: docFooter);
    }
}
