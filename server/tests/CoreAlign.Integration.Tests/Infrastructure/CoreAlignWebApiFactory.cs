using System.Collections.Generic;
using CoreAlign.API.HostedServices;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CoreAlign.Integration.Tests.Infrastructure;

internal sealed class DbCommandRoundTripInterceptorConfigurer : IDbContextOptionsConfiguration<CoreAlignDbContext>
{
    private readonly DbCommandRoundTripInterceptor _interceptor;

    public DbCommandRoundTripInterceptorConfigurer(DbCommandRoundTripInterceptor interceptor)
    {
        _interceptor = interceptor;
    }

    public void Configure(IServiceProvider serviceProvider, DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_interceptor);
    }
}

public class CoreAlignWebApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly string _connectionString;

    public TenantFixture TenantA { get; private set; } = null!;
    public TenantFixture TenantB { get; private set; } = null!;

    public CoreAlignWebApiFactory()
    {
        _connectionString = $"Data Source=file:integration-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);
        Environment.SetEnvironmentVariable("Hangfire__UseMemoryStorage", "true");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "Integration-Tests-CoreAlign-Secret-Key-For-Wapf-Only-Never-Used-In-Prod-2026!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CoreAlign.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CoreAlign.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost");
        Environment.SetEnvironmentVariable("Frontend__AutoLaunch", "false");
        Environment.SetEnvironmentVariable("Email__Provider", "LogOnly");
        Environment.SetEnvironmentVariable("EInvoice__Provider", "Stub");
        Environment.SetEnvironmentVariable("EInvoice__BaseUrl", "https://stub.local");
        Environment.SetEnvironmentVariable("EInvoice__Username", "stub");
        Environment.SetEnvironmentVariable("EInvoice__Password", "stub");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(BuildOverrides());
        });
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(BuildOverrides());
        });

        builder.ConfigureServices(services =>
        {
            RemoveHostedService<DemoDataSeeder>(services);
            RemoveHostedService<FrontendDevServerLauncher>(services);

            services.AddSingleton<DbCommandRoundTripInterceptor>();
            services.AddSingleton<IDbContextOptionsConfiguration<CoreAlignDbContext>, DbCommandRoundTripInterceptorConfigurer>();

            services
                .AddAuthentication(TestAuthenticationOptions.SchemeName)
                .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                    TestAuthenticationOptions.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                opts.DefaultAuthenticateScheme = TestAuthenticationOptions.SchemeName;
                opts.DefaultChallengeScheme = TestAuthenticationOptions.SchemeName;
                opts.DefaultScheme = TestAuthenticationOptions.SchemeName;
            });

            services.PostConfigureAll<AuthorizationOptions>(opts =>
            {
                opts.DefaultPolicy = new AuthorizationPolicyBuilder(TestAuthenticationOptions.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();
                opts.FallbackPolicy = null;
            });
        });
    }

    private Dictionary<string, string?> BuildOverrides() => new()
    {
        ["Database:Provider"] = "Sqlite",
        ["ConnectionStrings:DefaultConnection"] = _connectionString,
        ["Hangfire:UseMemoryStorage"] = "true",
        ["Jwt:SecretKey"] = "Integration-Tests-CoreAlign-Secret-Key-For-Wapf-Only-Never-Used-In-Prod-2026!",
        ["Jwt:Issuer"] = "CoreAlign.IntegrationTests",
        ["Jwt:Audience"] = "CoreAlign.IntegrationTests",
        ["Jwt:AccessTokenExpirationMinutes"] = "60",
        ["Cors:AllowedOrigins:0"] = "http://localhost",
        ["Frontend:AutoLaunch"] = "false",
        ["Email:Provider"] = "LogOnly",
        ["EInvoice:Provider"] = "Stub",
        ["EInvoice:BaseUrl"] = "https://stub.local",
        ["EInvoice:Username"] = "stub",
        ["EInvoice:Password"] = "stub",
    };

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        await db.Database.EnsureCreatedAsync();

        var seeder = new IntegrationSeed(Services);
        TenantA = await seeder.SeedTenantAsync("Tenant-A", "tenant-a");
        TenantB = await seeder.SeedTenantAsync("Tenant-B", "tenant-b");
    }

    public new Task DisposeAsync()
    {
        _connection.Close();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    private static void RemoveHostedService<T>(IServiceCollection services) where T : IHostedService
    {
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}
