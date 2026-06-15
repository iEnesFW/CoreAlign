using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Accounting;

public sealed record GlDetailReportQuery(Guid? AccountId, DateTime? FromUtc, DateTime? ToUtc) : IRequest<ReportDocument>;

public sealed class GlDetailReportQueryHandler : IRequestHandler<GlDetailReportQuery, ReportDocument>
{
    private readonly IReportDataReader _reader;
    private readonly IGLAccountRepository _accounts;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public GlDetailReportQueryHandler(
        IReportDataReader reader,
        IGLAccountRepository accounts,
        ITenantRepository tenants,
        ITenantContext tenantContext)
    {
        _reader = reader;
        _accounts = accounts;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    public async Task<ReportDocument> Handle(GlDetailReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var lines = await _reader.GetGlDetailAsync(request.AccountId, request.FromUtc, request.ToUtc, cancellationToken);

        var columns = new List<ReportColumn>
        {
            new("date", "Posting date", ReportColumnType.Date),
            new("journal", "Journal #", ReportColumnType.Text),
            new("source", "Source doc", ReportColumnType.Text),
            new("account", "Account", ReportColumnType.Text),
            new("description", "Description", ReportColumnType.Text),
            new("debit", "Debit", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("credit", "Credit", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("balance", "Running balance", ReportColumnType.Decimal, ReportColumnAlign.Right),
        };

        var groups = new List<ReportGroup>();
        foreach (var accountGroup in lines.GroupBy(l => new { l.AccountCode, l.AccountName }).OrderBy(g => g.Key.AccountCode))
        {
            decimal running = 0m;
            decimal sumDebit = 0m;
            decimal sumCredit = 0m;
            var rows = new List<ReportRow>();
            foreach (var l in accountGroup.OrderBy(l => l.PostingDate).ThenBy(l => l.JournalNumber))
            {
                running += l.Debit - l.Credit;
                sumDebit += l.Debit;
                sumCredit += l.Credit;
                rows.Add(ReportRow.Of(
                    (object?)l.PostingDate,
                    l.JournalNumber,
                    l.SourceDocumentNumber ?? string.Empty,
                    $"{l.AccountCode} {l.AccountName}",
                    l.Description ?? string.Empty,
                    l.Debit,
                    l.Credit,
                    running));
            }
            var groupTotals = new List<ReportCell>
            {
                ReportCell.From("Total"),
                ReportCell.Empty,
                ReportCell.Empty,
                ReportCell.Empty,
                ReportCell.Empty,
                ReportCell.From(sumDebit),
                ReportCell.From(sumCredit),
                ReportCell.From(running),
            };
            groups.Add(new ReportGroup($"{accountGroup.Key.AccountCode} — {accountGroup.Key.AccountName}", rows, groupTotals));
        }

        string? subtitle = null;
        if (request.AccountId.HasValue)
        {
            var acc = await _accounts.GetByIdAsync(request.AccountId.Value, cancellationToken);
            if (acc is not null)
            {
                subtitle = $"Account: {acc.Code} {acc.Name}";
            }
        }

        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "GL detail",
            Subtitle: subtitle,
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: request.FromUtc,
            PeriodToUtc: request.ToUtc,
            Currency: tenant?.DefaultCurrency ?? "TRY",
            Locale: tenant?.LocaleCode ?? "tr-TR");

        return new ReportDocument(header, columns, Rows: Array.Empty<ReportRow>(), Groups: groups);
    }
}
