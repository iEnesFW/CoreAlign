using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.EventHandlers;

/// <summary>
/// Builds the AR-side journal lines for a sales document so the entry foots to
/// the invoice total the customer is billed. Revenue is booked at the taxable
/// base; recharged freight credits shipping income (602) and any rounding the
/// invoice applies credits a rounding gain (679) or debits a rounding loss
/// (689); any tevkifat (withholding) the customer does not pay is debited to a
/// withholding-receivable control account. The result is
/// DR(AR + Withholding [+ RoundingLoss]) == CR(Revenue + VAT + Shipping
/// [+ RoundingGain]) with DR AccountsReceivable == invoice.Total exactly.
/// <paramref name="reverse"/> flips the entry for credit notes and for
/// voids/cancellations.
/// </summary>
internal static class SalesGLLines
{
    public static IReadOnlyList<GLPostingLine> Build(
        decimal revenue, decimal tax, decimal withholding, decimal shipping, decimal rounding, bool reverse)
    {
        revenue = Math.Max(0m, revenue);
        withholding = Math.Max(0m, withholding);
        shipping = Math.Max(0m, shipping);
        var receivable = Math.Max(0m, revenue + tax - withholding + shipping + rounding);
        var roundingGain = rounding > 0m ? rounding : 0m;
        var roundingLoss = rounding < 0m ? -rounding : 0m;
        return reverse
            ? new[]
            {
                new GLPostingLine(GLPostingKey.SalesRevenue, revenue, 0m),
                new GLPostingLine(GLPostingKey.OutputVat, tax, 0m),
                new GLPostingLine(GLPostingKey.ShippingIncome, shipping, 0m),
                new GLPostingLine(GLPostingKey.RoundingGain, roundingGain, 0m),
                new GLPostingLine(GLPostingKey.RoundingLoss, 0m, roundingLoss),
                new GLPostingLine(GLPostingKey.AccountsReceivable, 0m, receivable),
                new GLPostingLine(GLPostingKey.WithholdingReceivable, 0m, withholding),
            }
            : new[]
            {
                new GLPostingLine(GLPostingKey.AccountsReceivable, receivable, 0m),
                new GLPostingLine(GLPostingKey.WithholdingReceivable, withholding, 0m),
                new GLPostingLine(GLPostingKey.RoundingLoss, roundingLoss, 0m),
                new GLPostingLine(GLPostingKey.SalesRevenue, 0m, revenue),
                new GLPostingLine(GLPostingKey.OutputVat, 0m, tax),
                new GLPostingLine(GLPostingKey.ShippingIncome, 0m, shipping),
                new GLPostingLine(GLPostingKey.RoundingGain, 0m, roundingGain),
            };
    }
}

public class InvoiceIssuedGLHandler : INotificationHandler<InvoiceIssuedEvent>
{
    private readonly IGLPostingOutbox _outbox;

    public InvoiceIssuedGLHandler(IGLPostingOutbox outbox) => _outbox = outbox;

    /// <remarks>
    /// Reads NOTHING from the database. This event fires inside the SaveChanges that inserts the
    /// invoice, so an invoice created and issued in one command is not queryable yet: the old
    /// re-read returned null and the handler returned without booking anything. Measured on the dev
    /// database — 18 issued invoices carried customer-ledger rows and zero journal entries.
    /// </remarks>
    public Task Handle(InvoiceIssuedEvent n, CancellationToken cancellationToken)
    {
        var reverse = n.Type == InvoiceType.CreditNote;
        return _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.SalesInvoice,
            n.InvoiceId,
            n.InvoiceNumber,
            n.OccurredAtUtc.Date,
            JournalEntryType.Mahsup,
            reverse ? $"İade faturası {n.InvoiceNumber}" : $"Satış faturası {n.InvoiceNumber}",
            SalesGLLines.Build(n.TaxableTotal, n.TaxTotal, n.WithholdingTotal, n.ShippingCost, n.RoundingAdjustment, reverse),
            n.Currency, n.ExchangeRate), cancellationToken);
    }
}

public class InvoiceVoidedGLHandler : INotificationHandler<InvoiceVoidedEvent>
{
    private readonly IGLPostingOutbox _outbox;
    private readonly IInvoiceRepository _invoices;

