using Asp.Versioning;
using CoreAlign.Application.Common;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Application.Providers.Payment.Commands;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

/// <summary>
/// Receives the bank/issuer 3D Secure callback after the user completes the
/// challenge. Anonymous (no JWT) — the request authenticity is established
/// through the provider-specific MAC/signature inside the form payload, which
/// the dispatcher delegates back to the provider for verification.
/// The bank does not send a tenant id, so we resolve it from the persisted
/// <c>PaymentTransaction</c> row via the external transaction id and push the
/// matching tenant scope before invoking the verify command.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/payments/3ds-callback")]
public class Payment3DSecureController : ControllerBase
{
    private const int MaxFormFields = 64;
    private const string SuccessRedirect = "/payment-success";
    private const string FailureRedirect = "/payment-failed";

    private readonly IMediator _mediator;
    private readonly IPaymentTransactionRepository _paymentTxRepo;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<Payment3DSecureController> _logger;

    public Payment3DSecureController(
        IMediator mediator,
        IPaymentTransactionRepository paymentTxRepo,
        ITenantContext tenantContext,
        ILogger<Payment3DSecureController> logger)
    {
        _mediator = mediator;
        _paymentTxRepo = paymentTxRepo;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("{providerName}")]
    public async Task<IActionResult> Callback(string providerName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return BadRequest(ApiResponse<object>.Failure("Provider name is required.", 400));
        }

        IReadOnlyDictionary<string, string> formData;
        try
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in form)
                {
                    if (dict.Count >= MaxFormFields) break;
                    dict[kv.Key] = kv.Value.ToString();
                }
                formData = dict;
            }
            else
            {
                formData = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse 3DS callback form for provider {Provider}.", providerName);
            return BadRequest(ApiResponse<object>.Failure("3DS callback payload could not be parsed.", 400));
        }

        var transactionId = ResolveTransactionId(formData);
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return BadRequest(ApiResponse<object>.Failure("3DS callback missing transaction identifier.", 400));
        }

        var paymentTx = await _paymentTxRepo
            .GetByExternalIdGlobalAsync(providerName, transactionId, cancellationToken)
            .ConfigureAwait(false);
        if (paymentTx is null)
        {
            _logger.LogWarning(
                "3DS callback received for unknown transaction {TransactionId} (provider {Provider}); redirecting to failure.",
                transactionId, providerName);
            return Redirect(FailureRedirect);
        }

        using var tenantScope = _tenantContext.PushScope(paymentTx.TenantId);
        try
        {
            var result = await _mediator
                .Send(new VerifyThreeDSecureCommand(providerName, transactionId, formData), cancellationToken)
                .ConfigureAwait(false);
            return Redirect(result.Success ? SuccessRedirect : FailureRedirect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "3DS callback verification failed for provider {Provider}.", providerName);
            return Redirect(FailureRedirect);
        }
    }

    private static string ResolveTransactionId(IReadOnlyDictionary<string, string> data)
    {
        foreach (var key in new[] { "paymentId", "payment_id", "PaymentId", "merchant_oid", "intentId", "transaction_id" })
        {
            if (data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return string.Empty;
    }
}
