using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using MediatR;

namespace CoreAlign.Application.Invoices.Commands;

public record IssueCreditNoteLineInput(
    Guid InvoiceLineId,
    decimal Quantity);

public record IssueCreditNoteCommand(
    Guid InvoiceId,
    IReadOnlyList<IssueCreditNoteLineInput> Lines,
    string? Reason = null,
    Guid? ReturnRequestId = null,
    Guid? OperationId = null)
    : IRequest<InvoiceDto>, ITransactionalRequest;
