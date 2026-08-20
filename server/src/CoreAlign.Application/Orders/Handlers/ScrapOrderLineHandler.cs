using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public sealed class ScrapOrderLineCommandValidator : AbstractValidator<ScrapOrderLineCommand>
{
    public ScrapOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderLineId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class ScrapOrderLineHandler : IRequestHandler<ScrapOrderLineCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IAllocationService _allocator;
    private readonly IUnitOfWork _uow;

    public ScrapOrderLineHandler(IOrderRepository orders, IAllocationService allocator, IUnitOfWork uow)
    {
        _orders = orders;
        _allocator = allocator;
        _uow = uow;
    }

    public async Task<OrderDto> Handle(ScrapOrderLineCommand c, CancellationToken ct)
    {
        var order = await _orders.GetWithLinesAsync(c.OrderId, ct) ?? throw new OrderNotFoundException();
        order.RecordLineScrap(c.OrderLineId, c.Quantity, c.Notes);

        // WHY the reservation is given back: a scrapped unit will never ship, so the stock it was
        // holding must return to available. Without this the reservation outlived every release
        // path — PartiallyShipped orders cannot be cancelled or reverted — and the quantity stayed
        // reserved forever, on hand but unsellable.
        await _allocator.ReleaseForOrderLineAsync(order.Id, c.OrderLineId, c.Quantity, ct);

        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return OrderMapper.ToDto(order);
    }
}
