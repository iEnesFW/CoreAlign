using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Validators;

namespace CoreAlign.Application.Tests.Billing;

public class CreateSubscriptionOrderCommandValidatorTests
{
    private readonly CreateSubscriptionOrderCommandValidator _sut = new();

    private static OrderItemInput[] OneItem() => new[] { new OrderItemInput(Guid.NewGuid(), Guid.NewGuid()) };

    [Fact]
    public void Mock_gateway_does_not_require_billing_info()
    {
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "mock", BillingInfo: null, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Null_gateway_does_not_require_billing_info()
    {
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: null, BillingInfo: null, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Iyzico_without_billing_info_fails()
    {
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "iyzico", BillingInfo: null, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.BillingInfoRequired");
    }

    [Fact]
    public void Iyzico_with_full_valid_billing_info_passes()
    {
        var bi = new SubscriptionBillingInfoInput(
            Name: "Ali",
            Surname: "Yilmaz",
            Email: "ali@example.com",
            GsmNumber: "+905551112233",
            IdentityNumber: "12345678901",
            Address: "Maslak Mh. Buyukdere Cd. No 1",
            City: "Istanbul",
            Country: "Turkey",
            ZipCode: "34000");
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "iyzico", BillingInfo: bi, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Iyzico_with_invalid_email_fails()
    {
        var bi = BuildValid() with { Email = "not-an-email" };
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "iyzico", BillingInfo: bi, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.BillingEmailInvalid");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("phone-but-letters")]
    [InlineData("12345")]
    public void Iyzico_with_invalid_gsm_fails(string gsm)
    {
        var bi = BuildValid() with { GsmNumber = gsm };
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "iyzico", BillingInfo: bi, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.BillingGsmInvalid");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("1234")]
    public void Iyzico_with_short_identity_fails(string identity)
    {
        var bi = BuildValid() with { IdentityNumber = identity };
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "iyzico", BillingInfo: bi, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.BillingIdentityInvalid");
    }

    [Fact]
    public void Iyzico_with_single_char_country_fails()
    {
        var bi = BuildValid() with { Country = "T" };
        var command = new CreateSubscriptionOrderCommand(OneItem(), GatewayName: "iyzico", BillingInfo: bi, CurrentUserId: Guid.NewGuid());

        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.BillingCountryInvalid");
    }

    [Fact]
    public void Empty_items_fail()
    {
        var command = new CreateSubscriptionOrderCommand(Array.Empty<OrderItemInput>(), GatewayName: "mock", CurrentUserId: Guid.NewGuid());
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    private static SubscriptionBillingInfoInput BuildValid() => new(
        Name: "Ali",
        Surname: "Yilmaz",
        Email: "ali@example.com",
        GsmNumber: "+905551112233",
        IdentityNumber: "12345678901",
        Address: "Maslak Mh. Buyukdere Cd. No 1",
        City: "Istanbul",
        Country: "Turkey",
        ZipCode: "34000");
}
