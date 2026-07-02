using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Payments.DTOs;
using CoreAlign.Application.Payments.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Payments.Handlers;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly ICustomerRepository _customers;
    private readonly IInvoiceRepository _invoices;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;

    public CreatePaymentHandler(
        IPaymentRepository payments,
        ICustomerRepository customers,
        IInvoiceRepository invoices,
        IDocumentSequenceRepository sequences,
        IUnitOfWork uow)
    {
        _payments = payments;
        _customers = customers;
        _invoices = invoices;
        _sequences = sequences;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(CreatePaymentCommand c, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(c.CustomerId, ct)
            ?? throw new CustomerNotFoundException();

        // An advance is a prepayment captured before any invoice exists, so it cannot
        // carry invoice applications at creation — it is offset later via OffsetCustomerAdvance.
        if (c.IsAdvance && c.Applications is { Count: > 0 })
        {
            throw new PaymentApplicationException("An advance payment cannot be applied to invoices at creation; use offset-advance later.");
        }

        var number = await _sequences.ConsumeAsync(DocumentSequenceType.PaymentNumber, DateTime.UtcNow, ct);
        var payment = new Payment(
            paymentNumber: number,
            customerId: customer.Id,
            customerNameSnapshot: customer.LegalName ?? customer.Name,
            direction: c.Direction,
            paymentDate: c.PaymentDate,
            method: c.Method,
            amount: c.Amount,
            currency: c.Currency,
            isAdvance: c.IsAdvance);
        payment.UpdateDetails(
            paymentDate: c.PaymentDate,
            postingDate: c.PaymentDate.Date,
            method: c.Method,
            amount: c.Amount,
            exchangeRate: c.ExchangeRate,
            bankAccountInfo: c.BankAccountInfo,
            referenceNumber: c.ReferenceNumber,
            checkNumber: c.CheckNumber,
            checkDueDate: c.CheckDueDate,
            notes: c.Notes);

        await _payments.AddAsync(payment, ct);

        if (c.AutoConfirm)
        {
            payment.Confirm(null);
        }

        if (c.AutoConfirm && c.Applications is { Count: > 0 })
        {
            // Batch-load every targeted invoice in one round-trip — replaces the
            // previous N×GetByIdAsync that scaled badly when applying many invoices.
            var invoiceMap = await _invoices.GetByIdsAsync(
                c.Applications.Select(a => a.InvoiceId),
                ct);
            foreach (var apply in c.Applications)
            {
                if (!invoiceMap.TryGetValue(apply.InvoiceId, out var invoice))
                {
                    throw new InvoiceNotFoundException();
                }
                if (invoice.CustomerId != customer.Id)
                {
                    throw new PaymentApplicationException("Invoice does not belong to this customer.");
                }
                payment.Apply(apply.InvoiceId, apply.AppliedAmount, invoice.AmountDue);
                invoice.RecordPayment(apply.AppliedAmount, DateTime.UtcNow);
                // No need to Update — entity is tracked by GetByIdsAsync.
            }
        }

        await _uow.SaveChangesAsync(ct);
        payment.Customer = customer;
        return PaymentMapper.ToDto(payment);
    }
}

public class UpdatePaymentHandler : IRequestHandler<UpdatePaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _uow;

    public UpdatePaymentHandler(IPaymentRepository payments, IUnitOfWork uow)
    {
        _payments = payments;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(UpdatePaymentCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.Id, ct) ?? throw new PaymentNotFoundException();
        payment.UpdateDetails(c.PaymentDate, c.PostingDate, c.Method, c.Amount, c.ExchangeRate, c.BankAccountInfo, c.ReferenceNumber, c.CheckNumber, c.CheckDueDate, c.Notes);
        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}

public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _uow;

    public ConfirmPaymentHandler(IPaymentRepository payments, IUnitOfWork uow)
    {
        _payments = payments;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(ConfirmPaymentCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.Id, ct) ?? throw new PaymentNotFoundException();
        payment.Confirm(c.PostedByUserId);
        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}

public class ApplyPaymentHandler : IRequestHandler<ApplyPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;

    public ApplyPaymentHandler(IPaymentRepository payments, IInvoiceRepository invoices, IUnitOfWork uow)
    {
        _payments = payments;
        _invoices = invoices;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(ApplyPaymentCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.Id, ct) ?? throw new PaymentNotFoundException();

        if (!payment.IsConfirmed)
        {
            payment.Confirm(null);
        }

        // Batch-load all target invoices once instead of N round-trips.
        var invoiceMap = await _invoices.GetByIdsAsync(
            c.Applications.Select(a => a.InvoiceId),
            ct);

        foreach (var apply in c.Applications)
        {
            if (!invoiceMap.TryGetValue(apply.InvoiceId, out var invoice))
            {
                throw new InvoiceNotFoundException();
            }
            if (invoice.CustomerId != payment.CustomerId)
            {
                throw new PaymentApplicationException("Invoice does not belong to this payment's customer.");
            }
            if (payment.Applications.Any(a => a.InvoiceId == apply.InvoiceId))
            {
                continue;
            }
            payment.Apply(apply.InvoiceId, apply.AppliedAmount, invoice.AmountDue);
            invoice.RecordPayment(apply.AppliedAmount, DateTime.UtcNow);
            // Tracked by GetByIdsAsync — no explicit Update needed.
        }

        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}

