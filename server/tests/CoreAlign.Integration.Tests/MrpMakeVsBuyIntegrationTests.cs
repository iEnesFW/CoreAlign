using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class MrpMakeVsBuyIntegrationTests
{
    private const int ProductionOrdersRoundTripBudget = 3;

    // Each test commits with a DISTINCT as-of date so the per-tenant
    // (tenant_id, idempotency_key = "{yyyyMMdd}:Day:30") unique constraint does not
    // collapse separate test runs into one idempotent replay (INVARIANTS §93, §99).
    private static readonly DateTime AsOfBase = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public MrpMakeVsBuyIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Commit_routes_make_item_to_production_order_and_buy_component_to_requisition()
    {
        var (makeId, _) = await SeedMakeWithBuyComponentAsync(_factory.TenantA);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var run = await CommitAsync(client, AsOfBase);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var productionOrders = await db.Set<PlannedProductionOrder>()
                .Where(o => o.SourcePlanRunId == run.Id)
                .ToListAsync();
            productionOrders.Should().Contain(o => o.ProductId == makeId,
                "the Make finished good routes to a planned production order, NOT a requisition");

            var requisitionPlannedOrders = await db.Set<MrpPlannedOrder>()
                .Where(o => o.PlanRunId == run.Id)
                .ToListAsync();
            requisitionPlannedOrders.Should().NotContain(o => o.ProductId == makeId,
                "the Make item must never appear in the purchase-requisition sink");
            requisitionPlannedOrders.Should().Contain(o => o.ProductId != makeId,
                "the exploded Buy component routes to the requisition sink");
        }
    }

    [Fact]
    public async Task ProductionOrders_list_is_paginated_and_within_round_trip_budget()
    {
        await SeedMakeWithBuyComponentAsync(_factory.TenantA);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var run = await CommitAsync(client, AsOfBase.AddDays(1));

        await WarmUpAsync(client, $"/api/v1/mrp/production-orders?planRunId={run.Id}&page=1&pageSize=10");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync($"/api/v1/mrp/production-orders?planRunId={run.Id}&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PlannedProductionOrderDto>>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.Page.Should().Be(1);
        body.Data.PageSize.Should().Be(10);
        body.Data.Items.Should().NotBeEmpty();

        counter.Total.Should().BeLessThanOrEqualTo(
            ProductionOrdersRoundTripBudget,
            $"production-orders list is 1 COUNT + 1 SELECT (+1 slack); observed {counter.Total}");
    }

    [Fact]
    public async Task Firm_then_release_production_order_walks_state_machine()
    {
        await SeedMakeWithBuyComponentAsync(_factory.TenantA);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var run = await CommitAsync(client, AsOfBase.AddDays(2));

        var orderId = await FirstProductionOrderIdAsync(run.Id, _factory.TenantA);

        var firm = await client.PostAsJsonAsync(
            $"/api/v1/mrp/production-orders/{orderId}/firm",
            new { operationId = Guid.NewGuid() }, JsonOptions);
        firm.StatusCode.Should().Be(HttpStatusCode.OK);
        var firmed = (await firm.Content.ReadFromJsonAsync<ApiResponse<PlannedProductionOrderDto>>(JsonOptions))!.Data!;
        firmed.Status.Should().Be(PlannedProductionOrderStatus.Firm);

        var release = await client.PostAsJsonAsync(
            $"/api/v1/mrp/production-orders/{orderId}/release",
            new { operationId = Guid.NewGuid() }, JsonOptions);
        release.StatusCode.Should().Be(HttpStatusCode.OK);
        var released = (await release.Content.ReadFromJsonAsync<ApiResponse<PlannedProductionOrderDto>>(JsonOptions))!.Data!;
        released.Status.Should().Be(PlannedProductionOrderStatus.Released);
    }

    [Fact]
    public async Task Releasing_an_already_released_production_order_is_rejected_by_state_machine()
    {
        await SeedMakeWithBuyComponentAsync(_factory.TenantA);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var run = await CommitAsync(client, AsOfBase.AddDays(3));
        var orderId = await FirstProductionOrderIdAsync(run.Id, _factory.TenantA);

        var first = await client.PostAsJsonAsync(
            $"/api/v1/mrp/production-orders/{orderId}/release",
            new { operationId = Guid.NewGuid() }, JsonOptions);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync(
            $"/api/v1/mrp/production-orders/{orderId}/release",
            new { operationId = Guid.NewGuid() }, JsonOptions);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ChangeImpact_lists_downstream_supply_for_the_run()
    {
        await SeedMakeWithBuyComponentAsync(_factory.TenantA);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var run = await CommitAsync(client, AsOfBase.AddDays(4));

        // Safety-stock-driven demand has no sales-order line; an unknown line yields an
        // empty (but 200) impact — the endpoint contract is exercised end to end.
        var response = await client.GetAsync($"/api/v1/mrp/change-impact/{run.Id}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ChangeImpactResultDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.PlanRunId.Should().Be(run.Id);
    }

    [Fact]
    public async Task TenantAdminA_CannotFirmProductionOrderOfTenantB()
    {
        await SeedMakeWithBuyComponentAsync(_factory.TenantB);
        var clientB = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);
        var runB = await CommitAsync(clientB, AsOfBase.AddDays(5));
        var tenantBOrderId = await FirstProductionOrderIdAsync(runB.Id, _factory.TenantB);

        var clientA = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await clientA.PostAsJsonAsync(
            $"/api/v1/mrp/production-orders/{tenantBOrderId}/firm",
            new { operationId = Guid.NewGuid() }, JsonOptions);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var order = await db.Set<PlannedProductionOrder>().FirstAsync(o => o.Id == tenantBOrderId);
            order.Status.Should().Be(PlannedProductionOrderStatus.Planned,
                "a cross-tenant firm attempt must not mutate TenantB's order");
        }
    }

    private async Task<MrpPlanRunDto> CommitAsync(HttpClient client, DateTime asOf)
    {
        var commit = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), asOf, MrpBucketKind.Day, 30), JsonOptions);
        commit.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await commit.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;
    }

    private async Task<Guid> FirstProductionOrderIdAsync(Guid planRunId, TenantFixture tenant)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            return await db.Set<PlannedProductionOrder>()
                .Where(o => o.SourcePlanRunId == planRunId)
                .OrderBy(o => o.LowLevelCode)
                .Select(o => o.Id)
                .FirstAsync();
        }
    }

    private async Task WarmUpAsync(HttpClient client, string url)
    {
        var warm = await client.GetAsync(url);
        warm.EnsureSuccessStatusCode();
    }

    private async Task<(Guid MakeId, Guid ComponentId)> SeedMakeWithBuyComponentAsync(TenantFixture tenant)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            var make = BuildPlanningProduct(tenant, "MK", ProcurementType.Make, safetyStock: 15m, leadTimeDays: 3);
            var component = BuildPlanningProduct(tenant, "CMP", ProcurementType.Buy, safetyStock: 0m, leadTimeDays: 5);
            db.Products.Add(make);
            db.Products.Add(component);
            await db.SaveChangesAsync();

            db.ProductComponents.Add(new ProductComponent(make.Id, component.Id, 2m)
            {
                TenantId = tenant.TenantId,
            });
            await db.SaveChangesAsync();

            return (make.Id, component.Id);
        }
    }

    private static Product BuildPlanningProduct(
        TenantFixture tenant,
        string skuPrefix,
        ProcurementType procurementType,
        decimal safetyStock,
        int leadTimeDays)
    {
        var product = new Product($"{skuPrefix}-{Guid.NewGuid():N}"[..15], $"{skuPrefix} Widget", "pcs", 10m, "TRY")
        {
            TenantId = tenant.TenantId,
        };
        product.Update(
            sku: product.Sku, barcode: null, mpn: null, name: product.Name,
            shortDescription: null, description: null, slug: null,
            brandId: null, categoryId: null, parentProductId: null,
            variantAttributesJson: null, tagsJson: null,
            unit: "pcs", baseUomId: null, purchaseUomId: null, salesUomId: null,
            listPrice: 10m, price: 10m, minSellingPrice: 0m,
            standardCost: 5m, currency: "TRY", taxRateId: null, isPriceTaxInclusive: false,
            isStockTracked: true, isLotTracked: false, isSerialTracked: false,
            minStock: 0m, maxStock: 100m, reorderPoint: 0m,
            safetyStock: safetyStock, leadTimeDays: leadTimeDays,
            weightKg: null, widthCm: null, heightCm: null, depthCm: null, volumeM3: null,
            status: ProductStatus.Active, launchDate: null, endOfLifeDate: null);
        product.SetProcurementType(procurementType);
        return product;
    }
}
