using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class MrpPlanningControllerIntegrationTests
{
    private const int PreviewRoundTripBudget = 9;
    private const string IsolatedPreviewAsOf = "2020-01-01";

    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public MrpPlanningControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preview_returns_200_with_grid_payload()
    {
        var productId = await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync($"/api/v1/mrp/plan/preview?asOf={IsolatedPreviewAsOf}&bucket=Day&horizon=30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MrpPlanResultDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        var plan = body.Data!;
        plan.HorizonDays.Should().Be(30);
        plan.Items.Should().NotBeNull();

        // Enriched contract the workbench consumes (the reconciled DTO): a preview carries
        // no run id, a Preview status, and the computed summary counts; item rows carry the
        // planning master fields (reorderPoint/leadTimeDays/reserved) the grid + chart read.
        plan.PlanRunId.Should().BeNull("a preview is not a committed run");
        plan.Status.Should().Be(MrpPlanRunStatus.Preview);
        plan.PlannedOrderCount.Should().Be(plan.BuyOrderCount + plan.MakeOrderCount);
        plan.BuyOrderCount.Should().BeGreaterThan(0, "the seeded product is below safety stock");

        var seeded = plan.Items.Single(i => i.ProductId == productId);
        seeded.ReorderPoint.Should().Be(50m);
        seeded.LeadTimeDays.Should().Be(5);
        seeded.ProcurementType.Should().Be(ProcurementType.Buy);
        seeded.PlannedOrders.Should().NotBeEmpty();
        seeded.PlannedOrders[0].ProcurementType.Should().Be(ProcurementType.Buy);
    }

    [Fact]
    public async Task Commit_on_fresh_tenant_returns_200_not_500()
    {
        // MRP-BUG-1 regression: the auto-generate path consumed a sequence with no
        // intervening SaveChanges, 500-ing on a fresh tenant. CommitAsync follows the
        // safe EnsureExists -> SaveChanges -> Consume ordering.
        await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var cmd = new CommitMrpPlanCommand(Guid.NewGuid(), AsOfDateUtc: DateTime.UtcNow, BucketKind: MrpBucketKind.Day, HorizonDays: 30);
        var response = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit", cmd, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.Number.Should().NotBeNullOrEmpty();
        body.Data.Status.Should().Be(MrpPlanRunStatus.Committed);
    }

    [Fact]
    public async Task Commit_twice_same_key_is_idempotent_and_persists_one_run()
    {
        // MRP-BUG-2 regression: re-running the same plan must not duplicate.
        await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var asOf = DateTime.UtcNow;
        var first = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), asOf, MrpBucketKind.Day, 30), JsonOptions);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstRun = (await first.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;

        var second = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), asOf, MrpBucketKind.Day, 30), JsonOptions);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondRun = (await second.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;

        secondRun.Id.Should().Be(firstRun.Id);
        secondRun.IdempotencyKey.Should().Be(firstRun.IdempotencyKey);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var count = await db.Set<MrpPlanRun>()
                .CountAsync(r => r.IdempotencyKey == firstRun.IdempotencyKey);
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task Commit_then_release_creates_requisition_and_marks_planned_orders_released()
    {
        await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var commit = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), DateTime.UtcNow, MrpBucketKind.Day, 30), JsonOptions);
        commit.StatusCode.Should().Be(HttpStatusCode.OK);
        var run = (await commit.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;

        if (run.PlannedOrderCount == 0)
        {
            return;
        }

        Guid[] plannedOrderIds;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
            {
                plannedOrderIds = await db.Set<MrpPlannedOrder>()
                    .Where(o => o.PlanRunId == run.Id)
                    .Select(o => o.Id)
                    .ToArrayAsync();
            }
        }

        var release = await client.PostAsJsonAsync(
            $"/api/v1/mrp/plan/{run.Id}/release",
            new { plannedOrderIds, operationId = Guid.NewGuid() });
        release.StatusCode.Should().Be(HttpStatusCode.OK);
        var releaseResult = (await release.Content.ReadFromJsonAsync<ApiResponse<ReleasePlannedOrdersResultDto>>(JsonOptions))!.Data!;
        releaseResult.PlannedOrdersReleased.Should().BeGreaterThan(0);
        releaseResult.RequisitionIds.Should().NotBeEmpty();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
            {
                var released = await db.Set<MrpPlannedOrder>()
                    .Where(o => o.PlanRunId == run.Id && o.IsReleased)
                    .CountAsync();
                released.Should().BeGreaterThan(0);

                var reqCount = await db.PurchaseRequisitions
                    .CountAsync(r => releaseResult.RequisitionIds.Contains(r.Id));
                reqCount.Should().Be(releaseResult.RequisitionIds.Count);
            }
        }
    }

    [Fact]
    public async Task Firmed_planned_order_is_honored_as_supply_and_not_duplicated_on_replan()
    {
        // T3: a firmed planned order must survive the next regeneration as fixed supply,
        // so re-planning does not spam a duplicate requisition for coverage already firmed.
        // A far-future, test-unique as-of avoids idempotency collisions on the shared DB;
        // assertions are scoped to the seeded product to stay contamination-safe.
        var productId = await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var asOf = DateTime.UtcNow.AddDays(200);
        var firstCommit = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), asOf, MrpBucketKind.Day, 30), JsonOptions);
        firstCommit.StatusCode.Should().Be(HttpStatusCode.OK);
        var run1 = (await firstCommit.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;

        Guid plannedOrderId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
            {
                plannedOrderId = await db.Set<MrpPlannedOrder>()
                    .Where(o => o.PlanRunId == run1.Id && o.ProductId == productId)
                    .Select(o => o.Id)
                    .FirstAsync();
            }
        }

        var firm = await client.PostAsJsonAsync(
            $"/api/v1/mrp/planned-orders/{plannedOrderId}/firm",
            new { operationId = Guid.NewGuid() });
        firm.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondCommit = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), asOf.AddDays(1), MrpBucketKind.Day, 30), JsonOptions);
        secondCommit.StatusCode.Should().Be(HttpStatusCode.OK);
        var run2 = (await secondCommit.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;
        run2.Id.Should().NotBe(run1.Id, "a different as-of day yields a distinct run");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
            {
                var run2Orders = await db.Set<MrpPlannedOrder>()
                    .Where(o => o.PlanRunId == run2.Id && o.ProductId == productId)
                    .ToListAsync();
                run2Orders.Should().ContainSingle(
                    "the firmed order is carried forward exactly once — not dropped, not duplicated");
                run2Orders[0].IsFirmed.Should().BeTrue("the carried-forward order keeps its firmed decision");
            }
        }
    }

    [Fact]
    public async Task Firmed_order_carries_forward_without_accumulating_across_repeated_replans()
    {
        // T3 review finding #4 regression: firmed supply must be scoped to the latest run and
        // carried forward, so re-planning N times never accumulates N copies as live supply.
        // Runs on TenantB with a far-future as-of so its run sequence is the tenant's latest
        // throughout — isolated from the TenantA firm-survival test on the shared DB (the
        // "latest committed run" carry-forward reads is tenant-scoped).
        var productId = await SeedReorderProductAsync(_factory.TenantB);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);

        var asOf = DateTime.UtcNow.AddDays(900);
        var run1 = await CommitAsync(client, asOf, 30);
        await FirmFirstOrderForProductAsync(client, run1, productId, _factory.TenantB.TenantId);

        var run2 = await CommitAsync(client, asOf.AddDays(1), 30);
        var run3 = await CommitAsync(client, asOf.AddDays(2), 30);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var latestOrders = await db.Set<MrpPlannedOrder>()
                .Where(o => o.PlanRunId == run3.Id && o.ProductId == productId)
                .ToListAsync();
            latestOrders.Should().ContainSingle(
                "after three re-plans the latest run still holds exactly one firmed order — no accumulation");
            latestOrders[0].IsFirmed.Should().BeTrue();
        }
    }

    private async Task<MrpPlanRunDto> CommitAsync(HttpClient client, DateTime asOf, int horizon)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), asOf, MrpBucketKind.Day, horizon), JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resp.Content.ReadFromJsonAsync<ApiResponse<MrpPlanRunDto>>(JsonOptions))!.Data!;
    }

    private async Task FirmFirstOrderForProductAsync(HttpClient client, MrpPlanRunDto run, Guid productId, Guid tenantId)
    {
        Guid plannedOrderId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            using (TenantContextAccessor.PushTenant(tenantId))
            {
                plannedOrderId = await db.Set<MrpPlannedOrder>()
                    .Where(o => o.PlanRunId == run.Id && o.ProductId == productId)
                    .Select(o => o.Id)
                    .FirstAsync();
            }
        }
        var firm = await client.PostAsJsonAsync(
            $"/api/v1/mrp/planned-orders/{plannedOrderId}/firm",
            new { operationId = Guid.NewGuid() });
        firm.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlanRuns_list_is_paginated()
    {
        await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), DateTime.UtcNow, MrpBucketKind.Day, 30), JsonOptions);

        var response = await client.GetAsync("/api/v1/mrp/plan/runs?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<MrpPlanRunDto>>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.Page.Should().Be(1);
        body.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task ActionMessages_list_is_paginated_and_filterable()
    {
        await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        await client.PostAsJsonAsync("/api/v1/mrp/plan/commit",
            new CommitMrpPlanCommand(Guid.NewGuid(), DateTime.UtcNow, MrpBucketKind.Day, 30), JsonOptions);

        var response = await client.GetAsync("/api/v1/mrp/action-messages?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<MrpActionMessageDto>>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Preview_stays_within_round_trip_budget()
    {
        await SeedReorderProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        await WarmUpAsync(client, "/api/v1/mrp/plan/preview?bucket=Day&horizon=30");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/mrp/plan/preview?bucket=Day&horizon=30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            PreviewRoundTripBudget,
            $"MRP preview load is bounded by the snapshot loader's batch queries + BOM waves, " +
            $"NOT per-product N+1 (observed {counter.Total})");
    }

    private async Task WarmUpAsync(HttpClient client, string url)
    {
        var warm = await client.GetAsync(url);
        warm.EnsureSuccessStatusCode();
    }

    private async Task<Guid> SeedReorderProductAsync(TenantFixture? tenant = null)
    {
        var tenantId = (tenant ?? _factory.TenantA).TenantId;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(tenantId))
        {
            var warehouse = new Warehouse($"WH-{Guid.NewGuid():N}"[..10], "MRP WH")
            {
                TenantId = tenantId,
            };
            db.Warehouses.Add(warehouse);

            var product = new Product($"MRPP-{Guid.NewGuid():N}"[..15], "MRP Plan Widget", "pcs", 10m, "TRY")
            {
                TenantId = tenantId,
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
                minStock: 10m, maxStock: 100m, reorderPoint: 50m,
                safetyStock: 20m, leadTimeDays: 5,
                weightKg: null, widthCm: null, heightCm: null, depthCm: null, volumeM3: null,
                status: ProductStatus.Active, launchDate: null, endOfLifeDate: null);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var stockItem = new StockItem(product.Id, warehouse.Id)
            {
                TenantId = tenantId,
            };
            stockItem.ApplyReceipt(5m, 5m, DateTime.UtcNow);
            db.Set<StockItem>().Add(stockItem);
            await db.SaveChangesAsync();
            return product.Id;
        }
    }
}
