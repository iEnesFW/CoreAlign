using System.Text.Json;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Infrastructure.Providers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Providers;

public class DataProtectionCredentialProtectorTests
{
    private sealed record SampleCreds(string ApiKey, string ApiSecret);

    private static DataProtectionCredentialProtector BuildSut(IDataProtectionProvider? provider = null) =>
        new(
            provider ?? new EphemeralDataProtectionProvider(),
            NullLogger<DataProtectionCredentialProtector>.Instance);

    [Fact]
    public void Protect_and_UnprotectAs_roundtrip_returns_original_record()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var original = new SampleCreds("k-123", "s-456");
        var plaintext = JsonSerializer.Serialize(original);

        var encrypted = sut.Protect(tenantId, ProviderCategory.EFatura, plaintext);
        var decrypted = sut.UnprotectAs<SampleCreds>(tenantId, ProviderCategory.EFatura, encrypted);

        encrypted.Should().NotBe(plaintext);
        decrypted.Should().NotBeNull();
        decrypted!.ApiKey.Should().Be("k-123");
        decrypted.ApiSecret.Should().Be("s-456");
    }

    [Fact]
    public void TryUnprotect_returns_false_when_payload_tampered()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var encrypted = sut.Protect(tenantId, ProviderCategory.EFatura, "{\"value\":1}");
        var tampered = encrypted + "XYZ";

        var ok = sut.TryUnprotect(tenantId, ProviderCategory.EFatura, tampered, out var plaintext);

        ok.Should().BeFalse();
        plaintext.Should().BeNull();
    }

    [Fact]
    public void Different_tenants_cannot_decrypt_each_others_credentials()
    {
        var sut = BuildSut();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var encryptedForA = sut.Protect(tenantA, ProviderCategory.EFatura, "{\"secret\":\"A\"}");

        var ok = sut.TryUnprotect(tenantB, ProviderCategory.EFatura, encryptedForA, out var plaintext);

        ok.Should().BeFalse();
        plaintext.Should().BeNull();
    }

    [Fact]
    public void Different_categories_use_isolated_purposes()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var encryptedForEFatura = sut.Protect(tenantId, ProviderCategory.EFatura, "{\"v\":1}");

        var ok = sut.TryUnprotect(tenantId, ProviderCategory.Payment, encryptedForEFatura, out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void UnprotectAs_returns_null_when_encrypted_is_null_or_whitespace()
    {
        var sut = BuildSut();

        sut.UnprotectAs<SampleCreds>(Guid.NewGuid(), ProviderCategory.EFatura, null).Should().BeNull();
        sut.UnprotectAs<SampleCreds>(Guid.NewGuid(), ProviderCategory.EFatura, "  ").Should().BeNull();
    }

    [Fact]
    public void UnprotectAs_throws_when_plaintext_is_malformed_json()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var encrypted = sut.Protect(tenantId, ProviderCategory.EFatura, "not-json");

        var act = () => sut.UnprotectAs<SampleCreds>(tenantId, ProviderCategory.EFatura, encrypted);

        act.Should().Throw<ProviderCredentialDecryptionException>();
    }
}
