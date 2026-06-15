using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Application.Common;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class MrpControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MrpControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_dashboard_returns_200_with_payload()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/mrp/dashboard?topN=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MrpDashboardDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.GeneratedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Post_generate_suggestions_returns_202_and_writes_outbox_event_when_candidates_exist()
    {
        var productId = await SeedReorderCandidateAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.PostAsJsonAsync(
            "/api/v1/mrp/generate-suggestions",
            new GenerateMrpSuggestionsCommand(DateTime.UtcNow));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MrpSuggestionResultDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var outboxCount = await db.OutboxMessages
                .Where(o => o.Type == "MrpSuggestionsCreated")
                .CountAsync();
            outboxCount.Should().BeGreaterThan(0);
        }
    }

    private async Task<Guid> SeedReorderCandidateAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var product = new Product($"MRP-{Guid.NewGuid():N}"[..15], "Mrp Reorder Widget", "pcs", 10m, "TRY")
            {
                TenantId = _factory.TenantA.TenantId,
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
                minStock: 0m, maxStock: 100m, reorderPoint: 50m,
                safetyStock: 0m, leadTimeDays: 3,
                weightKg: null, widthCm: null, heightCm: null, depthCm: null, volumeM3: null,
                status: ProductStatus.Active, launchDate: null, endOfLifeDate: null);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }
    }
}
