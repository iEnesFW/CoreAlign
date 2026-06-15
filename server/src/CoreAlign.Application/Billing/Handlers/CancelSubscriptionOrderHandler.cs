using CoreAlign.Application.Billing.DTOs;
using CoreAlign.Application.Billing.Mapping;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Billing.Handlers;

public class CancelSubscriptionOrderHandler : IRequestHandler<CancelSubscriptionOrderCommand, SubscriptionOrderDto>
{
    private readonly ISubscriptionOrderRepository _orders;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public CancelSubscriptionOrderHandler(ISubscriptionOrderRepository orders, ITenantContext tenant, IUnitOfWork uow)
    {
        _orders = orders;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<SubscriptionOrderDto> Handle(CancelSubscriptionOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new SubscriptionOrderNotFoundException();
        _tenant.EnsureSameTenant(order.TenantId);

        if (order.CreatedByUserId != request.CurrentUserId && !request.IsAdmin)
        {
            throw new SubscriptionOrderForbiddenException();
        }

        if (order.Status is not (SubscriptionOrderStatus.Draft or SubscriptionOrderStatus.PendingPayment))
        {
            throw new SubscriptionOrderInvalidStateException($"Order in status {order.Status} cannot be cancelled.");
        }

        order.MarkCancelled(request.Reason);
        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return BillingMapper.ToDto(order);
    }
}
