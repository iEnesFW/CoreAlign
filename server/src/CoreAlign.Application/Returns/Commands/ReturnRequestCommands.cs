using CoreAlign.Application.Common;
using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Returns.Commands;

public record CreateReturnRequestLineInput(
    Guid OrderLineId,
    decimal QuantityReturned,
    bool Restockable = true,
    string? LineNotes = null);

public record CreateReturnRequestCommand(
    Guid OrderId,
    ReturnReasonCode Reason,
    string? ReasonText,
    IReadOnlyList<CreateReturnRequestLineInput> Lines,
    Guid? SourceInvoiceId = null,
    string? CustomerNotes = null,
    string? InternalNotes = null) : IRequest<ReturnRequestDto>, ITransactionalRequest;

public record ApproveReturnRequestCommand(Guid Id) : IRequest<ReturnRequestDto>, ITransactionalRequest;

public record RejectReturnRequestCommand(Guid Id, string? Reason) : IRequest<ReturnRequestDto>, ITransactionalRequest;

public record CancelReturnRequestCommand(Guid Id) : IRequest<ReturnRequestDto>, ITransactionalRequest;

public record ReceiveReturnedItemsCommand(
    Guid Id,
    Guid WarehouseId,
    bool AutoIssueCreditNote = true) : IRequest<ReturnRequestDto>, ITransactionalRequest;
