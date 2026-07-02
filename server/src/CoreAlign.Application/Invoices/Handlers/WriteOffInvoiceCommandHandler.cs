using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class WriteOffInvoiceCommandValidator : AbstractValidator<WriteOffInvoiceCommand>
{
    public WriteOffInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class WriteOffInvoiceCommandHandler : IRequestHandler<WriteOffInvoiceCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _unitOfWork;

    public WriteOffInvoiceCommandHandler(IInvoiceRepository invoices, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDto> Handle(WriteOffInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Tenant-scoped load → cross-tenant id resolves to null → 404 (no IDOR leak).
        var invoice = await _invoices.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvoiceNotFoundException();

        invoice.WriteOff(DateTime.UtcNow, request.Reason);

        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return InvoiceMapper.ToDto(invoice);
    }
}
