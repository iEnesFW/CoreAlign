using CoreAlign.Application.Billing.Payments;

namespace CoreAlign.Application.Providers.Payment;

public interface IPaymentProvider : IPaymentGateway, IExternalProvider
{
    new string Name { get; }

    Task<IReadOnlyList<PaymentMethodDescriptor>> ListMethodsAsync(Guid tenantId, CancellationToken ct);

    Task<PaymentLinkResult> CreateLinkAsync(PaymentIntentRequest req, PaymentLinkOptions opts, CancellationToken ct);
}

public enum PaymentMethodKind
{
    CardOnFile = 0,
    ThreeDS = 1,
    BankTransfer = 2,
    QrCode = 3,
    BankLink = 4,
    MobilePos = 5,
    Installment = 6,
}

public sealed record PaymentMethodDescriptor(
    PaymentMethodKind Kind,
    string DisplayName,
    decimal MinAmount,
    decimal MaxAmount,
    IReadOnlyList<string> SupportedCurrencies);

public sealed record PaymentIntentRequest(
    decimal Amount,
    string Currency,
    string OrderReference,
    string BuyerName,
    string BuyerEmail);

public sealed record PaymentLinkOptions(
    int ExpiryMinutes,
    string CallbackUrl);

public sealed record PaymentLinkResult(
    string LinkUrl,
    DateTime ExpiresAtUtc,
    string ProviderRefId);
