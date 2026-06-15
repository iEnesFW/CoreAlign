using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class PurchaseRequisitionsControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public PurchaseRequisitionsControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_then_submit_then_approve_flow_returns_success_statuses()
    {
        var productId = await SeedProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var createCmd = new CreatePurchaseRequisitionCommand(
            PurchaseRequisitionReason.Manual,
            new List<PurchaseRequisitionLineInput>
            {
                new(productId, QuantityRequested: 5m, EstimatedUnitCost: 12m),
            },
            Notes: "integration test");

        var createResponse = await client.PostAsJsonAsync("/api/v1/purchase-requisitions", createCmd, JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PurchaseRequisitionDto>>(JsonOptions);
        created!.Data.Should().NotBeNull();
        var id = created.Data!.Id;
        created.Data.Status.Should().Be(PurchaseRequisitionStatus.Draft);

        var submitResponse = await client.PostAsync($"/api/v1/purchase-requisitions/{id}/submit", content: null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<ApiResponse<PurchaseRequisitionDto>>(JsonOptions);
        submitted!.Data!.Status.Should().Be(PurchaseRequisitionStatus.Submitted);

        var approveResponse = await client.PostAsync($"/api/v1/purchase-requisitions/{id}/approve", content: null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResponse.Content.ReadFromJsonAsync<ApiResponse<PurchaseRequisitionDto>>(JsonOptions);
        approved!.Data!.Status.Should().Be(PurchaseRequisitionStatus.Approved);
        approved.Data.ApprovedByUserId.Should().Be(_factory.TenantA.TenantAdminUserId);
    }

    [Fact]
    public async Task Search_filters_by_status()
    {
        var productId = await SeedProductAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var createCmd = new CreatePurchaseRequisitionCommand(
            PurchaseRequisitionReason.Manual,
            new List<PurchaseRequisitionLineInput>
            {
                new(productId, QuantityRequested: 1m, EstimatedUnitCost: 1m),
            });
        var createResponse = await client.PostAsJsonAsync("/api/v1/purchase-requisitions", createCmd, JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync(
            $"/api/v1/purchase-requisitions?status={(int)PurchaseRequisitionStatus.Draft}&page=1&pageSize=25");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PurchaseRequisitionDto>>>(JsonOptions);
        list!.Data.Should().NotBeNull();
        list.Data!.Items.Should().OnlyContain(x => x.Status == PurchaseRequisitionStatus.Draft);
    }

    private async Task<Guid> SeedProductAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var product = new Product($"PR-{Guid.NewGuid():N}"[..14], "PR Widget", "pcs", 12m, "TRY")
            {
                TenantId = _factory.TenantA.TenantId,
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }
    }
}
