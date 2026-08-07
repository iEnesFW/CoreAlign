using CoreAlign.Application.Billing.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Billing;

public record OrderItemInput(Guid ModuleId, Guid PlanId);

/// <summary>
/// Buyer / billing snapshot collected at checkout. REQUIRED when the chosen
/// gateway is not the dev mock; validated by
/// <see cref="Validators.CreateSubscriptionOrderCommandValidator"/>.
/// </summary>
public record SubscriptionBillingInfoInput(
    string? Name,
    string? Surname,
    string? Email,
    string? GsmNumber,
    string? IdentityNumber,
    string? Address,
    string? City,
    string? Country,
    string? ZipCode);

public record CreateSubscriptionOrderCommand(
    IReadOnlyList<OrderItemInput> Items,
    string? GatewayName = null,
    SubscriptionBillingInfoInput? BillingInfo = null,
    Guid CurrentUserId = default,
    string? BuyerIpAddress = null,
    Guid? OperationId = null) : IRequest<SubscriptionOrderCreationResult>, ITransactionalRequest;

public record ApplyMockPaymentApprovalCommand(
    Guid OrderId,
    string Action,
    Guid CurrentUserId = default) : IRequest<SubscriptionOrderDto>, ITransactionalRequest;

public record CancelSubscriptionOrderCommand(
    Guid OrderId,
    string? Reason,
    Guid CurrentUserId = default,
    bool IsAdmin = false) : IRequest<SubscriptionOrderDto>, ITransactionalRequest;

public record ProcessPaymentWebhookCommand(
    string GatewayName,
    string Payload,
    IReadOnlyDictionary<string, string> Headers) : IRequest<PaymentWebhookResult>, ITransactionalRequest;

public record PaymentWebhookResult(bool Accepted, string? OrderId, string? Status, string? Message);

public record ListPaymentGatewaysQuery() : IRequest<IReadOnlyList<DTOs.PaymentGatewayDescriptor>>;

public record ListModulesCatalogQuery() : IRequest<IReadOnlyList<ModuleDto>>;

public record ListTenantModulesQuery() : IRequest<IReadOnlyList<TenantModuleDto>>;

public record ListSubscriptionOrdersQuery(
    SubscriptionOrderStatus? Status = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<SubscriptionOrderDto>>;

public record GetSubscriptionOrderByIdQuery(Guid Id) : IRequest<SubscriptionOrderDto>;
