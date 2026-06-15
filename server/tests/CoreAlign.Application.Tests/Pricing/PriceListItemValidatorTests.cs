using CoreAlign.Application.Pricing.PriceListItems.Commands;
using CoreAlign.Application.Pricing.PriceListItems.Validators;

namespace CoreAlign.Application.Tests.Pricing;

public class PriceListItemValidatorTests
{
    [Fact]
    public void Add_rejects_min_greater_than_max()
    {
        var validator = new AddPriceListItemCommandValidator();
        var cmd = new AddPriceListItemCommand(Guid.NewGuid(), Guid.NewGuid(), 10m, MinQuantity: 100m, MaxQuantity: 50m);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.MinQuantityMustBeLessThanMaxQuantity");
    }

    [Fact]
    public void Add_rejects_discount_percent_above_100()
    {
        var validator = new AddPriceListItemCommandValidator();
        var cmd = new AddPriceListItemCommand(Guid.NewGuid(), Guid.NewGuid(), 10m, DiscountPercent: 150m);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.DiscountPercentRange");
    }

    [Fact]
    public void Add_accepts_valid_command()
    {
        var validator = new AddPriceListItemCommandValidator();
        var cmd = new AddPriceListItemCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, 1m, 10m, 5m);
        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Update_requires_non_negative_price()
    {
        var validator = new UpdatePriceListItemCommandValidator();
        var cmd = new UpdatePriceListItemCommand(Guid.NewGuid(), Guid.NewGuid(), -1m, null, null, null);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.PriceMustBeNonNegative");
    }
}
