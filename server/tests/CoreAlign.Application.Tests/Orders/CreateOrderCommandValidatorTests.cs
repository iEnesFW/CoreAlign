using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Validators;

namespace CoreAlign.Application.Tests.Orders;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand Build(string orderNumber) => new(
        OrderNumber: orderNumber,
        CustomerId: Guid.NewGuid(),
        OrderDate: DateTime.UtcNow,
        Currency: "TRY",
        Notes: null,
        Lines: [new OrderLineInput(Guid.NewGuid(), 1m, 10m)]);

    [Fact]
    public void Empty_order_number_passes_so_handler_can_auto_generate()
    {
        _validator.Validate(Build(string.Empty)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Explicit_order_number_passes()
    {
        _validator.Validate(Build("SO-2026-0001")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Order_number_over_64_chars_fails()
    {
        var result = _validator.Validate(Build(new string('x', 65)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public void Missing_lines_fails()
    {
        var command = Build(string.Empty) with { Lines = [] };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
