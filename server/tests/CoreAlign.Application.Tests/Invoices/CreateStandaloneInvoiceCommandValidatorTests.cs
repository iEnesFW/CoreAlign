using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Validators;

namespace CoreAlign.Application.Tests.Invoices;

public class CreateStandaloneInvoiceCommandValidatorTests
{
    private readonly CreateStandaloneInvoiceCommandValidator _validator = new();

    private static CreateStandaloneInvoiceCommand BuildCommand(
        IReadOnlyList<StandaloneInvoiceLineInput>? lines = null,
        decimal? headerDiscountPercent = null) =>
        new(
            CustomerId: Guid.NewGuid(),
            IssueDate: DateTime.UtcNow,
            Currency: "TRY",
            Lines: lines ?? [new StandaloneInvoiceLineInput(null, "SKU-1", "Hizmet", null, 1m, 100m, 20m)],
            HeaderDiscountPercent: headerDiscountPercent);

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(BuildCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Command_without_lines_fails()
    {
        var result = _validator.Validate(BuildCommand(lines: []));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Line_with_zero_quantity_fails()
    {
        var lines = new[] { new StandaloneInvoiceLineInput(null, "SKU-1", "Hizmet", null, 0m, 100m, 20m) };

        var result = _validator.Validate(BuildCommand(lines));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.QuantityMustBePositive");
    }

    [Fact]
    public void Line_with_out_of_range_tax_rate_fails()
    {
        var lines = new[] { new StandaloneInvoiceLineInput(null, "SKU-1", "Hizmet", null, 1m, 100m, 150m) };

        var result = _validator.Validate(BuildCommand(lines));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Header_discount_above_hundred_percent_fails()
    {
        var result = _validator.Validate(BuildCommand(headerDiscountPercent: 120m));

        result.IsValid.Should().BeFalse();
    }
}
