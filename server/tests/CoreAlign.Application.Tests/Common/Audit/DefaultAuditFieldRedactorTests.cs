using CoreAlign.Application.Common.Audit;

namespace CoreAlign.Application.Tests.Common.Audit;

public class DefaultAuditFieldRedactorTests
{
    private readonly DefaultAuditFieldRedactor _sut = new();

    [Fact]
    public void Password_field_is_masked()
    {
        var redacted = _sut.Redact("Password", "hunter2");

        redacted.Should().Be("***");
    }

    [Fact]
    public void Non_sensitive_field_returns_original_value()
    {
        var redacted = _sut.Redact("Email", "user@example.com");

        redacted.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("Token")]
    [InlineData("TOKEN")]
    [InlineData("token")]
    [InlineData("RefreshToken")]
    public void Sensitive_token_match_is_case_insensitive(string fieldName)
    {
        var redacted = _sut.Redact(fieldName, "abc.def.ghi");

        redacted.Should().Be("***");
    }

    [Fact]
    public void Null_value_returns_null_regardless_of_sensitivity()
    {
        _sut.Redact("Password", null).Should().BeNull();
        _sut.Redact("Email", null).Should().BeNull();
    }
}
