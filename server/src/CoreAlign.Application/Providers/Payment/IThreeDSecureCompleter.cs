using CoreAlign.Application.Billing.Payments;

namespace CoreAlign.Application.Providers.Payment;

/// <summary>
/// Opt-in capability for a payment provider to AUTHORITATIVELY complete a 3-D
/// Secure flow. The browser-redirect 3DS callback is attacker-controllable, so
/// its self-reported status must never be trusted to capture a payment. A
/// provider implementing this re-establishes the real outcome either by
/// re-querying the provider API (Iyzico/Stripe) or by verifying a provider-keyed
/// signature/hash carried in the callback (PayTR). Providers that do not
/// implement it fall back to the legacy webhook path (dev/mock only).
/// </summary>
public interface IThreeDSecureCompleter
{
    Task<WebhookProcessingResult> CompleteThreeDSecureAsync(
        Payment3DSecureCallback callback,
        CancellationToken cancellationToken = default);
}
