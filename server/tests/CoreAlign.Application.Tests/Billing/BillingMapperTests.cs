using CoreAlign.Application.Billing.Mapping;

namespace CoreAlign.Application.Tests.Billing;

public class BillingMapperTests
{
    [Theory]
    [InlineData("12345678901", "12345******")]
    [InlineData("11111", "*****")]
    [InlineData("12", "**")]
    [InlineData("123456", "12345*")]
    public void MaskIdentity_returns_first_five_then_asterisks(string input, string expected)
    {
        BillingMapper.MaskIdentity(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MaskIdentity_returns_null_for_empty(string? input)
    {
        BillingMapper.MaskIdentity(input).Should().BeNull();
    }
}
