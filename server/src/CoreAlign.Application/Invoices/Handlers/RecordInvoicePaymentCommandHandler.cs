using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Payments.Commands;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class RecordInvoicePaymentCommandHandler : IRequestHandler<RecordInvoicePaymentCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IMediator _mediator;

    public RecordInvoicePaymentCommandHandler(IInvoiceRepository invoiceRepository, IMediator mediator)
    {
        _invoiceRepository = invoiceRepository;
        _mediator = mediator;
    }

    public async Task<InvoiceDto> Handle(RecordInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvoiceNotFoundException();

        if (!invoice.IsIssued && invoice.Status != InvoiceStatus.Overdue)
        {
            throw new InvoiceStatusTransitionException(invoice.Status.ToString(), "record payment");
        }

        if (request.Amount <= 0m)
        {
            throw new InvalidInvoiceStateException("Payment amount must be greater than zero.");
        }

        var due = invoice.AmountDue;
        if (request.Amount > due)
        {
            throw new CannotOverPayInvoiceException(due, request.Amount);
        }

        await _mediator.Send(new CreatePaymentCommand(
            CustomerId: invoice.CustomerId,
            PaymentDate: request.PaymentDate ?? DateTime.UtcNow,
            Method: request.Method,
            Amount: request.Amount,
            Currency: invoice.Currency,
            Direction: PaymentDirection.CustomerReceipt,
            ExchangeRate: invoice.ExchangeRate,
            ReferenceNumber: request.ReferenceNumber,
            Notes: request.Notes,
            AutoConfirm: true,
            Applications: new List<PaymentApplyLine> { new(invoice.Id, request.Amount) }),
            cancellationToken);

        var updated = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvoiceNotFoundException();
        return InvoiceMapper.ToDto(updated);
    }
}
