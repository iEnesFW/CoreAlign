using System.Net;
using System.Net.Http.Json;
using CoreAlign.Application.Common;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class TwoFactorEndpointsIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public TwoFactorEndpointsIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enroll_requires_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/v1/Auth/2fa/enroll", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StepUp_requires_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/Auth/2fa/step-up", new { code = "000000" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Enroll_then_verify_enables_two_factor_for_the_jwt_user()
    {
        // The endpoints derive the user id from the JWT (no body user id to forge),
        // so this also proves the IDOR-safe wiring operates on the caller's own account.
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);

        var enrollResponse = await client.PostAsync("/api/v1/Auth/2fa/enroll", content: null);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var enrollment = await enrollResponse.Content.ReadFromJsonAsync<ApiResponse<TwoFactorEnrollmentDto>>();
        enrollment.Should().NotBeNull();
        enrollment!.Data.Should().NotBeNull();
        enrollment.Data!.ManualKey.Should().NotBeNullOrWhiteSpace();

        var code = new Totp(Base32Encoding.ToBytes(enrollment.Data.ManualKey), step: 30, mode: OtpHashMode.Sha1, totpSize: 6)
            .ComputeTotp();

        var verifyResponse = await client.PostAsJsonAsync("/api/v1/Auth/2fa/verify", new { code });
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var backupCodes = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<TwoFactorBackupCodesDto>>();
        backupCodes!.Data!.BackupCodes.Should().NotBeEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == _factory.TenantB.TenantAdminUserId);
        user.IsTwoFactorEnabled.Should().BeTrue();
        // Secret is encrypted at rest (converter active) yet decrypts on read so verify worked.
        user.TwoFactorSecretKey.Should().Be(enrollment.Data.ManualKey);
    }
}
