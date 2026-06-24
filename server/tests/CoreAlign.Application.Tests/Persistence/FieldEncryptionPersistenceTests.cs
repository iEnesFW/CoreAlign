using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Persistence;

public class FieldEncryptionPersistenceTests
{
    private const string Secret = "JBSWY3DPEHPK3PXP";

    private static IDataProtectionProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("field-encryption-persistence-test");
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    private static ITenantContext Tenant(Guid tenantId)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);
        return tenant;
    }

    private static CoreAlignDbContext Context(SqliteConnection conn, Guid tenantId, IDataProtectionProvider? provider)
    {
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options;
        return new CoreAlignDbContext(options, Tenant(tenantId), Substitute.For<IPublisher>(), provider!);
    }

    [Fact]
    public async Task TwoFactorSecret_is_stored_encrypted_and_decrypts_on_read()
    {
        var tenantId = Guid.NewGuid();
        var provider = BuildProvider();
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        try
        {
            await using (var seed = Context(conn, tenantId, provider))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Tenants.Add(new Tenant("T", "t") { Id = tenantId });
                var user = new User(tenantId, "enc-user", "enc@example.com", "hash") { TwoFactorSecretKey = Secret };
                seed.Users.Add(user);
                await seed.SaveChangesAsync();
            }

            // Raw column read WITHOUT the converter (no protector) — proves ciphertext at rest.
            await using (var raw = Context(conn, tenantId, provider: null))
            {
                var stored = await raw.Users.AsNoTracking().Select(u => u.TwoFactorSecretKey).SingleAsync();
                stored.Should().NotBeNull();
                stored.Should().NotBe(Secret);
                stored!.Length.Should().BeGreaterThan(Secret.Length);
            }

            // Read WITH the converter — decrypts back to the original secret.
            await using (var read = Context(conn, tenantId, provider))
            {
                var roundtripped = await read.Users.AsNoTracking().Select(u => u.TwoFactorSecretKey).SingleAsync();
                roundtripped.Should().Be(Secret);
            }
        }
        finally
        {
            conn.Close();
        }
    }

    [Fact]
    public async Task Legacy_plaintext_secret_still_reads_after_converter_is_enabled()
    {
        var tenantId = Guid.NewGuid();
        var provider = BuildProvider();
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        try
        {
            // Seed a row the OLD way (no converter) so the column holds raw plaintext,
            // simulating a user enrolled before encryption was introduced.
            await using (var legacy = Context(conn, tenantId, provider: null))
            {
                await legacy.Database.EnsureCreatedAsync();
                legacy.Tenants.Add(new Tenant("T", "t") { Id = tenantId });
                legacy.Users.Add(new User(tenantId, "legacy", "legacy@example.com", "hash") { TwoFactorSecretKey = Secret });
                await legacy.SaveChangesAsync();
            }

            // Reading through the resilient converter must NOT throw — it passes the
            // legacy plaintext through unchanged.
            await using (var read = Context(conn, tenantId, provider))
            {
                var value = await read.Users.AsNoTracking().Select(u => u.TwoFactorSecretKey).SingleAsync();
                value.Should().Be(Secret);
            }
        }
        finally
        {
            conn.Close();
        }
    }
}
