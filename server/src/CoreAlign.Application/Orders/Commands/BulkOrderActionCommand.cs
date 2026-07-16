using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Orders.Commands;

public enum BulkOrderActionType
{
    Submit,
    Approve,
    Allocate,
    Cancel,
}

public sealed record BulkOrderActionItemResult(Guid OrderId, bool Success, string? Error);

public sealed record BulkOrderActionResult(
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<BulkOrderActionItemResult> Items);

public sealed record BulkOrderActionCommand(
    List<Guid> OrderIds,
    BulkOrderActionType Action,
    string? Reason = null,
    Guid? ActorUserId = null) : IRequest<BulkOrderActionResult>;

public sealed class BulkOrderActionCommandValidator : AbstractValidator<BulkOrderActionCommand>
{
    public BulkOrderActionCommandValidator()
    {
        RuleFor(x => x.OrderIds).NotEmpty();
        RuleFor(x => x.OrderIds.Count)
            .LessThanOrEqualTo(200)
            .WithMessage("A bulk action can target at most 200 orders at once.");
    }
}

public sealed class BulkOrderActionCommandHandler
    : IRequestHandler<BulkOrderActionCommand, BulkOrderActionResult>
{
    private readonly IMediator _mediator;

    public BulkOrderActionCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<BulkOrderActionResult> Handle(BulkOrderActionCommand request, CancellationToken cancellationToken)
    {
        var items = new List<BulkOrderActionItemResult>();

        // Each order is processed through its own single-order command, so every item
        // runs in its own transaction (ITransactionalRequest) — a failure on one order
        // (e.g. invalid status transition or cross-tenant id → NotFound) is captured as a
        // failed item and the rest continue. Only business (DomainException) failures are
        // caught per-item; unexpected exceptions bubble to the middleware.
        foreach (var orderId in request.OrderIds.Distinct())
        {
            try
            {
                IRequest<OrderDto> command = request.Action switch
                {
                    BulkOrderActionType.Submit => new SubmitOrderCommand(orderId),
                    BulkOrderActionType.Approve => new ApproveOrderCommand(orderId, request.ActorUserId),
                    BulkOrderActionType.Allocate => new AllocateOrderCommand(orderId),
                    BulkOrderActionType.Cancel => new CancelOrderCommand(orderId, request.Reason),
                    _ => throw new InvalidOrderLineException("Unsupported bulk order action."),
                };
                await _mediator.Send(command, cancellationToken);
                items.Add(new BulkOrderActionItemResult(orderId, true, null));
            }
            catch (DomainException ex)
            {
                items.Add(new BulkOrderActionItemResult(orderId, false, ex.Message));
            }
            catch (ValidationException ex)
            {
                items.Add(new BulkOrderActionItemResult(orderId, false, ex.Message));
            }
        }

        return new BulkOrderActionResult(
            items.Count(i => i.Success),
            items.Count(i => !i.Success),
            items);
    }
}
