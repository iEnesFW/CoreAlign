using CoreAlign.Application.Shipments.Commands;
using FluentValidation;

namespace CoreAlign.Application.Shipments.Validators;

public class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.ShipmentRequiresAtLeastOneLine");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.OrderLineId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
            line.RuleFor(l => l.SerialNumber).MaximumLength(64);
            line.RuleFor(l => l.Notes).MaximumLength(500);
        });
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class PickShipmentCommandValidator : AbstractValidator<PickShipmentCommand>
{
    public PickShipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class PackShipmentCommandValidator : AbstractValidator<PackShipmentCommand>
{
    public PackShipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DispatchShipmentCommandValidator : AbstractValidator<DispatchShipmentCommand>
{
    public DispatchShipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CarrierName).MaximumLength(120);
        RuleFor(x => x.TrackingNumber).MaximumLength(120);
        RuleFor(x => x.TrackingUrl).MaximumLength(500);
        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0m).When(x => x.ShippingCost.HasValue)
            .WithMessage("Validation.NonNegative");
    }
}

public class DeliverShipmentCommandValidator : AbstractValidator<DeliverShipmentCommand>
{
    public DeliverShipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReceivedBy).MaximumLength(200);
        RuleFor(x => x.DeliveredAtUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .When(x => x.DeliveredAtUtc.HasValue)
            .WithMessage("Validation.DeliveryDateCannotBeInFuture");
    }
}

public class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
{
    public CancelShipmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
