using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.API.HostedServices;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class ModulePurchaseIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public ModulePurchaseIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid ModuleId, Guid PlanId)> EnsureSellableModuleAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        var module = new Module($"TEST{suffix}", $"Test Module {suffix}", "Integration fixture", "Test", "box", 900, isActive: true, isCore: false);
        db.Set<Module>().Add(module);
        var plan = new ModulePricePlan(module.Id, "Monthly", "Aylık", 30, 149m, "TRY", isActive: true, sortOrder: 0);
        db.Set<ModulePricePlan>().Add(plan);
        await db.SaveChangesAsync();
        return (module.Id, plan.Id);
    }

    /// <summary>
    /// RED-BEFORE: with EnsureExists and Consume back to back (no save between), the very first
    /// purchase on a tenant whose SubscriptionOrderNumber sequence was never seeded threw and the
    /// endpoint answered 500. Every unit test stayed green because they substitute the sequence
    /// repository — this is the MRP-BUG-1 shape recorded in INVARIANTS.
    /// </summary>
    [Fact]
    public async Task A_first_purchase_on_a_tenant_without_a_seeded_sequence_succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var (moduleId, planId) = await EnsureSellableModuleAsync(suffix);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            var existing = await db.Set<DocumentSequence>()
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == _factory.TenantA.TenantId
                            && s.Type == DocumentSequenceType.SubscriptionOrderNumber)
                .ToListAsync();
            db.Set<DocumentSequence>().RemoveRange(existing);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.PostAsJsonAsync("/api/v1/billing/orders", new
        {
            gatewayName = "mock",
            items = new[] { new { moduleId, planId } },
        });

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            await response.Content.ReadAsStringAsync());

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("SUB-", "the order number comes from the document sequence the fix now seeds in time");
    }

    [Fact]
    public async Task The_mock_gateway_is_offered_so_the_pipeline_can_run_without_a_merchant_account()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/billing/gateways");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("mock", "the registry resolved an empty gateway list before the DI fix");
    }

    /// <summary>
    /// The catalog used to be seeded only from DemoDataSeeder, which is hard-off in Production —
    /// so the store rendered a legitimate-looking empty state that nobody could buy from. This runs
    /// the always-on seeder directly (the fixture removes DemoDataSeeder) and proves it is
    /// idempotent, which is what lets it run on every boot.
    /// </summary>
    [Fact]
    public async Task The_module_catalog_seeder_stands_alone_and_is_idempotent()
    {
        int afterFirst;
        int afterSecond;
        using (var scope = _factory.Services.CreateScope())
        {
            await ModuleCatalogSeed.SeedAsync(scope.ServiceProvider, CancellationToken.None);
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            afterFirst = await db.Set<Module>().CountAsync();
        }
        using (var scope = _factory.Services.CreateScope())
        {
            await ModuleCatalogSeed.SeedAsync(scope.ServiceProvider, CancellationToken.None);
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            afterSecond = await db.Set<Module>().CountAsync();
        }

        afterFirst.Should().BeGreaterThan(0);
        afterSecond.Should().Be(afterFirst);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync("/api/v1/modules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task A_grant_that_starts_in_the_future_is_not_active_yet()
    {
        var now = DateTime.UtcNow;
        var future = new TenantModule(Guid.NewGuid(), now.AddDays(10), now.AddDays(40), TenantModuleSource.Granted);
        var current = new TenantModule(Guid.NewGuid(), now.AddDays(-1), now.AddDays(30), TenantModuleSource.Paid);

        future.IsCurrentlyActive.Should().BeFalse("a grant scheduled for later must not unlock the module today");
        current.IsCurrentlyActive.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Purchasing_is_refused_without_authentication()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var (moduleId, planId) = await EnsureSellableModuleAsync(suffix);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/billing/orders", new
        {
            gatewayName = "mock",
            items = new[] { new { moduleId, planId } },
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
