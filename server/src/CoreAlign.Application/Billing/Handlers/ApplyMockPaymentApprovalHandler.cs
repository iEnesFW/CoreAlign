using CoreAlign.Application.Billing.DTOs;
using CoreAlign.Application.Billing.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Billing.Handlers;

/// <summary>
/// Dev-only command that short-circuits the mock gateway's webhook step from the
/// SPA's approve/cancel button. Strictly refuses to operate on any order that is
/// not on the "mock" gateway — real gateway flows must always go through
/// <see cref="ProcessPaymentWebhookHandler"/>.
/// </summary>
public class ApplyMockPaymentApprovalHandler : IRequestHandler<ApplyMockPaymentApprovalCommand, SubscriptionOrderDto>
{
    private const string MockGatewayName = "mock";

    private readonly ISubscriptionOrderRepository _orders;
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISubscriptionActivatedOutbox _activatedOutbox;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public ApplyMockPaymentApprovalHandler(
        ISubscriptionOrderRepository orders,
        IPaymentAttemptRepository attempts,
        ISubscriptionActivatedOutbox activatedOutbox,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _orders = orders;
        _attempts = attempts;
        _activatedOutbox = activatedOutbox;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<SubscriptionOrderDto> Handle(ApplyMockPaymentApprovalCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken)
            ?? throw new SubscriptionOrderNotFoundException();
        _tenant.EnsureSameTenant(order.TenantId);

        if (!string.Equals(order.GatewayName, MockGatewayName, StringComparison.OrdinalIgnoreCase))
        {
            throw new SubscriptionOrderInvalidStateException("Order was not created with the mock gateway.");
        }

        var normalized = request.Action.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "approve":
                if (order.Status == SubscriptionOrderStatus.Paid)
                {
                    return BillingMapper.ToDto(order);
                }
                if (order.Status != SubscriptionOrderStatus.PendingPayment)
                {
                    throw new SubscriptionOrderInvalidStateException($"Order in status {order.Status} cannot be approved.");
                }
                order.MarkPaid($"mock_ref_{Guid.NewGuid():N}");
                _orders.Update(order);

                await _attempts.AddAsync(new PaymentAttempt(
                    order.Id,
                    MockGatewayName,
                    order.GatewayIntentId,
                    PaymentAttemptStatus.Succeeded,
                    order.TotalAmount,
                    order.Currency,
                    rawResponseJson: null), cancellationToken);

                await _activatedOutbox.EnqueueAsync(new SubscriptionActivatedPayload(order.Id, tenantId), cancellationToken);
                break;

            case "cancel":
                if (order.Status == SubscriptionOrderStatus.Cancelled)
                {
                    return BillingMapper.ToDto(order);
                }
                if (order.Status is SubscriptionOrderStatus.Paid)
                {
                    throw new SubscriptionOrderInvalidStateException("Cannot cancel a paid order.");
                }
                order.MarkCancelled("Mock gateway: user cancelled.");
                _orders.Update(order);
                await _attempts.AddAsync(new PaymentAttempt(
                    order.Id,
                    MockGatewayName,
                    order.GatewayIntentId,
                    PaymentAttemptStatus.Cancelled,
                    order.TotalAmount,
                    order.Currency,
                    rawResponseJson: null), cancellationToken);
                break;

            case "fail":
                if (order.Status == SubscriptionOrderStatus.Failed)
                {
                    return BillingMapper.ToDto(order);
                }
                if (order.Status is SubscriptionOrderStatus.Paid)
                {
                    throw new SubscriptionOrderInvalidStateException("Cannot fail a paid order.");
                }
                order.MarkFailed("Mock gateway: simulated failure.");
                _orders.Update(order);
                await _attempts.AddAsync(new PaymentAttempt(
                    order.Id,
                    MockGatewayName,
                    order.GatewayIntentId,
                    PaymentAttemptStatus.Failed,
                    order.TotalAmount,
                    order.Currency,
                    rawResponseJson: null,
                    failureReason: "Mock gateway: simulated failure."), cancellationToken);
                break;

            default:
                throw new ArgumentException($"Unknown mock action '{request.Action}'.", nameof(request.Action));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return BillingMapper.ToDto(order);
    }
}