    public InvoiceVoidedGLHandler(IGLPostingOutbox outbox, IInvoiceRepository invoices)
    {
        _outbox = outbox;
        _invoices = invoices;
    }

    public async Task Handle(InvoiceVoidedEvent n, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(n.InvoiceId, cancellationToken);
        if (invoice is null) return;

        // Voiding a sales invoice reverses the original issuance; voiding a credit
        // note un-reverses it.
        var reverse = invoice.Type != InvoiceType.CreditNote;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.SalesInvoiceReversal,
            n.InvoiceId,
            n.InvoiceNumber,
            n.OccurredAtUtc.Date,
            JournalEntryType.Mahsup,
            $"Fatura iptali {n.InvoiceNumber}",
            SalesGLLines.Build(invoice.TaxableTotal, invoice.TaxTotal, invoice.WithholdingTotal, invoice.ShippingCost, invoice.RoundingAdjustment, reverse),
            invoice.Currency, invoice.ExchangeRate), cancellationToken);
    }
}

public class InvoiceCancelledGLHandler : INotificationHandler<InvoiceCancelledEvent>
{
    private readonly IGLPostingOutbox _outbox;
    private readonly IInvoiceRepository _invoices;

    public InvoiceCancelledGLHandler(IGLPostingOutbox outbox, IInvoiceRepository invoices)
    {
        _outbox = outbox;
        _invoices = invoices;
    }

    public async Task Handle(InvoiceCancelledEvent n, CancellationToken cancellationToken)
    {
        if (!n.WasIssued) return; // never posted to AR → nothing to reverse

        var invoice = await _invoices.GetByIdAsync(n.InvoiceId, cancellationToken);
        if (invoice is null) return;

        var reverse = invoice.Type != InvoiceType.CreditNote;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.SalesInvoiceReversal,
            n.InvoiceId,
            n.InvoiceNumber,
            n.OccurredAtUtc.Date,
            JournalEntryType.Mahsup,
            $"Fatura iptali {n.InvoiceNumber}",
            SalesGLLines.Build(invoice.TaxableTotal, invoice.TaxTotal, invoice.WithholdingTotal, invoice.ShippingCost, invoice.RoundingAdjustment, reverse),
            invoice.Currency, invoice.ExchangeRate), cancellationToken);
    }
}

public class InvoiceWrittenOffGLHandler : INotificationHandler<InvoiceWrittenOffEvent>
{
    private readonly IGLPostingOutbox _outbox;
    private readonly IInvoiceRepository _invoices;

    public InvoiceWrittenOffGLHandler(IGLPostingOutbox outbox, IInvoiceRepository invoices)
    {
        _outbox = outbox;
        _invoices = invoices;
    }

    public async Task Handle(InvoiceWrittenOffEvent n, CancellationToken cancellationToken)
    {
        // Bad-debt write-off recognizes a loss: DR doubtful-debt expense (654) /
        // CR AR (120) for the amount still outstanding. Revenue stays recognized
        // (it was earned) — only the uncollectible receivable is expensed.
        var invoice = await _invoices.GetByIdAsync(n.InvoiceId, cancellationToken);
        var exchangeRate = invoice?.ExchangeRate ?? 1m;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.InvoiceWriteOff,
            n.InvoiceId,
            n.InvoiceNumber,
            n.OccurredAtUtc.Date,
            JournalEntryType.Mahsup,
            $"Değersiz alacak kaydı {n.InvoiceNumber}",
            new[]
            {
                new GLPostingLine(GLPostingKey.DoubtfulDebtExpense, n.Amount, 0m),
                new GLPostingLine(GLPostingKey.AccountsReceivable, 0m, n.Amount),
            },
            invoice?.Currency ?? n.Currency, exchangeRate), cancellationToken);
    }
}

public class PaymentConfirmedGLHandler : INotificationHandler<PaymentConfirmedEvent>
{
    private readonly IGLPostingOutbox _outbox;
    private readonly IPaymentRepository _payments;

    public PaymentConfirmedGLHandler(IGLPostingOutbox outbox, IPaymentRepository payments)
    {
        _outbox = outbox;
        _payments = payments;
    }

