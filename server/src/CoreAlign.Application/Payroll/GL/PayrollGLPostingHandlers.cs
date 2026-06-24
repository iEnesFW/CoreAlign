using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Payroll.GL;

public class PayrollAccrualGLHandler : INotificationHandler<PayrollRunPostedEvent>
{
    private readonly IGLPostingOutbox _outbox;
    private readonly IPayrollRunRepository _runs;

    public PayrollAccrualGLHandler(IGLPostingOutbox outbox, IPayrollRunRepository runs)
    {
        _outbox = outbox;
        _runs = runs;
    }

    public async Task Handle(PayrollRunPostedEvent n, CancellationToken cancellationToken)
    {
        var run = await _runs.GetByIdAsync(n.PayrollRunId, cancellationToken);
        if (run is null) return;

        var totals = PayrollRunTotals.From(run);
        var periodEnd = PayrollPeriod.End(n.PeriodYear, n.PeriodMonth);

        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.PayrollAccrual,
            n.PayrollRunId,
            n.RunNumber,
            periodEnd,
            JournalEntryType.Mahsup,
            $"Bordro tahakkuku {n.RunNumber}",
            PayrollGLLines.Accrual(totals, reverse: false),
            run.Currency), cancellationToken);
    }
}

public class PayrollNetPaymentGLHandler : INotificationHandler<PayrollRunPaidEvent>
{
    private readonly IGLPostingOutbox _outbox;

    public PayrollNetPaymentGLHandler(IGLPostingOutbox outbox) => _outbox = outbox;

    public async Task Handle(PayrollRunPaidEvent n, CancellationToken cancellationToken)
    {
        if (n.TotalNet <= 0m) return;

        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.PayrollNetPayment,
            n.PayrollRunId,
            n.RunNumber,
            n.OccurredAtUtc.Date,
            JournalEntryType.Tediye,
            $"Bordro net ödemesi {n.RunNumber}",
            PaymentGLLines.CashMovement(
                GLPostingKey.Bank, GLPostingKey.PersonnelNetPayable, n.TotalNet, cashIsDebit: false)),
            cancellationToken);
    }
}

public class PayPayrollTaxesHandler : IRequestHandler<PayPayrollTaxesCommand, Unit>
{
    private readonly IGLPostingOutbox _outbox;

    public PayPayrollTaxesHandler(IGLPostingOutbox outbox) => _outbox = outbox;

    public async Task<Unit> Handle(PayPayrollTaxesCommand c, CancellationToken ct)
    {
        var cashKey = c.FromCash ? GLPostingKey.Cash : GLPostingKey.Bank;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.PayrollTaxPayment,
            c.PaymentId,
            c.Reference,
            c.PaymentDate.Date,
            JournalEntryType.Tediye,
            $"Muhtasar ödemesi {c.Reference}",
            PaymentGLLines.CashMovement(
                cashKey, GLPostingKey.TaxesPayable, c.Amount, cashIsDebit: false)),
            ct);
        return Unit.Value;
    }
}

public class PayPayrollSgkHandler : IRequestHandler<PayPayrollSgkCommand, Unit>
{
    private readonly IGLPostingOutbox _outbox;

    public PayPayrollSgkHandler(IGLPostingOutbox outbox) => _outbox = outbox;

    public async Task<Unit> Handle(PayPayrollSgkCommand c, CancellationToken ct)
    {
        var cashKey = c.FromCash ? GLPostingKey.Cash : GLPostingKey.Bank;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.PayrollSgkPayment,
            c.PaymentId,
            c.Reference,
            c.PaymentDate.Date,
            JournalEntryType.Tediye,
            $"SGK ödemesi {c.Reference}",
            PaymentGLLines.CashMovement(
                cashKey, GLPostingKey.SgkPayable, c.Amount, cashIsDebit: false)),
            ct);
        return Unit.Value;
    }
}

internal static class PayrollPeriod
{
    public static DateTime End(int year, int month) =>
        new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);
}
