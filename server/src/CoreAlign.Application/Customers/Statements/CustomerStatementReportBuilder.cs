using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Customers.Statements;

public static class CustomerStatementReportBuilder
{
    public static ReportDocument Build(CustomerStatementDto statement, Tenant? tenant)
    {
        ArgumentNullException.ThrowIfNull(statement);

        var columns = new List<ReportColumn>
        {
            new("date", "Date / Tarih", ReportColumnType.Date),
            new("kind", "Type / Tip", ReportColumnType.Text),
            new("documentNumber", "Document # / Belge No", ReportColumnType.Text),
            new("description", "Description / Açıklama", ReportColumnType.Text),
            new("debit", "Debit / Borç", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("credit", "Credit / Alacak", ReportColumnType.Currency, ReportColumnAlign.Right),
            new("balance", "Balance / Bakiye", ReportColumnType.Currency, ReportColumnAlign.Right),
        };

        var dataRows = statement.Lines.Select(l => ReportRow.Of(
            (object?)l.OccurredAtUtc,
            l.EntryKind,
            l.DocumentNumber,
            l.Description ?? string.Empty,
            l.Debit,
            l.Credit,
            l.RunningBalance)).ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From("Closing / Kapanış"),
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.Empty,
            ReportCell.From(statement.TotalDebit),
            ReportCell.From(statement.TotalCredit),
            ReportCell.From(statement.ClosingBalance),
        };

        var subtitle = BuildSubtitle(statement);
        var header = new ReportHeader(
            TenantName: tenant?.Name ?? string.Empty,
            TenantLegalName: tenant?.LegalName,
            Title: "Customer statement / Müşteri ekstresi",
            Subtitle: subtitle,
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: statement.FromUtc,
            PeriodToUtc: statement.ToUtc,
            Currency: statement.Currency,
            Locale: tenant?.LocaleCode ?? "tr-TR");

        var notes = BuildNotes(statement);
        return new ReportDocument(header, columns, dataRows, FooterTotals: footer, Notes: notes);
    }

    private static string BuildSubtitle(CustomerStatementDto statement)
    {
        var label = string.IsNullOrWhiteSpace(statement.CustomerCode)
            ? statement.CustomerName
            : $"{statement.CustomerName} ({statement.CustomerCode})";
        return label;
    }

    private static string BuildNotes(CustomerStatementDto statement)
    {
        var fromText = statement.FromUtc?.ToString("yyyy-MM-dd") ?? "-";
        var toText = statement.ToUtc?.ToString("yyyy-MM-dd") ?? "-";
        return string.Concat(
            $"Period / Dönem: {fromText} → {toText}",
            "  |  ",
            $"Opening / Açılış: {statement.OpeningBalance:N4} {statement.Currency}",
            "  |  ",
            $"Closing / Kapanış: {statement.ClosingBalance:N4} {statement.Currency}");
    }
}
