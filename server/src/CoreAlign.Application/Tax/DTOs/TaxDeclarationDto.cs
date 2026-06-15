using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tax.DTOs;

public record TaxDeclarationDto(
    Guid Id,
    int Year,
    int Month,
    TaxDeclarationType DeclarationType,
    TaxDeclarationStatus Status,
    decimal TotalAmount,
    decimal TaxAmount,
    decimal WithholdingAmount,
    string CurrencyCode,
    int LineCount,
    string? FailureReason,
    DateTime? GeneratedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TaxDeclarationLineDto> Lines);

public record TaxDeclarationLineDto(
    Guid Id,
    string? CounterpartyTaxNumber,
    string CounterpartyName,
    int DocumentCount,
    decimal TotalAmount,
    decimal TaxAmount);

public record TaxDeclarationSummaryDto(
    Guid Id,
    int Year,
    int Month,
    TaxDeclarationType DeclarationType,
    TaxDeclarationStatus Status,
    decimal TotalAmount,
    decimal TaxAmount,
    int LineCount,
    DateTime? GeneratedAtUtc,
    DateTime? SubmittedAtUtc);
