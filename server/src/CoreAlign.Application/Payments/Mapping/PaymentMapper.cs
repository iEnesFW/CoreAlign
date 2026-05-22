using CoreAlign.Application.Payments.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Payments.Mapping;

public static class PaymentMapper
{
    public static PaymentDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        PaymentNumber = p.PaymentNumber,
        Direction = p.Direction,
        Status = p.Status,
        CustomerId = p.CustomerId,
        CustomerName = p.Customer?.Name ?? p.CustomerNameSnapshot,
        PaymentDate = p.PaymentDate,
        PostingDate = p.PostingDate,
        Method = p.Method,
        Currency = p.Currency,
        ExchangeRate = p.ExchangeRate,
        Amount = p.Amount,
        AppliedAmount = p.AppliedAmount,
        UnappliedAmount = p.UnappliedAmount,
        BankAccountInfo = p.BankAccountInfo,
        ReferenceNumber = p.ReferenceNumber,
        CheckNumber = p.CheckNumber,
        CheckDueDate = p.CheckDueDate,
        ConfirmedAtUtc = p.ConfirmedAtUtc,
        VoidedAtUtc = p.VoidedAtUtc,
        VoidReason = p.VoidReason,
        Notes = p.Notes,
        Applications = p.Applications.Select(ToApplicationDto).ToList(),
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc,
    };

    public static PaymentSummaryDto ToSummaryDto(Payment p) => new()
    {
        Id = p.Id,
        PaymentNumber = p.PaymentNumber,
        Direction = p.Direction,
        Status = p.Status,
        CustomerId = p.CustomerId,
        CustomerName = p.Customer?.Name ?? p.CustomerNameSnapshot,
        PaymentDate = p.PaymentDate,
        Method = p.Method,
        Amount = p.Amount,
        UnappliedAmount = p.UnappliedAmount,
        Currency = p.Currency,
    };

    public static PaymentSummaryDto ToSummaryDto(PaymentSearchRow r) => new()
    {
        Id = r.Id,
        PaymentNumber = r.PaymentNumber,
        Direction = r.Direction,
        Status = r.Status,
        CustomerId = r.CustomerId,
        CustomerName = r.CustomerName,
        PaymentDate = r.PaymentDate,
        Method = r.Method,
        Amount = r.Amount,
        UnappliedAmount = Math.Max(0m, r.Amount - r.AppliedAmount),
        Currency = r.Currency,
    };

    public static PaymentApplicationDto ToApplicationDto(PaymentApplication a) => new()
    {
        Id = a.Id,
        PaymentId = a.PaymentId,
        PaymentNumber = a.Payment?.PaymentNumber ?? string.Empty,
        PaymentDate = a.Payment?.PaymentDate ?? a.AppliedAtUtc,
        PaymentMethod = a.Payment?.Method ?? default,
        InvoiceId = a.InvoiceId,
        InvoiceNumber = a.Invoice?.InvoiceNumber ?? string.Empty,
        AppliedAmount = a.AppliedAmount,
        AppliedAtUtc = a.AppliedAtUtc,
    };

    public static CustomerLedgerEntryDto ToDto(CustomerLedgerEntry e) => new()
    {
        Id = e.Id,
        CustomerId = e.CustomerId,
        OccurredAtUtc = e.OccurredAtUtc,
        PostingDate = e.PostingDate,
        EntryType = e.EntryType,
        Amount = e.Amount,
        Currency = e.Currency,
        AmountInBase = e.AmountInBase,
        SourceType = e.SourceType,
        SourceDocumentId = e.SourceDocumentId,
        SourceDocumentNumber = e.SourceDocumentNumber,
        RunningBalanceAfter = e.RunningBalanceAfter,
        Description = e.Description,
    };
}
