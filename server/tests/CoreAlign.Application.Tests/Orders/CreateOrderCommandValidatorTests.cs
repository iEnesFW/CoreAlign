using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Validators;

namespace CoreAlign.Application.Tests.Orders;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new(TestCurrencyGuard.Accepting("TRY"));

    private static CreateOrderCommand Build(string orderNumber) => new(
        OrderNumber: orderNumber,
        CustomerId: Guid.NewGuid(),
        OrderDate: DateTime.UtcNow,
        Currency: "TRY",
        Notes: null,
        Lines: [new OrderLineInput(Guid.NewGuid(), 1m, 10m)]);

    [Fact]
    public async Task Empty_order_number_passes_so_handler_can_auto_generate()
    {
        (await _validator.ValidateAsync(Build(string.Empty))).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Explicit_order_number_passes()
    {
        (await _validator.ValidateAsync(Build("SO-2026-0001"))).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Order_number_over_64_chars_fails()
    {
        var result = await _validator.ValidateAsync(Build(new string('x', 65)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public async Task Missing_lines_fails()
    {
        var command = Build(string.Empty) with { Lines = [] };

        (await _validator.ValidateAsync(command)).IsValid.Should().BeFalse();
    }
}
