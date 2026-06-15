using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.Validators;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Validators;

public class MasterDataValidatorTests
{
    [Fact]
    public void CreateBrand_rejects_empty_code()
    {
        var v = new CreateBrandCommandValidator();
        v.Validate(new CreateBrandCommand("", "Name")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateBrand_rejects_invalid_characters_in_code()
    {
        var v = new CreateBrandCommandValidator();
        var result = v.Validate(new CreateBrandCommand("apl!", "Apple"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.CodeFormat");
    }

    [Fact]
    public void CreateBrand_accepts_alphanumeric_with_dot_dash_underscore()
    {
        var v = new CreateBrandCommandValidator();
        v.Validate(new CreateBrandCommand("APL_1.0-x", "Apple")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProductCategory_rejects_self_parent()
    {
        var v = new UpdateProductCategoryCommandValidator();
        var id = Guid.NewGuid();
        var result = v.Validate(new UpdateProductCategoryCommand(id, "C", "name", id, null, true));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.CategoryCannotBeOwnParent");
    }

    [Fact]
    public void CreateUnitOfMeasure_rejects_zero_conversion_factor()
    {
        var v = new CreateUnitOfMeasureCommandValidator();
        var result = v.Validate(new CreateUnitOfMeasureCommand("KG", "Kilogram", "kg", null, 0m, 2));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.MustBePositive");
    }

    [Fact]
    public void CreateUnitOfMeasure_rejects_decimal_places_above_8()
    {
        var v = new CreateUnitOfMeasureCommandValidator();
        v.Validate(new CreateUnitOfMeasureCommand("KG", "K", "kg", null, 1m, 9)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateUnitOfMeasure_rejects_self_base()
    {
        var v = new UpdateUnitOfMeasureCommandValidator();
        var id = Guid.NewGuid();
        var result = v.Validate(new UpdateUnitOfMeasureCommand(id, "G", "Gram", "g", id, 1m, 2, true));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.UoMCannotBeOwnBase");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreateTaxRate_rejects_out_of_range_percent(decimal pct)
    {
        var v = new CreateTaxRateCommandValidator();
        var result = v.Validate(new CreateTaxRateCommand("KDV", "KDV", pct));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.TaxRateRange");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(100)]
    public void CreateTaxRate_accepts_in_range_percent(decimal pct)
    {
        var v = new CreateTaxRateCommandValidator();
        v.Validate(new CreateTaxRateCommand("KDV", "KDV", pct)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePaymentTerm_rejects_discount_days_exceeding_net_days()
    {
        var v = new CreatePaymentTermCommandValidator();
        var result = v.Validate(new CreatePaymentTermCommand("PT", "Term", NetDays: 10, DiscountDays: 20, DiscountPercent: 2m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.DiscountDaysExceedsNetDays");
    }

    [Fact]
    public void CreatePaymentTerm_rejects_negative_net_days()
    {
        var v = new CreatePaymentTermCommandValidator();
        v.Validate(new CreatePaymentTermCommand("PT", "Term", NetDays: -1)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreatePaymentTerm_rejects_discount_percent_above_100()
    {
        var v = new CreatePaymentTermCommandValidator();
        v.Validate(new CreatePaymentTermCommand("PT", "Term", 30, 10, 150m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreatePriceList_requires_iso_currency()
    {
        var v = new CreatePriceListCommandValidator();
        var result = v.Validate(new CreatePriceListCommand("PL", "PL", "us"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.CurrencyIsoFormat");
    }

    [Fact]
    public void CreatePriceList_rejects_valid_until_before_valid_from()
    {
        var v = new CreatePriceListCommandValidator();
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var until = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = v.Validate(new CreatePriceListCommand("PL", "PL", "USD", false, from, until));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.ValidUntilMustBeAfterValidFrom");
    }

    [Fact]
    public void CreatePriceList_accepts_valid_range()
    {
        var v = new CreatePriceListCommandValidator();
        v.Validate(new CreatePriceListCommand("PL", "PL", "USD",
            true,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateWarehouse_rejects_empty_code()
    {
        var v = new CreateWarehouseCommandValidator();
        v.Validate(new CreateWarehouseCommand("", "W", WarehouseType.Main)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateWarehouse_truncates_long_phone()
    {
        var v = new UpdateWarehouseCommandValidator();
        var cmd = new UpdateWarehouseCommand(
            Guid.NewGuid(), "W", "WH", WarehouseType.Main, null, null, null, null, null, null,
            Phone: new string('1', 100), ManagerUserId: null, IsDefault: false, IsActive: true);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCustomerGroup_rejects_discount_above_100()
    {
        var v = new CreateCustomerGroupCommandValidator();
        var result = v.Validate(new CreateCustomerGroupCommand("CG", "G1", null, null, DefaultDiscountPercent: 150m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.DiscountPercentRange");
    }

    [Fact]
    public void CreateCustomerGroup_accepts_zero_discount()
    {
        var v = new CreateCustomerGroupCommandValidator();
        v.Validate(new CreateCustomerGroupCommand("CG", "G1")).IsValid.Should().BeTrue();
    }
}