    public async Task Handle(PaymentConfirmedEvent n, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetByIdAsync(n.PaymentId, cancellationToken);
        var cashKey = payment?.Method == PaymentMethod.Cash ? GLPostingKey.Cash : GLPostingKey.Bank;
        var isReceipt = n.Direction == PaymentDirection.CustomerReceipt;
        var isAdvance = payment?.IsAdvance == true;

        // Advance receipt: DR cash / CR 340 (Alınan Sipariş Avansları) — no invoice yet,
        // so it must NOT hit AR(120). Normal payment: DR cash / CR AR. Refund: the reverse.
        var controlKey = isAdvance ? GLPostingKey.CustomerAdvanceReceived : GLPostingKey.AccountsReceivable;
        var lines = PaymentGLLines.CashMovement(
            cashKey, controlKey, n.Amount, cashIsDebit: isReceipt);

        await _outbox.EnqueueAsync(new GLPostingRequest(
            isAdvance ? JournalSourceType.CustomerAdvanceReceived : JournalSourceType.CustomerPayment,
            n.PaymentId,
            n.PaymentNumber,
            n.OccurredAtUtc.Date,
            isReceipt ? JournalEntryType.Tahsil : JournalEntryType.Tediye,
            isAdvance
                ? $"Avans tahsilatı {n.PaymentNumber}"
                : (isReceipt ? $"Tahsilat {n.PaymentNumber}" : $"İade ödemesi {n.PaymentNumber}"),
            lines,
            n.Currency, n.ExchangeRate), cancellationToken);
    }
}

public class PaymentVoidedGLHandler : INotificationHandler<PaymentVoidedEvent>
{
    private readonly IGLPostingOutbox _outbox;
    private readonly IPaymentRepository _payments;

    public PaymentVoidedGLHandler(IGLPostingOutbox outbox, IPaymentRepository payments)
    {
        _outbox = outbox;
        _payments = payments;
    }

    public async Task Handle(PaymentVoidedEvent n, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetWithApplicationsAsync(n.PaymentId, cancellationToken);
        var cashKey = payment?.Method == PaymentMethod.Cash ? GLPostingKey.Cash : GLPostingKey.Bank;
        var currency = payment?.Currency ?? "TRY";
        var exchangeRate = payment?.ExchangeRate ?? 1m;
        var wasReceipt = payment is null || payment.Direction == PaymentDirection.CustomerReceipt;
        var isAdvance = payment?.IsAdvance == true;

        // WHY: an advance offset booked DR 340 / CR 120 (CustomerAdvanceApplied); voiding must reverse it (DR 120 / CR 340).
        if (isAdvance && payment is not null)
        {
            foreach (var app in payment.Applications)
            {
                await _outbox.EnqueueAsync(new GLPostingRequest(
                    JournalSourceType.CustomerAdvanceAppliedReversal, app.Id, n.PaymentNumber, n.OccurredAtUtc.Date,
                    JournalEntryType.Mahsup, $"Avans mahsup iptali {n.PaymentNumber}",
                    new[]
                    {
                        new GLPostingLine(GLPostingKey.AccountsReceivable, app.AppliedAmount, 0m),
                        new GLPostingLine(GLPostingKey.CustomerAdvanceReceived, 0m, app.AppliedAmount),
                    },
                    currency, exchangeRate), cancellationToken);
            }
        }

        // Reverse the original cash movement: a receipt becomes DR control / CR Cash (cash credited),
        // a refund the opposite. An advance was booked to 340 (CustomerAdvanceReceived), so reverse it
        // there — NOT to AR(120), which never held the prepayment.
        var controlKey = isAdvance ? GLPostingKey.CustomerAdvanceReceived : GLPostingKey.AccountsReceivable;
        var reversalSource = isAdvance
            ? JournalSourceType.CustomerAdvanceReceivedReversal
            : JournalSourceType.CustomerPaymentReversal;
        var lines = PaymentGLLines.CashMovement(
            cashKey, controlKey, n.Amount, cashIsDebit: !wasReceipt);

        await _outbox.EnqueueAsync(new GLPostingRequest(
            reversalSource,
            n.PaymentId,
            n.PaymentNumber,
            n.OccurredAtUtc.Date,
            JournalEntryType.Mahsup,
            isAdvance ? $"Avans iptali {n.PaymentNumber}" : $"Tahsilat iptali {n.PaymentNumber}",
            lines,
            currency, exchangeRate), cancellationToken);
    }
}
