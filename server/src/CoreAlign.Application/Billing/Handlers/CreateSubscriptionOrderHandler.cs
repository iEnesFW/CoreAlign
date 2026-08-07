using CoreAlign.Application.Billing.DTOs;
using CoreAlign.Application.Billing.Mapping;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Billing.Handlers;

public class CreateSubscriptionOrderHandler : IRequestHandler<CreateSubscriptionOrderCommand, SubscriptionOrderCreationResult>
{
    private const int SequencePadLength = 5;

    private readonly IModuleRepository _modules;
    private readonly IModulePricePlanRepository _plans;
    private readonly ISubscriptionOrderRepository _orders;
    private readonly IPaymentAttemptRepository _attempts;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;
    private readonly IOptions<BillingOptions> _options;

    public CreateSubscriptionOrderHandler(
        IModuleRepository modules,
        IModulePricePlanRepository plans,
        ISubscriptionOrderRepository orders,
        IPaymentAttemptRepository attempts,
        IDocumentSequenceRepository sequences,
        IPaymentGatewayRegistry gateways,
        ITenantContext tenant,
        IUnitOfWork uow,
        IOptions<BillingOptions> options)
    {
        _modules = modules;
        _plans = plans;
        _orders = orders;
        _attempts = attempts;
        _sequences = sequences;
        _gateways = gateways;
        _tenant = tenant;
        _uow = uow;
        _options = options;
    }

    public async Task<SubscriptionOrderCreationResult> Handle(CreateSubscriptionOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(request));
        if (request.Items.Count == 0) throw new ArgumentException("At least one item is required.", nameof(request));

        var tenantId = _tenant.RequireTenantId();

        // Durable replay guard: a double submit (or a network retry) must not burn a second order
        // number, create a second gateway intent, or charge the buyer twice. The DB backs it with a
        // partial unique index on (tenant_id, operation_id).
        if (request.OperationId is { } operationId && operationId != Guid.Empty)
        {
            var replay = await _orders.GetByOperationIdAsync(operationId, cancellationToken);
            if (replay is not null)
            {
                return new SubscriptionOrderCreationResult(
                    BillingMapper.ToDto(replay),
                    replay.GatewayName ?? string.Empty,
                    replay.GatewayIntentId,
                    replay.GatewayRedirectUrl);
            }
        }

        var gatewayName = ResolveGatewayName(request.GatewayName);
        var gateway = _gateways.Find(gatewayName)
            ?? throw new PaymentGatewayNotConfiguredException(gatewayName);

        var moduleIds = request.Items.Select(i => i.ModuleId).Distinct().ToList();
        var planIds = request.Items.Select(i => i.PlanId).Distinct().ToList();

        var modules = (await _modules.ListByIdsAsync(moduleIds, cancellationToken)).ToDictionary(m => m.Id);
        var plans = (await _plans.ListByIdsAsync(planIds, cancellationToken)).ToDictionary(p => p.Id);

        foreach (var input in request.Items)
        {
            if (!modules.TryGetValue(input.ModuleId, out var module) || !module.IsActive)
            {
                throw new ModuleNotFoundException();
            }
            if (!plans.TryGetValue(input.PlanId, out var plan) || !plan.IsActive)
            {
                throw new ModulePricePlanNotFoundException();
            }
            if (plan.ModuleId != input.ModuleId)
            {
                throw new ModulePricePlanNotFoundException();
            }
        }

