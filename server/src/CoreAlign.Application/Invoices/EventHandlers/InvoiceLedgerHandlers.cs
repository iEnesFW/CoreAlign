using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.EventHandlers;

internal static class LedgerPostingHelpers
{
    public static async Task PostAsync(
        ICustomerLedgerRepository ledger,
        Guid tenantId,
        Guid customerId,
        DateTime occurredAtUtc,
        DateTime postingDate,
        LedgerEntryType entryType,
        decimal amount,
        string currency,
        LedgerSourceType sourceType,
        Guid? sourceDocumentId,
        string? sourceDocumentNumber,
        string? description,
        CancellationToken cancellationToken)
    {
        await ledger.AcquireAppendLockAsync(customerId, cancellationToken);
        var lastBalance = await ledger.GetLastRunningBalanceAsync(customerId, cancellationToken);
        var signed = entryType == LedgerEntryType.Debit ? Math.Abs(amount) : -Math.Abs(amount);
        var entry = new CustomerLedgerEntry(
            customerId,
            occurredAtUtc,
            postingDate,
            entryType,
            amount,
            currency,
            1m,
            sourceType,
            sourceDocumentId,
            sourceDocumentNumber,
            description)
        {
            TenantId = tenantId,
        };
        entry.SetRunningBalance(lastBalance + signed);
        await ledger.AddAsync(entry, cancellationToken);
    }
}

public class InvoiceIssuedLedgerHandler : INotificationHandler<InvoiceIssuedEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    private readonly ICustomerTransactionRepository _customerTransactionRepository;

    public InvoiceIssuedLedgerHandler(
        ICustomerLedgerRepository ledger,
        ICustomerTransactionRepository customerTransactionRepository)
    {
        _ledger = ledger;
        _customerTransactionRepository = customerTransactionRepository;
    }

    public async Task Handle(InvoiceIssuedEvent notification, CancellationToken cancellationToken)
    {
        var entryType = notification.Type == InvoiceType.CreditNote ? LedgerEntryType.Credit : LedgerEntryType.Debit;
        var sourceType = notification.Type == InvoiceType.CreditNote ? LedgerSourceType.CreditNote : LedgerSourceType.Invoice;
        await LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            entryType, notification.Amount, notification.Currency,
            sourceType, notification.InvoiceId, notification.InvoiceNumber,
            notification.Type == InvoiceType.CreditNote ? "Credit note issued" : "Invoice issued",
            cancellationToken);

        var legacy = new CustomerTransaction(
            notification.CustomerId,
            CustomerTransactionType.InvoiceIssued,
            notification.Amount,
            notification.Currency)
        {
            TenantId = notification.TenantId,
            OccurredAtUtc = notification.OccurredAtUtc,
            InvoiceId = notification.InvoiceId,
            OrderId = notification.OrderId,
            Reference = notification.InvoiceNumber,
            Notes = "Invoice issued",
        };
        await _customerTransactionRepository.AddAsync(legacy, cancellationToken);
    }
}

public class InvoicePartiallyPaidLedgerHandler : INotificationHandler<InvoicePartiallyPaidEvent>
{
    private readonly ICustomerLedgerRepository _ledger;

    public InvoicePartiallyPaidLedgerHandler(ICustomerLedgerRepository ledger) => _ledger = ledger;

    public Task Handle(InvoicePartiallyPaidEvent notification, CancellationToken cancellationToken) =>
        LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            LedgerEntryType.Credit, notification.AmountApplied, notification.Currency,
            LedgerSourceType.Payment, notification.InvoiceId, notification.InvoiceNumber,
            $"Partial payment applied ({notification.Remaining} remaining)",
            cancellationToken);
}

public class InvoicePaidLedgerHandler : INotificationHandler<InvoicePaidEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    private readonly ICustomerTransactionRepository _customerTransactionRepository;

    public InvoicePaidLedgerHandler(
        ICustomerLedgerRepository ledger,
        ICustomerTransactionRepository customerTransactionRepository)
    {
        _ledger = ledger;
        _customerTransactionRepository = customerTransactionRepository;
    }

    public async Task Handle(InvoicePaidEvent notification, CancellationToken cancellationToken)
    {
        var legacy = new CustomerTransaction(
            notification.CustomerId,
            CustomerTransactionType.Payment,
            -notification.Amount,
            notification.Currency)
        {
            TenantId = notification.TenantId,
            OccurredAtUtc = notification.OccurredAtUtc,
            InvoiceId = notification.InvoiceId,
            Reference = notification.InvoiceNumber,
            Notes = "Invoice paid",
        };
        await _customerTransactionRepository.AddAsync(legacy, cancellationToken);
    }
}

