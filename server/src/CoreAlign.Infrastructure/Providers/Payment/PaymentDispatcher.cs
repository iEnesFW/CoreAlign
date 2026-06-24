using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Application.Providers.Payment.Events;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using BillingPaymentIntentRequest = CoreAlign.Application.Billing.Payments.PaymentIntentRequest;

namespace CoreAlign.Infrastructure.Providers.Payment;

/// <summary>
/// F2.2 payment orchestration. Selects exactly one provider per tenant (no
/// failover — payment success/fail is final), retries only transient HTTP
/// errors, persists a <see cref="PaymentTransaction"/> ledger row, writes
/// audit + outbox events, and surfaces the final outcome to the caller.
/// </summary>
public sealed class PaymentDispatcher : IPaymentDispatcher
{
    private const string PaymentInitiatedMessageType = "PaymentInitiated";
    private const string Payment3DSecureRequiredMessageType = "Payment3DSecureRequired";
    private const string PaymentSucceededMessageType = "PaymentSucceeded";
    private const string PaymentFailedMessageType = "PaymentFailed";
    private const string PaymentRefundedMessageType = "PaymentRefunded";
    private const string PaymentAttemptAuditKind = "PaymentDispatchAttempted";
    private const string PaymentRefundAuditKind = "PaymentRefundAttempted";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IProviderRegistry<IPaymentProvider> _registry;
    private readonly ITenantProviderConfigRepository _configRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxSignal _outboxSignal;
    private readonly IAuditContext _auditContext;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentDispatcher> _logger;

    public PaymentDispatcher(
        IProviderRegistry<IPaymentProvider> registry,
        ITenantProviderConfigRepository configRepository,
        IPaymentTransactionRepository transactionRepository,
        IOutboxRepository outboxRepository,
        IOutboxSignal outboxSignal,
        IAuditContext auditContext,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        ILogger<PaymentDispatcher> logger)
    {
        _registry = registry;
        _configRepository = configRepository;
        _transactionRepository = transactionRepository;
        _outboxRepository = outboxRepository;
        _outboxSignal = outboxSignal;
        _auditContext = auditContext;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentDispatchResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount <= 0m)
        {
            throw new ArgumentException("Amount must be positive.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey is required for charge requests.", nameof(request));
        }

        var tenantId = _tenantContext.RequireTenantId();

        var existing = await _transactionRepository
            .GetByIdempotencyKeyAsync(tenantId, request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return BuildResultFromExisting(existing);
        }

        var provider = await ResolvePrimaryProviderAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var transaction = new PaymentTransaction(
            tenantId,
            request.OrderId,
            request.InvoiceId,
            request.OrderReference,
            request.Amount,
            request.Currency,
            provider.Name,
            externalTransactionId: null,
            requiresThreeDSecure: false,
            redirectUrl: null,
            metadataJson: SerializeMetadata(request.Metadata),
            idempotencyKey: request.IdempotencyKey);
        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await EnqueueOutboxAsync(
            PaymentInitiatedMessageType,
            new PaymentInitiatedEvent(
                tenantId,
                transaction.Id,
                provider.Name,
                request.OrderReference,
                request.Amount,
                request.Currency,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        var attempts = new List<PaymentAttemptInfo>(1);
        var intentRequest = BuildIntentRequest(tenantId, request);

        var attemptStartedAt = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();
        PaymentIntentResult? intent = null;
        Exception? failure = null;
        try
        {
            intent = await ExecuteWithRetryAsync(
                ct => provider.CreateIntentAsync(intentRequest, ct),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        sw.Stop();

        if (failure is not null || intent is null)
        {
            var message = failure?.GetBaseException().Message ?? "Provider returned no intent.";
            attempts.Add(new PaymentAttemptInfo(provider.Name, false, "PROVIDER_ERROR", message, attemptStartedAt, sw.Elapsed));
            transaction.MarkFailed("PROVIDER_ERROR", message, metadataJson: null);
            _transactionRepository.Update(transaction);
            CaptureAttemptAudit(transaction.Id, provider.Name, false, "PROVIDER_ERROR", message);
            await EnqueueOutboxAsync(
                PaymentFailedMessageType,
                new PaymentFailedEvent(tenantId, transaction.Id, provider.Name, transaction.ExternalTransactionId, request.OrderReference, request.Amount, request.Currency, "PROVIDER_ERROR", message, DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new PaymentDispatchResult(
                new PaymentChargeOutcome(false, "Failed", null, request.Currency, "PROVIDER_ERROR", message, null),
                provider.Name,
                transaction.ExternalTransactionId ?? string.Empty,
                Requires3DSecure: false,
                RedirectUrl: null,
                attempts);
        }

        attempts.Add(new PaymentAttemptInfo(provider.Name, true, null, null, attemptStartedAt, sw.Elapsed));
        transaction.AttachExternalId(intent.IntentId);

        var requires3ds = intent.Status == PaymentIntentStatus.RequiresAction;
        var success = intent.Status == PaymentIntentStatus.Succeeded;

        if (requires3ds)
        {
            transaction.MarkRequires3DSecure(intent.IntentId, intent.RedirectUrl, RedactPciFields(intent.RawJson));
            _transactionRepository.Update(transaction);
            CaptureAttemptAudit(transaction.Id, provider.Name, true, "REQUIRES_3DS", intent.RedirectUrl);
            await EnqueueOutboxAsync(
                Payment3DSecureRequiredMessageType,
                new Payment3DSecureRequiredEvent(tenantId, transaction.Id, provider.Name, intent.IntentId, request.OrderReference, request.Amount, request.Currency, intent.RedirectUrl, DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new PaymentDispatchResult(
                new PaymentChargeOutcome(false, "Requires3DS", null, request.Currency, null, null, intent.RawJson),
                provider.Name,
                intent.IntentId,
                Requires3DSecure: true,
                RedirectUrl: intent.RedirectUrl,
                attempts);
        }

        if (success)
        {
            transaction.MarkCaptured(intent.IntentId, RedactPciFields(intent.RawJson));
            _transactionRepository.Update(transaction);
            CaptureAttemptAudit(transaction.Id, provider.Name, true, "SUCCESS", null);
            await EnqueueOutboxAsync(
                PaymentSucceededMessageType,
                new PaymentSucceededEvent(tenantId, transaction.Id, provider.Name, intent.IntentId, request.OrderReference, request.Amount, request.Currency, DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new PaymentDispatchResult(
                new PaymentChargeOutcome(true, "Succeeded", request.Amount, request.Currency, null, null, intent.RawJson),
                provider.Name,
                intent.IntentId,
                Requires3DSecure: false,
                RedirectUrl: null,
                attempts);
        }

        var declinedMessage = intent.Status switch
        {
            PaymentIntentStatus.Failed => "Provider declined the charge.",
            PaymentIntentStatus.Cancelled => "Provider cancelled the charge.",
            PaymentIntentStatus.Pending => "Provider is still processing the charge.",
            _ => "Provider returned an unexpected status.",
        };
        var declinedCode = intent.Status.ToString().ToUpperInvariant();
        transaction.MarkFailed(declinedCode, declinedMessage, RedactPciFields(intent.RawJson));
        _transactionRepository.Update(transaction);
        CaptureAttemptAudit(transaction.Id, provider.Name, false, declinedCode, declinedMessage);
        await EnqueueOutboxAsync(
            PaymentFailedMessageType,
            new PaymentFailedEvent(tenantId, transaction.Id, provider.Name, intent.IntentId, request.OrderReference, request.Amount, request.Currency, declinedCode, declinedMessage, DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PaymentDispatchResult(
            new PaymentChargeOutcome(false, intent.Status.ToString(), null, request.Currency, declinedCode, declinedMessage, intent.RawJson),
            provider.Name,
            intent.IntentId,
            Requires3DSecure: false,
            RedirectUrl: null,
            attempts);
    }

    public async Task<Payment3DSecureInitResult> Initiate3DSecureAsync(Payment3DSecureRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            throw new ArgumentException("CallbackUrl is required for 3DS initiate.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey is required for 3DS initiate.", nameof(request));
        }

        var tenantId = _tenantContext.RequireTenantId();

        var existing = await _transactionRepository
            .GetByIdempotencyKeyAsync(tenantId, request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return BuildThreeDSecureInitResultFromExisting(existing);
        }

        var provider = await ResolvePrimaryProviderAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var transaction = new PaymentTransaction(
            tenantId,
            request.OrderId,
            request.InvoiceId,
            request.OrderReference,
            request.Amount,
            request.Currency,
            provider.Name,
            externalTransactionId: null,
            requiresThreeDSecure: true,
            redirectUrl: null,
            metadataJson: SerializeMetadata(request.Metadata),
            idempotencyKey: request.IdempotencyKey);
        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var linkOptions = new PaymentLinkOptions(ExpiryMinutes: 30, CallbackUrl: request.CallbackUrl);
        var providerLinkRequest = new Application.Providers.Payment.PaymentIntentRequest(
            request.Amount,
            request.Currency,
            request.OrderReference,
            request.BuyerName,
            request.BuyerEmail);

        try
        {
            var link = await ExecuteWithRetryAsync(
                ct => provider.CreateLinkAsync(providerLinkRequest, linkOptions, ct),
                cancellationToken).ConfigureAwait(false);

            transaction.MarkRequires3DSecure(link.ProviderRefId, link.LinkUrl, metadataJson: null);
            _transactionRepository.Update(transaction);

            CaptureAttemptAudit(transaction.Id, provider.Name, true, "3DS_INITIATED", link.LinkUrl);
            await EnqueueOutboxAsync(
                Payment3DSecureRequiredMessageType,
                new Payment3DSecureRequiredEvent(tenantId, transaction.Id, provider.Name, link.ProviderRefId, request.OrderReference, request.Amount, request.Currency, link.LinkUrl, DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new Payment3DSecureInitResult(
                Initiated: true,
                ProviderUsed: provider.Name,
                TransactionId: link.ProviderRefId,
                HtmlContent: null,
                RedirectUrl: link.LinkUrl,
                FailureCode: null,
                FailureMessage: null);
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            transaction.MarkFailed("3DS_INIT_FAILED", message, metadataJson: null);
            _transactionRepository.Update(transaction);
            CaptureAttemptAudit(transaction.Id, provider.Name, false, "3DS_INIT_FAILED", message);
            await EnqueueOutboxAsync(
                PaymentFailedMessageType,
                new PaymentFailedEvent(tenantId, transaction.Id, provider.Name, null, request.OrderReference, request.Amount, request.Currency, "3DS_INIT_FAILED", message, DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new Payment3DSecureInitResult(
                Initiated: false,
                ProviderUsed: provider.Name,
                TransactionId: string.Empty,
                HtmlContent: null,
                RedirectUrl: null,
                FailureCode: "3DS_INIT_FAILED",
                FailureMessage: message);
        }
    }

    public async Task<Payment3DSecureVerifyResult> Verify3DSecureAsync(Payment3DSecureCallback callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (string.IsNullOrWhiteSpace(callback.ProviderName))
        {
            throw new ArgumentException("ProviderName is required.", nameof(callback));
        }

        var provider = _registry.Require(callback.ProviderName);
        var tenantId = _tenantContext.RequireTenantId();

        var transaction = await _transactionRepository
            .GetByExternalTransactionIdAsync(provider.Name, callback.TransactionId, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            // SECURITY: the 3DS browser-redirect callback is attacker-controllable; never
            // trust its self-reported status. Providers implementing IThreeDSecureCompleter
            // re-establish the real outcome (API re-query or signature/hash verification);
            // a failure there throws and is handled fail-closed (transaction marked failed).
            var webhookResult = provider is IThreeDSecureCompleter completer
                ? await completer
                    .CompleteThreeDSecureAsync(callback, cancellationToken)
                    .ConfigureAwait(false)
                : await provider
                    .HandleWebhookAsync(SerializeCallback(callback), callback.CallbackFields, cancellationToken)
                    .ConfigureAwait(false);

            var success = webhookResult.Status == PaymentIntentStatus.Succeeded;
            if (transaction is not null)
            {
                if (success)
                {
                    transaction.MarkCaptured(webhookResult.IntentId, RedactPciFields(webhookResult.RawJson));
                    await EnqueueOutboxAsync(
                        PaymentSucceededMessageType,
                        new PaymentSucceededEvent(tenantId, transaction.Id, provider.Name, webhookResult.IntentId, transaction.OrderReference, transaction.Amount, transaction.Currency, DateTime.UtcNow),
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    transaction.MarkFailed("3DS_VERIFY_FAILED", webhookResult.FailureReason ?? webhookResult.Status.ToString(), RedactPciFields(webhookResult.RawJson));
                    await EnqueueOutboxAsync(
                        PaymentFailedMessageType,
                        new PaymentFailedEvent(tenantId, transaction.Id, provider.Name, webhookResult.IntentId, transaction.OrderReference, transaction.Amount, transaction.Currency, "3DS_VERIFY_FAILED", webhookResult.FailureReason, DateTime.UtcNow),
                        cancellationToken).ConfigureAwait(false);
                }
                _transactionRepository.Update(transaction);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            CaptureAttemptAudit(transaction?.Id ?? Guid.Empty, provider.Name, success, success ? "3DS_VERIFIED" : "3DS_VERIFY_FAILED", webhookResult.FailureReason);

            return new Payment3DSecureVerifyResult(
                success,
                provider.Name,
                webhookResult.IntentId,
                webhookResult.Status.ToString(),
                success ? null : "3DS_VERIFY_FAILED",
                success ? null : webhookResult.FailureReason,
                webhookResult.RawJson);
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            if (transaction is not null)
            {
                transaction.MarkFailed("3DS_VERIFY_ERROR", message, metadataJson: null);
                _transactionRepository.Update(transaction);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            CaptureAttemptAudit(transaction?.Id ?? Guid.Empty, provider.Name, false, "3DS_VERIFY_ERROR", message);

            return new Payment3DSecureVerifyResult(
                Success: false,
                ProviderUsed: provider.Name,
                TransactionId: callback.TransactionId,
                Status: "Failed",
                FailureCode: "3DS_VERIFY_ERROR",
                FailureMessage: message,
                RawProviderJson: null);
        }
    }

    public async Task<PaymentRefundResult> RefundAsync(string transactionId, decimal? amount, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("transactionId is required.", nameof(transactionId));
        }
        if (amount is not null && amount.Value <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be positive.");
        }

        var tenantId = _tenantContext.RequireTenantId();
        var transaction = await LoadTransactionForRefundAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
        {
            throw new PaymentTransactionNotFoundException(transactionId);
        }

        var provider = _registry.Require(transaction.ProviderName);

        var refundRequest = new RefundRequest(
            IntentId: transaction.ExternalTransactionId ?? transactionId,
            Amount: amount,
            Reason: reason,
            PaymentTransactionId: transaction.ExternalTransactionId,
            Currency: transaction.Currency);

        RefundResult refundResult;
        try
        {
            refundResult = await ExecuteWithRetryAsync(
                ct => provider.RefundAsync(refundRequest, ct),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            CaptureRefundAudit(transaction.Id, provider.Name, false, "REFUND_ERROR", message);
            return new PaymentRefundResult(false, provider.Name, transactionId, null, null, "REFUND_ERROR", message);
        }

        if (!refundResult.Success)
        {
            CaptureRefundAudit(transaction.Id, provider.Name, false, "REFUND_DECLINED", refundResult.FailureReason);
            return new PaymentRefundResult(false, provider.Name, transactionId, null, null, "REFUND_DECLINED", refundResult.FailureReason);
        }

        var refundedAmount = amount ?? Math.Max(0m, transaction.Amount - transaction.RefundedAmount);
        transaction.RecordRefund(refundedAmount, RedactPciFields(refundResult.RawJson));
        _transactionRepository.Update(transaction);

        CaptureRefundAudit(transaction.Id, provider.Name, true, "REFUND_OK", refundResult.RefundId);
        await EnqueueOutboxAsync(
            PaymentRefundedMessageType,
            new PaymentRefundedEvent(
                tenantId,
                transaction.Id,
                provider.Name,
                transaction.ExternalTransactionId ?? transactionId,
                refundResult.RefundId,
                refundedAmount,
                transaction.Currency,
                reason ?? string.Empty,
                FullyRefunded: transaction.Status == PaymentTransactionStatus.Refunded,
                RefundedAtUtc: DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PaymentRefundResult(
            Success: true,
            ProviderUsed: provider.Name,
            TransactionId: transactionId,
            RefundId: refundResult.RefundId,
            RefundedAmount: refundedAmount,
            FailureCode: null,
            FailureMessage: null);
    }

    public async Task<PaymentTransactionInfo> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("transactionId is required.", nameof(transactionId));
        }

        var transaction = await LoadTransactionForRefundAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
        {
            throw new PaymentTransactionNotFoundException(transactionId);
        }

        return new PaymentTransactionInfo(
            transaction.ProviderName,
            transaction.ExternalTransactionId ?? transaction.Id.ToString(),
            transaction.Status.ToString(),
            transaction.Amount,
            transaction.Currency,
            transaction.CompletedAtUtc,
            transaction.MetadataJson);
    }

    private async Task<IPaymentProvider> ResolvePrimaryProviderAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var configs = await _configRepository
            .ListByTenantAsync(tenantId, ProviderCategory.Payment, cancellationToken)
            .ConfigureAwait(false);

        var primary = configs
            .Where(c => c.IsEnabled)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.ProviderName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (primary is null)
        {
            throw new PaymentProviderNotConfiguredException(tenantId);
        }

        return _registry.Find(primary.ProviderName)
            ?? throw new PaymentProviderNotConfiguredException(tenantId);
    }

    private async Task<PaymentTransaction?> LoadTransactionForRefundAsync(string transactionId, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(transactionId, out var id))
        {
            var byId = await _transactionRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (byId is not null) return byId;
        }

        foreach (var providerName in _registry.Names)
        {
            var match = await _transactionRepository
                .GetByExternalTransactionIdAsync(providerName, transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (match is not null)
            {
                return match;
            }
        }
        return null;
    }

    private static BillingPaymentIntentRequest BuildIntentRequest(Guid tenantId, PaymentChargeRequest request)
    {
        var orderId = request.OrderId ?? Guid.Empty;
        var meta = request.Metadata is null
            ? null
            : (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase);

        return new BillingPaymentIntentRequest(
            OrderId: orderId,
            OrderNumber: request.OrderReference,
            Amount: request.Amount,
            Currency: request.Currency,
            TenantId: tenantId,
            CreatedByUserId: Guid.Empty,
            Description: null,
            Metadata: meta,
            BillingInfo: null,
            LineItems: null);
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0) return null;
        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private static string SerializeCallback(Payment3DSecureCallback callback)
    {
        return JsonSerializer.Serialize(new
        {
            providerName = callback.ProviderName,
            transactionId = callback.TransactionId,
            fields = callback.CallbackFields,
        }, JsonOptions);
    }

    private void CaptureAttemptAudit(Guid aggregateId, string providerName, bool succeeded, string code, string? message)
    {
        try
        {
            var details = JsonSerializer.Serialize(new
            {
                providerName,
                succeeded,
                code,
                message,
                atUtc = DateTime.UtcNow,
            }, JsonOptions);
            _auditContext.CaptureCustom(aggregateId, nameof(PaymentTransaction), PaymentAttemptAuditKind, details);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture payment dispatch audit for provider {Provider}.", providerName);
        }
    }

    private void CaptureRefundAudit(Guid aggregateId, string providerName, bool succeeded, string code, string? detail)
    {
        try
        {
            var details = JsonSerializer.Serialize(new
            {
                providerName,
                succeeded,
                code,
                detail,
                atUtc = DateTime.UtcNow,
            }, JsonOptions);
            _auditContext.CaptureCustom(aggregateId, nameof(PaymentTransaction), PaymentRefundAuditKind, details);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture payment refund audit for provider {Provider}.", providerName);
        }
    }

    private async Task EnqueueOutboxAsync<TEvent>(string messageType, TEvent payload, CancellationToken cancellationToken)
        where TEvent : class
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            await _outboxRepository
                .AddAsync(new OutboxMessage(messageType, json), cancellationToken)
                .ConfigureAwait(false);
            _outboxSignal.MarkPending();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue payment outbox event {Type}.", messageType);
        }
    }

    private static async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                last = ex;
                var delayMs = 200 * Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken).ConfigureAwait(false);
            }
        }
        throw last ?? new InvalidOperationException("Payment provider call failed without a captured exception.");
    }

    private static bool IsTransient(Exception ex) => ex switch
    {
        HttpRequestException => true,
        TaskCanceledException tex when !tex.CancellationToken.IsCancellationRequested => true,
        TimeoutException => true,
        _ => false,
    };

    private static PaymentDispatchResult BuildResultFromExisting(PaymentTransaction existing)
    {
        var success = existing.Status is PaymentTransactionStatus.Captured or PaymentTransactionStatus.Authorized;
        var requires3ds = existing.RequiresThreeDSecure && existing.Status == PaymentTransactionStatus.Pending;
        var statusLabel = existing.Status.ToString();
        var outcome = new PaymentChargeOutcome(
            success,
            statusLabel,
            success ? existing.Amount : (decimal?)null,
            existing.Currency,
            existing.FailureCode,
            existing.FailureReason,
            existing.MetadataJson);
        return new PaymentDispatchResult(
            outcome,
            existing.ProviderName,
            existing.ExternalTransactionId ?? existing.Id.ToString(),
            requires3ds,
            existing.RedirectUrl,
            Array.Empty<PaymentAttemptInfo>());
    }

    private static Payment3DSecureInitResult BuildThreeDSecureInitResultFromExisting(PaymentTransaction existing)
    {
        var initiated = existing.RequiresThreeDSecure && !string.IsNullOrWhiteSpace(existing.RedirectUrl);
        return new Payment3DSecureInitResult(
            Initiated: initiated,
            ProviderUsed: existing.ProviderName,
            TransactionId: existing.ExternalTransactionId ?? existing.Id.ToString(),
            HtmlContent: null,
            RedirectUrl: existing.RedirectUrl,
            FailureCode: existing.FailureCode,
            FailureMessage: existing.FailureReason);
    }

    /// <summary>
    /// Removes raw PAN, CVC, and full expiry date fields from a provider
    /// response payload before it is persisted into <c>MetadataJson</c>. The
    /// providers tokenize at their endpoint so PAN should never appear, but
    /// this is a belt-and-braces filter to keep the ledger PCI-DSS safe.
    /// </summary>
    internal static string? RedactPciFields(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }
        try
        {
            using var doc = JsonDocument.Parse(raw);
            using var stream = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteRedacted(doc.RootElement, writer);
            }
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return PanScrubFallback(raw);
        }
    }

    private static void WriteRedacted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    if (IsCvcField(prop.Name))
                    {
                        writer.WriteNull(prop.Name);
                        continue;
                    }
                    if (IsExpiryField(prop.Name))
                    {
                        writer.WriteNull(prop.Name);
                        continue;
                    }
                    if (IsCardNumberField(prop.Name))
                    {
                        writer.WriteString(prop.Name, MaskPan(prop.Value));
                        continue;
                    }
                    writer.WritePropertyName(prop.Name);
                    WriteRedacted(prop.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedacted(item, writer);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsCardNumberField(string name) =>
        name.Equals("cardNumber", StringComparison.OrdinalIgnoreCase)
        || name.Equals("card_number", StringComparison.OrdinalIgnoreCase)
        || name.Equals("pan", StringComparison.OrdinalIgnoreCase)
        || name.Equals("number", StringComparison.OrdinalIgnoreCase);

    private static bool IsCvcField(string name) =>
        name.Equals("cvc", StringComparison.OrdinalIgnoreCase)
        || name.Equals("cvv", StringComparison.OrdinalIgnoreCase)
        || name.Equals("cvv2", StringComparison.OrdinalIgnoreCase)
        || name.Equals("securityCode", StringComparison.OrdinalIgnoreCase);

    private static bool IsExpiryField(string name) =>
        name.Equals("expiryDate", StringComparison.OrdinalIgnoreCase)
        || name.Equals("expiry_date", StringComparison.OrdinalIgnoreCase)
        || name.Equals("expireMonth", StringComparison.OrdinalIgnoreCase)
        || name.Equals("expireYear", StringComparison.OrdinalIgnoreCase)
        || name.Equals("exp_month", StringComparison.OrdinalIgnoreCase)
        || name.Equals("exp_year", StringComparison.OrdinalIgnoreCase)
        || name.Equals("expiry_month", StringComparison.OrdinalIgnoreCase)
        || name.Equals("expiry_year", StringComparison.OrdinalIgnoreCase);

    private static string MaskPan(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return "****";
        }
        var pan = value.GetString();
        if (string.IsNullOrWhiteSpace(pan))
        {
            return "****";
        }
        var digits = new string(pan.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return "****";
        }
        return "****" + digits[^4..];
    }

    private static readonly Regex PanRegex = new(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled);

    private static string PanScrubFallback(string raw) =>
        PanRegex.Replace(raw, "****");
}
