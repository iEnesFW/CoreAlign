using CoreAlign.Application.Tax.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tax;

public static class TaxDeclarationMapper
{
    public static TaxDeclarationDto ToDto(TaxDeclaration declaration)
    {
        var lines = declaration.Lines
            .Select(l => new TaxDeclarationLineDto(
                l.Id,
                l.CounterpartyTaxNumber,
                l.CounterpartyName,
                l.DocumentCount,
                l.TotalAmount,
                l.TaxAmount))
            .ToList();

        return new TaxDeclarationDto(
            declaration.Id,
            declaration.Year,
            declaration.Month,
            declaration.DeclarationType,
            declaration.Status,
            declaration.TotalAmount,
            declaration.TaxAmount,
            declaration.WithholdingAmount,
            declaration.CurrencyCode,
            declaration.LineCount,
            declaration.FailureReason,
            declaration.GeneratedAtUtc,
            declaration.SubmittedAtUtc,
            declaration.AcceptedAtUtc,
            declaration.CreatedAtUtc,
            declaration.UpdatedAtUtc,
            lines);
    }

    public static TaxDeclarationSummaryDto ToSummaryDto(TaxDeclaration declaration) =>
        new(
            declaration.Id,
            declaration.Year,
            declaration.Month,
            declaration.DeclarationType,
            declaration.Status,
            declaration.TotalAmount,
            declaration.TaxAmount,
            declaration.LineCount,
            declaration.GeneratedAtUtc,
            declaration.SubmittedAtUtc);
}
