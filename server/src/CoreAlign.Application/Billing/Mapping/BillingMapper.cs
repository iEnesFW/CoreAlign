using CoreAlign.Application.Billing.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Billing.Mapping;

public static class BillingMapper
{
    public static ModulePricePlanDto ToDto(ModulePricePlan plan) => new(
        plan.Id,
        plan.ModuleId,
        plan.Code,
        plan.DisplayLabel,
        plan.DurationDays,
        plan.Price,
        plan.Currency,
        plan.IsActive,
        plan.SortOrder);

    public static ModuleDto ToDto(Module module, IReadOnlyList<ModulePricePlan> plans) => new(
        module.Id,
        module.Code,
        module.Name,
        module.Description,
        module.Category,
        module.IconKey,
        module.SortOrder,
        module.IsActive,
        module.IsCore,
        plans.Select(ToDto).ToList());

    public static TenantModuleDto ToDto(TenantModule tm, Module module) => new(
        tm.Id,
        tm.ModuleId,
        module.Code,
        module.Name,
        tm.StartUtc,
        tm.EndUtc,
        tm.IsCurrentlyActive,
        tm.Source,
        tm.Notes);

    public static SubscriptionOrderItemDto ToDto(SubscriptionOrderItem item) => new(
        item.Id,
        item.ModuleId,
        item.PlanId,
        item.ModuleCode,
        item.ModuleName,
        item.PlanLabel,
        item.DurationDays,
        item.UnitPrice,
        item.Currency);

    public static PaymentAttemptDto ToDto(PaymentAttempt attempt) => new(
        attempt.Id,
        attempt.GatewayName,
        attempt.IntentId,
        attempt.Status,
        attempt.Amount,
        attempt.Currency,
        attempt.AttemptedAtUtc,
        attempt.CompletedAtUtc,
        attempt.FailureReason);

    public static SubscriptionOrderDto ToDto(SubscriptionOrder order) => new(
        order.Id,
        order.OrderNumber,
        order.Status,
        order.TotalAmount,
        order.Currency,
        order.CreatedByUserId,
        order.GatewayName,
        order.GatewayIntentId,
        order.PaymentReference,
        order.PaidAtUtc,
        order.CompletedAtUtc,
        order.CreatedAtUtc,
        BuildBilling(order),
        order.Items.Select(ToDto).ToList(),
        order.Attempts.Select(ToDto).ToList());

    private static SubscriptionOrderBillingDto? BuildBilling(SubscriptionOrder order)
    {
        var any = !string.IsNullOrWhiteSpace(order.BuyerName)
            || !string.IsNullOrWhiteSpace(order.BuyerSurname)
            || !string.IsNullOrWhiteSpace(order.BuyerEmail)
            || !string.IsNullOrWhiteSpace(order.BuyerGsmNumber)
            || !string.IsNullOrWhiteSpace(order.BuyerIdentityNumber)
            || !string.IsNullOrWhiteSpace(order.BillingAddress)
            || !string.IsNullOrWhiteSpace(order.BillingCity)
            || !string.IsNullOrWhiteSpace(order.BillingCountry)
            || !string.IsNullOrWhiteSpace(order.BillingZipCode);
        if (!any) return null;
        return new SubscriptionOrderBillingDto(
            order.BuyerName,
            order.BuyerSurname,
            order.BuyerEmail,
            order.BuyerGsmNumber,
            MaskIdentity(order.BuyerIdentityNumber),
            order.BillingAddress,
            order.BillingCity,
            order.BillingCountry,
            order.BillingZipCode);
    }

    /// <summary>
    /// PII safety: returns up to the first 5 visible characters followed by
    /// asterisks for the rest. Null/empty returns null.
    /// </summary>
    public static string? MaskIdentity(string? identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return null;
        var trimmed = identity.Trim();
        if (trimmed.Length <= 5) return new string('*', trimmed.Length);
        return string.Concat(trimmed.AsSpan(0, 5), new string('*', trimmed.Length - 5));
    }
}
