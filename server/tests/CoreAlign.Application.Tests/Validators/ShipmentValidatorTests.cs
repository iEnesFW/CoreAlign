using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Shipments.Validators;

namespace CoreAlign.Application.Tests.Validators;

public class ShipmentValidatorTests
{
    [Fact]
    public void CreateShipment_rejects_empty_order_id()
    {
        var v = new CreateShipmentCommandValidator();
        var cmd = new CreateShipmentCommand(Guid.Empty, Guid.NewGuid(), new List<ShipmentLineInput>
        {
            new(Guid.NewGuid(), 1m),
        });
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateShipment_rejects_empty_lines()
    {
        var v = new CreateShipmentCommandValidator();
        var cmd = new CreateShipmentCommand(Guid.NewGuid(), Guid.NewGuid(), new List<ShipmentLineInput>());
        var result = v.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.ShipmentRequiresAtLeastOneLine");
    }

    [Fact]
    public void CreateShipment_rejects_non_positive_line_quantity()
    {
        var v = new CreateShipmentCommandValidator();
        var cmd = new CreateShipmentCommand(Guid.NewGuid(), Guid.NewGuid(), new List<ShipmentLineInput>
        {
            new(Guid.NewGuid(), 0m),
        });
        var result = v.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.QuantityMustBePositive");
    }

    [Fact]
    public void CreateShipment_accepts_valid_command()
    {
        var v = new CreateShipmentCommandValidator();
        var cmd = new CreateShipmentCommand(Guid.NewGuid(), Guid.NewGuid(), new List<ShipmentLineInput>
        {
            new(Guid.NewGuid(), 2.5m, null, "S1", "note"),
        }, "shipment notes");
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PickShipment_rejects_empty_id()
    {
        var v = new PickShipmentCommandValidator();
        v.Validate(new PickShipmentCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DispatchShipment_rejects_negative_shipping_cost()
    {
        var v = new DispatchShipmentCommandValidator();
        var cmd = new DispatchShipmentCommand(Guid.NewGuid(), "UPS", "TRK", "https://x", ShippingCost: -1m);
        var result = v.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.NonNegative");
    }

    [Fact]
    public void DispatchShipment_accepts_null_shipping_cost()
    {
        var v = new DispatchShipmentCommandValidator();
        var cmd = new DispatchShipmentCommand(Guid.NewGuid(), null, null, null, null);
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeliverShipment_rejects_far_future_delivered_date()
    {
        var v = new DeliverShipmentCommandValidator();
        var cmd = new DeliverShipmentCommand(Guid.NewGuid(), "R", DateTime.UtcNow.AddDays(30));
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeliverShipment_accepts_recent_delivery_date()
    {
        var v = new DeliverShipmentCommandValidator();
        var cmd = new DeliverShipmentCommand(Guid.NewGuid(), "R", DateTime.UtcNow.AddHours(-1));
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CancelShipment_truncates_oversized_reason()
    {
        var v = new CancelShipmentCommandValidator();
        var cmd = new CancelShipmentCommand(Guid.NewGuid(), new string('x', 501));
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PackShipment_rejects_empty_id()
    {
        var v = new PackShipmentCommandValidator();
        v.Validate(new PackShipmentCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
