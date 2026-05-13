using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand, bool>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelInvoiceCommandHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvoiceNotFoundException();

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
        {
            throw new InvoiceStatusTransitionException(invoice.Status.ToString(), "cancel");
        }

        invoice.Cancel(DateTime.UtcNow);
        _invoiceRepository.Update(invoice);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
