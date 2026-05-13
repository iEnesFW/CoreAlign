using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class MarkInvoiceAsPaidCommandHandler : IRequestHandler<MarkInvoiceAsPaidCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkInvoiceAsPaidCommandHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(MarkInvoiceAsPaidCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvoiceNotFoundException();

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
        {
            throw new InvoiceStatusTransitionException(invoice.Status.ToString(), "mark as paid");
        }

        invoice.MarkAsPaid(DateTime.UtcNow);
        _invoiceRepository.Update(invoice);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvoiceMapper.ToDto(invoice);
    }
}