        var firstCurrency = plans[request.Items[0].PlanId].Currency;
        if (request.Items.Select(i => plans[i.PlanId].Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            throw new SubscriptionOrderInvalidStateException("All items in a single order must share the same currency.");
        }

        await _sequences.EnsureExistsAsync(DocumentSequenceType.SubscriptionOrderNumber, "SUB", SequencePadLength, DateTime.UtcNow.Year, cancellationToken);
        // WHY the save between: EnsureExists only ADDS a tracked row, ConsumeAsync queries the DB.
        // Without this the first purchase on a tenant whose sequence was never seeded throws
        // (the sequence is seeded only by DemoDataSeeder, which is off in production).
        await _uow.SaveChangesAsync(cancellationToken);
        var orderNumber = await _sequences.ConsumeAsync(DocumentSequenceType.SubscriptionOrderNumber, DateTime.UtcNow, cancellationToken);

        var order = new SubscriptionOrder(orderNumber, request.CurrentUserId, firstCurrency, null, request.OperationId);
        foreach (var input in request.Items)
        {
            var module = modules[input.ModuleId];
            var plan = plans[input.PlanId];
            order.AddItem(new SubscriptionOrderItem(
                module.Id, plan.Id,
                module.Code, module.Name,
                plan.DisplayLabel, plan.DurationDays,
                plan.Price, plan.Currency));
        }
        order.MoveToPendingPayment();

        if (request.BillingInfo is not null)
        {
            order.AttachBillingInfo(
                request.BillingInfo.Name,
                request.BillingInfo.Surname,
                request.BillingInfo.Email,
                request.BillingInfo.GsmNumber,
                request.BillingInfo.IdentityNumber,
                request.BuyerIpAddress,
                request.BillingInfo.Address,
                request.BillingInfo.City,
                request.BillingInfo.Country,
                request.BillingInfo.ZipCode);
        }

        await _orders.AddAsync(order, cancellationToken);

        var billingInfo = BuildGatewayBillingInfo(request, order.Id);
        var lineItems = order.Items.Select(i => new PaymentLineItem(
            i.Id.ToString(),
            $"{i.ModuleName} — {i.PlanLabel}",
            "Software/Subscription",
            i.UnitPrice)).ToList();

        var intentRequest = new PaymentIntentRequest(
            order.Id,
            order.OrderNumber,
            order.TotalAmount,
            order.Currency,
            tenantId,
            request.CurrentUserId,
            $"CoreAlign subscription {order.OrderNumber}",
            new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["orderId"] = order.Id.ToString(),
                ["orderNumber"] = order.OrderNumber,
            },
            billingInfo,
            lineItems);
        var intent = await gateway.CreateIntentAsync(intentRequest, cancellationToken);
        order.AttachIntent(gateway.Name, intent.IntentId, intent.RedirectUrl);

        await _attempts.AddAsync(new PaymentAttempt(
            order.Id,
            gateway.Name,
            intent.IntentId,
            PaymentAttemptStatus.Initiated,
            order.TotalAmount,
            order.Currency,
            intent.RawJson), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return new SubscriptionOrderCreationResult(
            BillingMapper.ToDto(order),
            gateway.Name,
            intent.IntentId,
            intent.RedirectUrl);
    }

    private string ResolveGatewayName(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return requested.Trim();
        var fallback = _options.Value.DefaultGatewayName;
        if (string.IsNullOrWhiteSpace(fallback))
        {
            throw new PaymentGatewayNotConfiguredException();
        }
        return fallback.Trim();
    }

    private static PaymentBillingInfo? BuildGatewayBillingInfo(CreateSubscriptionOrderCommand request, Guid orderId)
    {
        if (request.BillingInfo is null) return null;
        var bi = request.BillingInfo;
        var ip = string.IsNullOrWhiteSpace(request.BuyerIpAddress) ? "127.0.0.1" : request.BuyerIpAddress!.Trim();
        return new PaymentBillingInfo(
            Name: bi.Name?.Trim() ?? string.Empty,
            Surname: bi.Surname?.Trim() ?? string.Empty,
            Email: bi.Email?.Trim() ?? string.Empty,
            GsmNumber: bi.GsmNumber?.Trim() ?? string.Empty,
            IdentityNumber: bi.IdentityNumber?.Trim() ?? string.Empty,
            IpAddress: ip,
            Address: bi.Address?.Trim() ?? string.Empty,
            City: bi.City?.Trim() ?? string.Empty,
            Country: bi.Country?.Trim() ?? string.Empty,
            ZipCode: bi.ZipCode?.Trim() ?? string.Empty);
    }
}