public class OffsetCustomerAdvanceHandler : IRequestHandler<OffsetCustomerAdvanceCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IGLPostingService _gl;
    private readonly IUnitOfWork _uow;

    public OffsetCustomerAdvanceHandler(
        IPaymentRepository payments,
        IInvoiceRepository invoices,
        IGLPostingService gl,
        IUnitOfWork uow)
    {
        _payments = payments;
        _invoices = invoices;
        _gl = gl;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(OffsetCustomerAdvanceCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.PaymentId, ct) ?? throw new PaymentNotFoundException();
        if (!payment.IsAdvance)
        {
            throw new PaymentApplicationException("Only advance payments can be offset to invoices.");
        }
        if (!payment.IsConfirmed)
        {
            payment.Confirm(null);
        }

        var invoiceMap = await _invoices.GetByIdsAsync(c.Applications.Select(a => a.InvoiceId), ct);
        foreach (var apply in c.Applications)
        {
            if (!invoiceMap.TryGetValue(apply.InvoiceId, out var invoice))
            {
                throw new InvoiceNotFoundException();
            }
            if (invoice.CustomerId != payment.CustomerId)
            {
                throw new PaymentApplicationException("Invoice does not belong to this advance's customer.");
            }
            if (payment.Applications.Any(a => a.InvoiceId == apply.InvoiceId))
            {
                continue;
            }

            // The over-offset cap is the existing Apply guard: amount <= UnappliedAmount.
            var application = payment.Apply(apply.InvoiceId, apply.AppliedAmount, invoice.AmountDue);
            invoice.RecordPayment(apply.AppliedAmount, DateTime.UtcNow);

            // Offset (mahsup): consume the prepayment sitting in 340 against AR(120).
            // Posted inline + at the advance's own ExchangeRate; dedups on the application id.
            await _gl.PostAsync(new GLPostingRequest(
                JournalSourceType.CustomerAdvanceApplied,
                application.Id,
                payment.PaymentNumber,
                DateTime.UtcNow.Date,
                JournalEntryType.Mahsup,
                $"Avans mahsubu {payment.PaymentNumber}",
                new[]
                {
                    new GLPostingLine(GLPostingKey.CustomerAdvanceReceived, apply.AppliedAmount, 0m),
                    new GLPostingLine(GLPostingKey.AccountsReceivable, 0m, apply.AppliedAmount),
                },
                payment.Currency, payment.ExchangeRate), ct);
        }

        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}

public class ApplyPaymentFifoHandler : IRequestHandler<ApplyPaymentFifoCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;

    public ApplyPaymentFifoHandler(IPaymentRepository payments, IInvoiceRepository invoices, IUnitOfWork uow)
    {
        _payments = payments;
        _invoices = invoices;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(ApplyPaymentFifoCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.Id, ct) ?? throw new PaymentNotFoundException();

        if (!payment.IsConfirmed)
        {
            payment.Confirm(null);
        }

        var remaining = payment.UnappliedAmount;
        if (remaining > 0m)
        {
            var alreadyApplied = payment.Applications.Select(a => a.InvoiceId).ToHashSet();
            // Oldest-due-first open invoices for this customer (FIFO receivable settlement).
            var openInvoices = await _invoices.GetOpenForCustomerAsync(payment.CustomerId, ct);
            var targetIds = openInvoices
                .Where(i => !alreadyApplied.Contains(i.Id))
                .Select(i => i.Id)
                .ToList();

            if (targetIds.Count > 0)
            {
                // Re-load tracked so RecordPayment mutations persist (GetOpenForCustomerAsync is no-tracking).
                var invoiceMap = await _invoices.GetByIdsAsync(targetIds, ct);
                foreach (var id in targetIds)
                {
                    if (remaining <= 0m) break;
                    if (!invoiceMap.TryGetValue(id, out var invoice)) continue;
                    var due = invoice.AmountDue;
                    if (due <= 0m) continue;
                    var applyAmount = Math.Min(remaining, due);
                    payment.Apply(invoice.Id, applyAmount, due);
                    invoice.RecordPayment(applyAmount, DateTime.UtcNow);
                    remaining -= applyAmount;
                }
            }
        }

        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}

public class UnapplyPaymentHandler : IRequestHandler<UnapplyPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;

    public UnapplyPaymentHandler(IPaymentRepository payments, IInvoiceRepository invoices, IUnitOfWork uow)
    {
        _payments = payments;
        _invoices = invoices;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(UnapplyPaymentCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.Id, ct) ?? throw new PaymentNotFoundException();
        var app = payment.Applications.FirstOrDefault(a => a.Id == c.ApplicationId)
            ?? throw new PaymentApplicationException("Application not found on payment.");

        var invoice = await _invoices.GetByIdAsync(app.InvoiceId, ct);
        if (invoice is not null)
        {
            invoice.ReversePayment(app.AppliedAmount, DateTime.UtcNow);
            _invoices.Update(invoice);
        }
        payment.Unapply(c.ApplicationId);

        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}

public class VoidPaymentHandler : IRequestHandler<VoidPaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;

    public VoidPaymentHandler(IPaymentRepository payments, IInvoiceRepository invoices, IUnitOfWork uow)
    {
        _payments = payments;
        _invoices = invoices;
        _uow = uow;
    }

    public async Task<PaymentDto> Handle(VoidPaymentCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetWithApplicationsAsync(c.Id, ct) ?? throw new PaymentNotFoundException();

        var apps = payment.Applications.ToList();
        if (apps.Count > 0)
        {
            // One round-trip for every applied invoice — replaces N×GetByIdAsync.
            var invoiceMap = await _invoices.GetByIdsAsync(apps.Select(a => a.InvoiceId), ct);
            foreach (var app in apps)
            {
                if (invoiceMap.TryGetValue(app.InvoiceId, out var invoice))
                {
                    invoice.ReversePayment(app.AppliedAmount, DateTime.UtcNow);
                    // Tracked — no explicit Update needed.
                }
            }
        }

        payment.Void(c.Reason);
        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return PaymentMapper.ToDto(payment);
    }
}