public class InvoiceVoidedLedgerHandler : INotificationHandler<InvoiceVoidedEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    public InvoiceVoidedLedgerHandler(ICustomerLedgerRepository ledger) => _ledger = ledger;

    public Task Handle(InvoiceVoidedEvent notification, CancellationToken cancellationToken) =>
        LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            LedgerEntryType.Credit, notification.Amount, notification.Currency,
            LedgerSourceType.InvoiceVoid, notification.InvoiceId, notification.InvoiceNumber,
            $"Invoice voided{(string.IsNullOrEmpty(notification.Reason) ? string.Empty : $": {notification.Reason}")}",
            cancellationToken);
}

public class InvoiceWrittenOffLedgerHandler : INotificationHandler<InvoiceWrittenOffEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    public InvoiceWrittenOffLedgerHandler(ICustomerLedgerRepository ledger) => _ledger = ledger;

    public Task Handle(InvoiceWrittenOffEvent notification, CancellationToken cancellationToken) =>
        LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            LedgerEntryType.Credit, notification.Amount, notification.Currency,
            LedgerSourceType.WriteOff, notification.InvoiceId, notification.InvoiceNumber,
            $"Invoice written off{(string.IsNullOrEmpty(notification.Reason) ? string.Empty : $": {notification.Reason}")}",
            cancellationToken);
}

public class InvoiceCancelledLedgerHandler : INotificationHandler<InvoiceCancelledEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    private readonly ICustomerTransactionRepository _customerTransactionRepository;

    public InvoiceCancelledLedgerHandler(
        ICustomerLedgerRepository ledger,
        ICustomerTransactionRepository customerTransactionRepository)
    {
        _ledger = ledger;
        _customerTransactionRepository = customerTransactionRepository;
    }

    public async Task Handle(InvoiceCancelledEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.WasIssued) return;

        await LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            LedgerEntryType.Credit, notification.Amount, notification.Currency,
            LedgerSourceType.InvoiceVoid, notification.InvoiceId, notification.InvoiceNumber,
            "Invoice cancelled (reversal)",
            cancellationToken);

        var legacy = new CustomerTransaction(
            notification.CustomerId,
            CustomerTransactionType.Adjustment,
            -notification.Amount,
            notification.Currency)
        {
            TenantId = notification.TenantId,
            OccurredAtUtc = notification.OccurredAtUtc,
            InvoiceId = notification.InvoiceId,
            Reference = notification.InvoiceNumber,
            Notes = "Invoice cancelled (reversal)",
        };
        await _customerTransactionRepository.AddAsync(legacy, cancellationToken);
    }
}

public class PaymentConfirmedLedgerHandler : INotificationHandler<PaymentConfirmedEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    private readonly ICustomerTransactionRepository _legacy;

    public PaymentConfirmedLedgerHandler(ICustomerLedgerRepository ledger, ICustomerTransactionRepository legacy)
    {
        _ledger = ledger;
        _legacy = legacy;
    }

    public async Task Handle(PaymentConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var isReceipt = notification.Direction == PaymentDirection.CustomerReceipt;
        await LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            isReceipt ? LedgerEntryType.Credit : LedgerEntryType.Debit,
            notification.Amount, notification.Currency,
            LedgerSourceType.Payment, notification.PaymentId, notification.PaymentNumber,
            isReceipt ? "Payment received" : "Refund issued",
            cancellationToken);

        var legacy = new CustomerTransaction(
            notification.CustomerId,
            isReceipt ? CustomerTransactionType.Payment : CustomerTransactionType.Refund,
            isReceipt ? -notification.Amount : notification.Amount,
            notification.Currency)
        {
            TenantId = notification.TenantId,
            OccurredAtUtc = notification.OccurredAtUtc,
            Reference = notification.PaymentNumber,
            Notes = isReceipt ? "Payment received" : "Refund issued",
        };
        await _legacy.AddAsync(legacy, cancellationToken);
    }
}

public class PaymentVoidedLedgerHandler : INotificationHandler<PaymentVoidedEvent>
{
    private readonly ICustomerLedgerRepository _ledger;
    public PaymentVoidedLedgerHandler(ICustomerLedgerRepository ledger) => _ledger = ledger;

    public Task Handle(PaymentVoidedEvent notification, CancellationToken cancellationToken) =>
        LedgerPostingHelpers.PostAsync(
            _ledger, notification.TenantId, notification.CustomerId,
            notification.OccurredAtUtc, notification.OccurredAtUtc.Date,
            LedgerEntryType.Debit, notification.Amount, notification.Currency,
            LedgerSourceType.PaymentReversal, notification.PaymentId, notification.PaymentNumber,
            "Payment voided (reversal)",
            cancellationToken);
}
