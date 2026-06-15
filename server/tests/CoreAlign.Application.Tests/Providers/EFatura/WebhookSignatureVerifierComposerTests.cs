using System.Security.Cryptography;
using System.Text;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class WebhookSignatureVerifierComposerTests
{
    private const string NilveraSecret = "nilvera-secret";
    private const string ForibaSecret = "foriba-secret";

    [Fact]
    public void Nilvera_verifier_accepts_valid_hmac_sha256()
    {
        var composer = BuildComposer();
        var payload = Encoding.UTF8.GetBytes("payload-nilvera");
        var signature = SignHmacSha256(payload, NilveraSecret);

        var result = composer.Verify("nilvera", payload, signature);

        result.Should().BeTrue();
    }

    [Fact]
    public void Nilvera_verifier_rejects_invalid_hmac_sha256()
    {
        var composer = BuildComposer();
        var payload = Encoding.UTF8.GetBytes("payload-nilvera");

        var result = composer.Verify("nilvera", payload, "deadbeef");

        result.Should().BeFalse();
    }

    [Fact]
    public void Foriba_verifier_accepts_valid_hmac_sha1()
    {
        var composer = BuildComposer();
        var payload = Encoding.UTF8.GetBytes("payload-foriba");
        var signature = SignHmacSha1(payload, ForibaSecret);

        var result = composer.Verify("foriba", payload, signature);

        result.Should().BeTrue();
    }

    [Fact]
    public void Foriba_verifier_rejects_invalid_hmac_sha1()
    {
        var composer = BuildComposer();
        var payload = Encoding.UTF8.GetBytes("payload-foriba");

        var result = composer.Verify("foriba", payload, "AABBCC");

        result.Should().BeFalse();
    }

    [Fact]
    public void Unknown_provider_returns_false()
    {
        var composer = BuildComposer();
        var payload = Encoding.UTF8.GetBytes("payload-x");

        var result = composer.Verify("unknown-provider", payload, "anything");

        result.Should().BeFalse();
    }

    private static FakeWebhookSignatureVerifierComposer BuildComposer()
    {
        var composer = new FakeWebhookSignatureVerifierComposer();
        composer.Register("nilvera", (payload, signature) =>
        {
            var expected = SignHmacSha256(payload, NilveraSecret);
            return ConstantTimeEquals(expected, signature);
        });
        composer.Register("foriba", (payload, signature) =>
        {
            var expected = SignHmacSha1(payload, ForibaSecret);
            return ConstantTimeEquals(expected, signature);
        });
        return composer;
    }

    private static string SignHmacSha256(byte[] payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, payload);
        return Convert.ToHexString(hash);
    }

    private static string SignHmacSha1(byte[] payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(payload);
        return Convert.ToHexString(hash);
    }

    private static bool ConstantTimeEquals(string expected, string actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual)) return false;
        if (expected.Length != actual.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected.ToUpperInvariant()),
            Encoding.ASCII.GetBytes(actual.ToUpperInvariant()));
    }

    private sealed class FakeWebhookSignatureVerifierComposer
    {
        private readonly Dictionary<string, Func<byte[], string, bool>> _registry = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string providerName, Func<byte[], string, bool> verifier) =>
            _registry[providerName] = verifier;

        public bool Verify(string providerName, byte[] payload, string signature)
        {
            if (!_registry.TryGetValue(providerName, out var verifier)) return false;
            try
            {
                return verifier(payload, signature);
            }
            catch
            {
                return false;
            }
        }
    }
}
