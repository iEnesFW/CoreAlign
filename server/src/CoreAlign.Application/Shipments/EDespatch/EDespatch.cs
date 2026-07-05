using System.Text.Json;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Shipments.DTOs;
using CoreAlign.Application.Shipments.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Shipments.EDespatch;

public record IssueEDespatchCommand(
    Guid ShipmentId,
    string? CarrierVkn = null,
    string? VehiclePlate = null,
    string? DriverName = null,
    string? DriverTckn = null,
    Guid? OperationId = null) : IRequest<ShipmentDto>, ITransactionalRequest;

public class IssueEDespatchCommandValidator : AbstractValidator<IssueEDespatchCommand>
{
    public IssueEDespatchCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        When(x => !string.IsNullOrWhiteSpace(x.CarrierVkn), () =>
            RuleFor(x => x.CarrierVkn!).Matches(@"^\d{10,11}$").WithMessage("Validation.CarrierVknInvalid"));
        When(x => !string.IsNullOrWhiteSpace(x.DriverTckn), () =>
            RuleFor(x => x.DriverTckn!).Matches(@"^\d{11}$").WithMessage("Validation.DriverTcknInvalid"));
        When(x => !string.IsNullOrWhiteSpace(x.VehiclePlate), () =>
            RuleFor(x => x.VehiclePlate!).MaximumLength(20));
    }
}

public record EDespatchSubmissionRequestedPayload(Guid TenantId, Guid ShipmentId);

public interface IEDespatchSubmissionOutbox
{
    Task EnqueueSubmissionAsync(EDespatchSubmissionRequestedPayload payload, CancellationToken cancellationToken = default);
}

public sealed class EDespatchSubmissionOutbox : IEDespatchSubmissionOutbox
{
    public const string SubmissionMessageType = "EDespatchSubmissionRequested";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOutboxRepository _outbox;
    private readonly IOutboxSignal _signal;

    public EDespatchSubmissionOutbox(IOutboxRepository outbox, IOutboxSignal signal)
    {
        _outbox = outbox;
        _signal = signal;
    }

    public async Task EnqueueSubmissionAsync(EDespatchSubmissionRequestedPayload payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxMessage(SubmissionMessageType, json), cancellationToken);
        _signal.MarkPending();
    }

    internal static T? Deserialize<T>(string payloadJson) where T : class =>
        JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
}

