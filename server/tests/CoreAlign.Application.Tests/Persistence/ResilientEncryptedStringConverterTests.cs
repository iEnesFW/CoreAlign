using System.Security.Cryptography;
using CoreAlign.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Persistence;

public class ResilientEncryptedStringConverterTests
{
    private static IDataProtector BuildProtector()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("resilient-test");
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("resilient.purpose");
    }

    [Fact]
    public void Roundtrip_returns_original_value()
    {
        var protector = BuildProtector();
        var converter = new ResilientEncryptedStringConverter(protector);

        var ciphertext = (string?)converter.ConvertToProvider("99988877766");
        var roundtripped = (string?)converter.ConvertFromProvider(ciphertext);

        ciphertext.Should().NotBe("99988877766");
        roundtripped.Should().Be("99988877766");
    }

    [Fact]
    public void Null_passes_through_unchanged()
    {
        var protector = BuildProtector();
        var converter = new ResilientEncryptedStringConverter(protector);

        ((string?)converter.ConvertToProvider(null)).Should().BeNull();
        ((string?)converter.ConvertFromProvider(null)).Should().BeNull();
    }

    [Fact]
    public void Legacy_plaintext_passes_through_on_read_instead_of_throwing()
    {
        var protector = BuildProtector();
        var resilient = new ResilientEncryptedStringConverter(protector);
        var strict = new EncryptedStringConverter(protector);

        const string legacyPlaintext = "TR330006100519786457841326";

        // RED baseline: the strict converter throws on a value that is not valid ciphertext.
        var strictRead = () => strict.ConvertFromProvider(legacyPlaintext);
        strictRead.Should().Throw<CryptographicException>();

        // GREEN: the resilient converter returns the legacy plaintext unchanged (no throw),
        // so already-stored rows keep working after the converter is wired in.
        var resilientRead = (string?)resilient.ConvertFromProvider(legacyPlaintext);
        resilientRead.Should().Be(legacyPlaintext);
    }

    [Fact]
    public void Required_resilient_converter_roundtrips_and_passes_through_legacy_plaintext()
    {
        var protector = BuildProtector();
        var converter = new RequiredResilientEncryptedStringConverter(protector);

        var ciphertext = (string)converter.ConvertToProvider("12345678901")!;
        ciphertext.Should().NotBe("12345678901");
        ((string)converter.ConvertFromProvider(ciphertext)!).Should().Be("12345678901");

        // Legacy plaintext stored before encryption was enabled reads back unchanged.
        ((string)converter.ConvertFromProvider("12345678901")!).Should().Be("12345678901");
    }
}
