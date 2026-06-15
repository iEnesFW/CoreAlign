using CoreAlign.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Persistence;

public class EncryptedStringConverterTests
{
    private static IDataProtector BuildProtector()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("test");
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test.purpose");
    }

    [Fact]
    public void Roundtrip_returns_original_value()
    {
        var protector = BuildProtector();
        var converter = new EncryptedStringConverter(protector);

        var ciphertext = (string?)converter.ConvertToProvider("99988877766");
        var roundtripped = (string?)converter.ConvertFromProvider(ciphertext);

        roundtripped.Should().Be("99988877766");
    }

    [Fact]
    public void Null_passes_through_unchanged()
    {
        var protector = BuildProtector();
        var converter = new EncryptedStringConverter(protector);

        var ciphertext = (string?)converter.ConvertToProvider(null);
        ciphertext.Should().BeNull();

        var roundtripped = (string?)converter.ConvertFromProvider(null);
        roundtripped.Should().BeNull();
    }

    [Fact]
    public void Ciphertext_differs_from_plaintext()
    {
        var protector = BuildProtector();
        var converter = new EncryptedStringConverter(protector);

        var plaintext = "TR330006100519786457841326";
        var ciphertext = (string?)converter.ConvertToProvider(plaintext);

        ciphertext.Should().NotBeNull();
        ciphertext.Should().NotBe(plaintext);
        ciphertext!.Length.Should().BeGreaterThan(plaintext.Length);
    }

    [Fact]
    public void Required_converter_roundtrips_non_null_strings()
    {
        var protector = BuildProtector();
        var converter = new RequiredEncryptedStringConverter(protector);

        var ciphertext = (string)converter.ConvertToProvider("plaintext-value")!;
        var roundtripped = (string)converter.ConvertFromProvider(ciphertext)!;

        roundtripped.Should().Be("plaintext-value");
    }
}
