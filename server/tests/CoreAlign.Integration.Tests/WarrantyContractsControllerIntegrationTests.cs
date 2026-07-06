using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class WarrantyContractsControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public WarrantyContractsControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_creates_warranty_contract_and_returns_201()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var cmd = new CreateWarrantyContractCommand(
            OrderId: _factory.TenantA.OrderId,
            CustomerId: _factory.TenantA.CustomerId,
            CoverageType: WarrantyCoverageType.FullService,
            WarrantyMonths: 24,
            TermsJson: "{\"coverage\":\"full\"}");

        var response = await client.PostAsJsonAsync("/api/v1/warranty-contracts", cmd, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WarrantyContractDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Number.Should().StartWith($"WC-{DateTime.UtcNow.Year}-");
        body.Data.WarrantyMonths.Should().Be(24);
        body.Data.Status.Should().Be(WarrantyContractStatus.Active);
    }

    [Fact]
    public async Task Get_by_id_returns_persisted_contract()
    {
        var contractId = await SeedWarrantyContractAsync(_factory.TenantA.TenantId, _factory.TenantA.CustomerId, _factory.TenantA.OrderId, months: 12);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync($"/api/v1/warranty-contracts/{contractId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WarrantyContractDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(contractId);
    }

    [Fact]
    public async Task Post_extend_extends_end_date_by_added_months()
    {
        var contractId = await SeedWarrantyContractAsync(_factory.TenantA.TenantId, _factory.TenantA.CustomerId, _factory.TenantA.OrderId, months: 12);
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var getBefore = await client.GetFromJsonAsync<ApiResponse<WarrantyContractDto>>(
            $"/api/v1/warranty-contracts/{contractId}", JsonOptions);
        var beforeEnd = getBefore!.Data!.EndDate;

        var extendCmd = new ExtendWarrantyContractCommand(contractId, MonthsAdded: 6, Reason: "Customer upgrade");
        var response = await client.PostAsJsonAsync($"/api/v1/warranty-contracts/{contractId}/extend", extendCmd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WarrantyContractDto>>(JsonOptions);
        body!.Data!.EndDate.Should().Be(beforeEnd.AddMonths(6));
        body.Data.WarrantyMonths.Should().Be(18);
    }

    [Fact]
    public async Task TenantB_admin_cannot_read_TenantA_warranty_contract()
    {
        var contractId = await SeedWarrantyContractAsync(_factory.TenantA.TenantId, _factory.TenantA.CustomerId, _factory.TenantA.OrderId, months: 12);
        var clientB = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);

        var response = await clientB.GetAsync($"/api/v1/warranty-contracts/{contractId}");

        var acceptableDeny = new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden };
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<WarrantyContractDto>>(JsonOptions);
            body!.Data.Should().BeNull("cross-tenant lookups must not surface the other tenant's data");
        }
        else
        {
            acceptableDeny.Should().Contain(response.StatusCode);
        }
    }

    private async Task<Guid> SeedWarrantyContractAsync(Guid tenantId, Guid customerId, Guid orderId, int months)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var service = sp.GetRequiredService<IWarrantyContractService>();

        using (TenantContextAccessor.PushTenant(tenantId))
        {
            var contract = await service.CreateAsync(
                orderId: orderId,
                customerId: customerId,
                coverageType: WarrantyCoverageType.FullService,
                warrantyMonths: months,
                termsJson: "{}");
            await db.SaveChangesAsync();
            return contract.Id;
        }
    }
}
