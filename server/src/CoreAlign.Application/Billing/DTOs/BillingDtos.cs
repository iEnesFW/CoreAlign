using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Billing.DTOs;

public record ModulePricePlanDto(
    Guid Id,
    Guid ModuleId,
    string Code,
    string DisplayLabel,
    int DurationDays,
    decimal Price,
    string Currency,
    bool IsActive,
    int SortOrder);

public record ModuleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? Category,
    string? IconKey,
    int SortOrder,
    bool IsActive,
    bool IsCore,
    IReadOnlyList<ModulePricePlanDto> Plans);

public record TenantModuleDto(
    Guid Id,
    Guid ModuleId,
    string Code,
    string Name,
    DateTime StartUtc,
    DateTime? EndUtc,
    bool IsCurrentlyActive,
    TenantModuleSource Source,
    string? Notes);

public record SubscriptionOrderItemDto(
    Guid Id,
    Guid ModuleId,
    Guid PlanId,
    string ModuleCode,
    string ModuleName,
    string PlanLabel,
    int DurationDays,
    decimal UnitPrice,
    string Currency);

public record PaymentAttemptDto(
    Guid Id,
    string GatewayName,
    string? IntentId,
    PaymentAttemptStatus Status,
    decimal Amount,
    string Currency,
    DateTime AttemptedAtUtc,
    DateTime? CompletedAtUtc,
    string? FailureReason);

public record SubscriptionOrderDto(
    Guid Id,
    string OrderNumber,
    SubscriptionOrderStatus Status,
    decimal TotalAmount,
    string Currency,
    Guid CreatedByUserId,
    string? GatewayName,
    string? GatewayIntentId,
    string? PaymentReference,
    DateTime? PaidAtUtc,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    SubscriptionOrderBillingDto? BillingInfo,
    IReadOnlyList<SubscriptionOrderItemDto> Items,
    IReadOnlyList<PaymentAttemptDto> Attempts);

/// <summary>
/// Read-only projection of the buyer / billing snapshot. Identity number is
/// MASKED (first 5 chars + asterisks) — never echo the raw value.
/// </summary>
public record SubscriptionOrderBillingDto(
    string? BuyerName,
    string? BuyerSurname,
    string? BuyerEmail,
    string? BuyerGsmNumber,
    string? BuyerIdentityNumberMasked,
    string? BillingAddress,
    string? BillingCity,
    string? BillingCountry,
    string? BillingZipCode);

public record SubscriptionOrderCreationResult(
    SubscriptionOrderDto Order,
    string GatewayName,
    string? IntentId,
    string? RedirectUrl);

/// <summary>
/// Exposed to the frontend so the checkout UI can render the gateway picker
/// and decide whether to demand the billing-info form.
/// </summary>
public record PaymentGatewayDescriptor(
    string Name,
    string DisplayLabel,
    bool RequiresBillingInfo,
    bool IsDefault);
