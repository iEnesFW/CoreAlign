using CoreAlign.Application.Common.Validation;

namespace CoreAlign.Application.Tests.Common.Validation;

public class CountryAddressRulesTests
{
    [Theory]
    [InlineData("TR", "34000")]
    [InlineData("TR", "06800")]
    public void IsValidPostalCode_returns_true_for_valid_tr_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("TR", "3400")]
    [InlineData("TR", "340000")]
    [InlineData("TR", "ABCDE")]
    public void IsValidPostalCode_returns_false_for_invalid_tr_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("US", "12345")]
    [InlineData("US", "12345-6789")]
    public void IsValidPostalCode_returns_true_for_valid_us_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("US", "1234")]
    [InlineData("US", "12345-678")]
    [InlineData("US", "ABCDE")]
    public void IsValidPostalCode_returns_false_for_invalid_us_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("GB", "SW1A 1AA")]
    [InlineData("GB", "EC1A1BB")]
    [InlineData("GB", "M11AE")]
    public void IsValidPostalCode_returns_true_for_valid_gb_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("GB", "12345")]
    [InlineData("GB", "INVALID")]
    public void IsValidPostalCode_returns_false_for_invalid_gb_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("DE", "10115")]
    [InlineData("DE", "80331")]
    public void IsValidPostalCode_returns_true_for_valid_de_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("DE", "1011")]
    [InlineData("DE", "101155")]
    [InlineData("DE", "ABCDE")]
    public void IsValidPostalCode_returns_false_for_invalid_de_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("US", true)]
    [InlineData("CA", true)]
    [InlineData("AU", true)]
    [InlineData("TR", false)]
    [InlineData("GB", false)]
    [InlineData("DE", false)]
    public void RequiresState_matches_country_rule(string country, bool expected)
    {
        CountryAddressRules.RequiresState(country).Should().Be(expected);
    }

    [Fact]
    public void IsKnown_returns_true_for_supported_country()
    {
        CountryAddressRules.IsKnown("TR").Should().BeTrue();
        CountryAddressRules.IsKnown("us").Should().BeTrue();
    }

    [Fact]
    public void IsKnown_returns_false_for_unknown_country()
    {
        CountryAddressRules.IsKnown("ZZ").Should().BeFalse();
        CountryAddressRules.IsKnown(null).Should().BeFalse();
        CountryAddressRules.IsKnown("").Should().BeFalse();
    }

    [Fact]
    public void IsValidPostalCode_returns_true_for_unknown_country()
    {
        CountryAddressRules.IsValidPostalCode("ZZ", "anything").Should().BeTrue();
    }

    [Fact]
    public void IsValidPostalCode_returns_false_for_empty_postal_in_known_country()
    {
        CountryAddressRules.IsValidPostalCode("TR", null).Should().BeFalse();
        CountryAddressRules.IsValidPostalCode("TR", "  ").Should().BeFalse();
    }

    [Theory]
    [InlineData("TR", "+90 555 123 4567")]
    [InlineData("US", "+1 (555) 123-4567")]
    [InlineData("DE", "+49 30 12345678")]
    public void IsValidPhoneNumber_accepts_country_formatted_numbers(string country, string phone)
    {
        CountryAddressRules.IsValidPhoneNumber(country, phone).Should().BeTrue();
    }

    [Theory]
    [InlineData("TR", "12345")]
    [InlineData("US", "1234")]
    public void IsValidPhoneNumber_rejects_too_short_numbers(string country, string phone)
    {
        CountryAddressRules.IsValidPhoneNumber(country, phone).Should().BeFalse();
    }

    [Theory]
    [InlineData("TR", "123456789012345")]
    [InlineData("ES", "1234567890")]
    public void IsValidPhoneNumber_rejects_too_long_numbers(string country, string phone)
    {
        CountryAddressRules.IsValidPhoneNumber(country, phone).Should().BeFalse();
    }

    [Fact]
    public void IsValidPhoneNumber_returns_true_when_phone_empty()
    {
        CountryAddressRules.IsValidPhoneNumber("TR", null).Should().BeTrue();
        CountryAddressRules.IsValidPhoneNumber("TR", "").Should().BeTrue();
    }

    [Fact]
    public void IsValidPhoneNumber_returns_true_for_unknown_country()
    {
        CountryAddressRules.IsValidPhoneNumber("ZZ", "anything").Should().BeTrue();
    }

    [Theory]
    [InlineData("CA", "K1A 0B1")]
    [InlineData("CA", "K1A0B1")]
    [InlineData("NL", "1234 AB")]
    [InlineData("NL", "1234AB")]
    [InlineData("AU", "2000")]
    [InlineData("JP", "150-0001")]
    [InlineData("JP", "1500001")]
    public void IsValidPostalCode_returns_true_for_valid_multi_country_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("CA", "12345")]
    [InlineData("NL", "1234")]
    [InlineData("AU", "20000")]
    [InlineData("JP", "150-00")]
    public void IsValidPostalCode_returns_false_for_invalid_multi_country_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("CN", "100000")]
    [InlineData("CN", "518000")]
    public void IsValidPostalCode_returns_true_for_valid_cn_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("CN", "10000")]
    [InlineData("CN", "1000000")]
    [InlineData("CN", "ABCDEF")]
    public void IsValidPostalCode_returns_false_for_invalid_cn_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("BR", "01310-100")]
    [InlineData("BR", "01310100")]
    public void IsValidPostalCode_returns_true_for_valid_br_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("BR", "0131-100")]
    [InlineData("BR", "013101000")]
    [InlineData("BR", "ABCDE-FGH")]
    public void IsValidPostalCode_returns_false_for_invalid_br_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }

    [Theory]
    [InlineData("MX", "01000")]
    [InlineData("MX", "44100")]
    public void IsValidPostalCode_returns_true_for_valid_mx_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeTrue();
    }

    [Theory]
    [InlineData("MX", "1000")]
    [InlineData("MX", "010000")]
    [InlineData("MX", "ABCDE")]
    public void IsValidPostalCode_returns_false_for_invalid_mx_postal(string country, string postal)
    {
        CountryAddressRules.IsValidPostalCode(country, postal).Should().BeFalse();
    }
}
