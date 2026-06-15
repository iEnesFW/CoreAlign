using CoreAlign.Application.Common.Validation;

namespace CoreAlign.Application.Tests.Common;

public class TurkishTaxIdValidatorTests
{
    [Theory]
    [InlineData("1234567890")]
    public void IsValidVkn_returns_true_for_valid_vkn(string vkn)
    {
        TurkishTaxIdValidators.IsValidVkn(vkn).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("12345678901")]
    [InlineData("ABCDEFGHIJ")]
    [InlineData("4350309382")]
    [InlineData("1234567891")]
    public void IsValidVkn_returns_false_for_invalid_vkn(string? vkn)
    {
        TurkishTaxIdValidators.IsValidVkn(vkn).Should().BeFalse();
    }

    [Fact]
    public void IsValidVkn_trims_whitespace()
    {
        TurkishTaxIdValidators.IsValidVkn(" 1234567890 ").Should().BeTrue();
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("000000000")]
    [InlineData("987654321")]
    [InlineData("555555555")]
    public void IsValidVkn_check_digit_matches_recomputed(string prefix)
    {
        var check = ComputeVknCheckDigit(prefix);
        var candidate = prefix + check;
        TurkishTaxIdValidators.IsValidVkn(candidate).Should().BeTrue();
        var wrong = prefix + ((check + 1) % 10);
        TurkishTaxIdValidators.IsValidVkn(wrong).Should().BeFalse();
    }

    private static int ComputeVknCheckDigit(string prefix9)
    {
        var d = prefix9.Select(c => c - '0').ToArray();
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var tmp = (d[i] + (9 - i)) % 10;
            var pow = (tmp * (1 << (9 - i))) % 9;
            if (tmp != 0 && pow == 0) pow = 9;
            sum += pow;
        }
        return (10 - (sum % 10)) % 10;
    }

    [Theory]
    [InlineData("10000000146")]
    [InlineData("12345678950")]
    public void IsValidTckn_returns_true_for_valid_tckn(string tckn)
    {
        TurkishTaxIdValidators.IsValidTckn(tckn).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("00000000000")]
    [InlineData("01234567890")]
    [InlineData("12345678901")]
    [InlineData("12345A78950")]
    public void IsValidTckn_returns_false_for_invalid_tckn(string? tckn)
    {
        TurkishTaxIdValidators.IsValidTckn(tckn).Should().BeFalse();
    }

    [Fact]
    public void IsValidTckn_rejects_zero_first_digit()
    {
        TurkishTaxIdValidators.IsValidTckn("01234567890").Should().BeFalse();
    }

    [Fact]
    public void IsValidTckn_validates_check_digits()
    {
        TurkishTaxIdValidators.IsValidTckn("10000000147").Should().BeFalse();
        TurkishTaxIdValidators.IsValidTckn("10000000156").Should().BeFalse();
    }
}
