using CoreAlign.Application.Providers.Payment.Commands;
using MediatR;

namespace CoreAlign.Application.Providers.Payment.Handlers;

/// <summary>
/// Routes <see cref="ChargePaymentCommand"/> through the MediatR pipeline
/// so the <c>TransactionBehavior</c> envelopes the dispatcher call. The
/// dispatcher itself still persists each PaymentTransaction state change
/// via <c>SaveChangesAsync</c>; running inside a MediatR transaction
/// guarantees outbox + audit + ledger commit atomically.
/// </summary>
public sealed class ChargePaymentCommandHandler : IRequestHandler<ChargePaymentCommand, PaymentDispatchResult>
{
    private readonly IPaymentDispatcher _dispatcher;

    public ChargePaymentCommandHandler(IPaymentDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<PaymentDispatchResult> Handle(ChargePaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcherRequest = new PaymentChargeRequest(
            OrderId: request.OrderId,
            InvoiceId: request.InvoiceId,
            Amount: request.Amount,
            Currency: request.Currency,
            OrderReference: request.OrderReference,
            BuyerName: request.BuyerName,
            BuyerEmail: request.BuyerEmail,
            BuyerIp: request.BuyerIp,
            CardToken: request.CardToken,
            RequestThreeDSecure: request.RequestThreeDSecure,
            CallbackUrl: request.CallbackUrl,
            Metadata: request.Metadata,
            IdempotencyKey: request.IdempotencyKey);

        return _dispatcher.ChargeAsync(dispatcherRequest, cancellationToken);
    }
}

public sealed class InitiateThreeDSecureCommandHandler : IRequestHandler<InitiateThreeDSecureCommand, Payment3DSecureInitResult>
{
    private readonly IPaymentDispatcher _dispatcher;

    public InitiateThreeDSecureCommandHandler(IPaymentDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<Payment3DSecureInitResult> Handle(InitiateThreeDSecureCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcherRequest = new Payment3DSecureRequest(
            OrderId: request.OrderId,
            InvoiceId: request.InvoiceId,
            Amount: request.Amount,
            Currency: request.Currency,
            OrderReference: request.OrderReference,
            CallbackUrl: request.CallbackUrl,
            BuyerName: request.BuyerName,
            BuyerEmail: request.BuyerEmail,
            BuyerIp: request.BuyerIp,
            CardToken: request.CardToken,
            Metadata: request.Metadata,
            IdempotencyKey: request.IdempotencyKey);

        return _dispatcher.Initiate3DSecureAsync(dispatcherRequest, cancellationToken);
    }
}

public sealed class VerifyThreeDSecureCommandHandler : IRequestHandler<VerifyThreeDSecureCommand, Payment3DSecureVerifyResult>
{
    private readonly IPaymentDispatcher _dispatcher;

    public VerifyThreeDSecureCommandHandler(IPaymentDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<Payment3DSecureVerifyResult> Handle(VerifyThreeDSecureCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var callback = new Payment3DSecureCallback(
            ProviderName: request.ProviderName,
            TransactionId: request.TransactionId,
            CallbackFields: request.CallbackFields);

        return _dispatcher.Verify3DSecureAsync(callback, cancellationToken);
    }
}

public sealed class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, PaymentRefundResult>
{
    private readonly IPaymentDispatcher _dispatcher;

    public RefundPaymentCommandHandler(IPaymentDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<PaymentRefundResult> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _dispatcher.RefundAsync(
            request.TransactionId,
            request.Amount,
            request.Reason,
            cancellationToken);
    }
}
