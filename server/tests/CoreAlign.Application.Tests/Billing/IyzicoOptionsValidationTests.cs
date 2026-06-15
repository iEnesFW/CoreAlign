using System.ComponentModel.DataAnnotations;
using CoreAlign.Infrastructure.Payments;

namespace CoreAlign.Application.Tests.Billing;

public class IyzicoOptionsValidationTests
{
    [Fact]
    public void Api_key_required()
    {
        var options = new IyzicoOptions
        {
            ApiKey = string.Empty,
            SecretKey = "secret",
            BaseUrl = "https://sandbox-api.iyzipay.com",
        };

        var results = Validate(options);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(IyzicoOptions.ApiKey)));
    }

    [Fact]
    public void Secret_key_required()
    {
        var options = new IyzicoOptions
        {
            ApiKey = "api",
            SecretKey = string.Empty,
            BaseUrl = "https://sandbox-api.iyzipay.com",
        };

        var results = Validate(options);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(IyzicoOptions.SecretKey)));
    }

    [Fact]
    public void Valid_options_pass()
    {
        var options = new IyzicoOptions
        {
            ApiKey = "api",
            SecretKey = "secret",
            BaseUrl = "https://sandbox-api.iyzipay.com",
            HttpTimeoutSeconds = 30,
        };

        var results = Validate(options);
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public void Http_timeout_out_of_range_fails(int seconds)
    {
        var options = new IyzicoOptions
        {
            ApiKey = "api",
            SecretKey = "secret",
            HttpTimeoutSeconds = seconds,
        };

        var results = Validate(options);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(IyzicoOptions.HttpTimeoutSeconds)));
    }

    [Fact]
    public void ToString_never_includes_secret()
    {
        var options = new IyzicoOptions
        {
            ApiKey = "api-key-public",
            SecretKey = "DO-NOT-LEAK-secret-VALUE",
            BaseUrl = "https://sandbox-api.iyzipay.com",
        };

        options.ToString().Should().NotContain("DO-NOT-LEAK-secret-VALUE");
        options.ToString().Should().NotContain("api-key-public");
    }

    private static IList<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }
}