public class IssueEDespatchCommandHandler : IRequestHandler<IssueEDespatchCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IOrderRepository _orders;
    private readonly IEDespatchSubmissionOutbox _outbox;
    private readonly ITenantContext _tenant;

    public IssueEDespatchCommandHandler(
        IShipmentRepository shipments,
        IOrderRepository orders,
        IEDespatchSubmissionOutbox outbox,
        ITenantContext tenant)
    {
        _shipments = shipments;
        _orders = orders;
        _outbox = outbox;
        _tenant = tenant;
    }

    public async Task<ShipmentDto> Handle(IssueEDespatchCommand c, CancellationToken ct)
    {
        var shipment = await _shipments.GetWithLinesAsync(c.ShipmentId, ct) ?? throw new ShipmentNotFoundException();

        // e-İrsaliye sevkiyat çıkışında (Dispatched) düzenlenir; teslim sonrası da izinli.
        if (shipment.Status is not (ShipmentStatus.Dispatched or ShipmentStatus.Delivered))
        {
            throw new InvalidShipmentStateException("e-Despatch can only be issued for a dispatched shipment.");
        }
        // Durable idempotency: bir ETTN alındıysa VEYA bir gönderim halihazırda kuyrukta/terminal durumda
        // (Queued/Submitted/Accepted/Rejected) ise tekrar düzenlenemez — yalnız önceki denemesi Failed olan
        // sevkiyat yeniden düzenlenebilir (kurtarma). Sıralı çift-tık ikinci bir gönderimi kuyruğa almaz;
        // outbox da ayrıca EDespatchUuid ile dedup eder.
        if (!string.IsNullOrEmpty(shipment.EDespatchUuid)
            || (!string.IsNullOrEmpty(shipment.EDespatchStatus)
                && !string.Equals(shipment.EDespatchStatus, EInvoiceStatuses.Failed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new EDespatchAlreadyIssuedException();
        }

        shipment.SetEDespatchCarrier(c.CarrierVkn, c.VehiclePlate, c.DriverName, c.DriverTckn);
        shipment.SetEDespatchProfile("TEMELIRSALIYE");
        shipment.RegisterEDespatch(null, EInvoiceStatuses.Queued);
        _shipments.Update(shipment);

        await _outbox.EnqueueSubmissionAsync(new EDespatchSubmissionRequestedPayload(_tenant.RequireTenantId(), shipment.Id), ct);

        var order = await _orders.GetWithLinesAndShipmentsAsync(shipment.OrderId, ct);
        if (order is not null) shipment.Order = order;
        return ShipmentMapper.ToDto(shipment);
    }
}

public sealed class ShipmentEDespatchOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => EDespatchSubmissionOutbox.SubmissionMessageType;

    private readonly IShipmentRepository _shipments;
    private readonly ICustomerRepository _customers;
    private readonly IElectronicInvoiceGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShipmentEDespatchOutboxHandler> _logger;

    public ShipmentEDespatchOutboxHandler(
        IShipmentRepository shipments,
        ICustomerRepository customers,
        IElectronicInvoiceGateway gateway,
        IUnitOfWork unitOfWork,
        ILogger<ShipmentEDespatchOutboxHandler> logger)
    {
        _shipments = shipments;
        _customers = customers;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = EDespatchSubmissionOutbox.Deserialize<EDespatchSubmissionRequestedPayload>(payloadJson);
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        var shipment = await _shipments.GetWithLinesAsync(payload.ShipmentId, cancellationToken);
        if (shipment is null) return OutboxHandlerResult.Failed($"Shipment {payload.ShipmentId} not found.");

        if (!string.IsNullOrEmpty(shipment.EDespatchUuid))
        {
            return OutboxHandlerResult.Processed("AlreadySubmitted");
        }

        var customer = await _customers.GetByIdAsync(shipment.CustomerId, cancellationToken);
        var addr = shipment.ShippingAddressSnapshot;
        var seller = new SellerParty("Tenant Seller", null, null, null, null, null, null, "Türkiye");
        var buyer = new BuyerParty(
            Name: customer?.Name ?? addr?.RecipientName ?? "Alıcı",
            TaxNumber: customer?.TaxNumber,
            NationalId: customer?.NationalId,
            TaxOffice: customer?.TaxOffice,
            AddressLine: addr?.Line1,
            City: addr?.City,
            PostalCode: addr?.PostalCode,
            Country: addr?.Country ?? "Türkiye");

        var xml = UblTrInvoiceXmlBuilder.BuildDespatch(shipment, seller, buyer);

        var request = new EInvoiceSubmissionRequest(
            shipment.TenantId,
            shipment.Id,
            xml,
            buyer.TaxNumber,
            buyer.Name,
            EInvoiceDocumentKind.Despatch);

        var result = await _gateway.SubmitAsync(request, cancellationToken);

        if (string.Equals(result.Status, "Failed", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(result.RemoteUuid))
        {
            shipment.RegisterEDespatch(null, EInvoiceStatuses.Failed);
            _shipments.Update(shipment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(
                "e-Despatch submission failed for shipment {ShipmentId}: {Reason}",
                shipment.Id, result.FailureReason ?? "unknown");
            return OutboxHandlerResult.Failed(result.FailureReason ?? "Gateway returned no remote uuid.");
        }

        shipment.RegisterEDespatch(result.RemoteUuid, result.Status);
        _shipments.Update(shipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "e-Despatch submitted for shipment {ShipmentId}: uuid={Uuid}, status={Status}",
            shipment.Id, result.RemoteUuid, result.Status);

        return OutboxHandlerResult.Processed($"Submitted:{result.RemoteUuid}");
    }
}
