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
public class MyServiceTicketsControllerTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public MyServiceTicketsControllerTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_caller_receives_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customer-portal/service-tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customer_creates_ticket_under_own_customer()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var body = new
        {
            Type = nameof(ServiceTicketType.WarrantyClaim),
            Priority = nameof(ServiceTicketPriority.Normal),
            Title = "AC unit not cooling",
            DescriptionMd = "Customer reports the air conditioning unit stopped cooling overnight.",
            WarrantyContractId = (Guid?)null,
        };

        var response = await client.PostAsJsonAsync("/api/v1/customer-portal/service-tickets", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceTicketDto>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.CustomerId.Should().Be(_factory.TenantA.CustomerId);
        payload.Data.Title.Should().Be("AC unit not cooling");
    }

    [Fact]
    public async Task Customer_lists_only_own_tickets()
    {
        await SeedServiceTicketAsync(_factory.TenantA, "Own ticket A1");
        await SeedServiceTicketAsync(_factory.TenantB, "Other tenant ticket");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync("/api/v1/customer-portal/service-tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<ServiceTicketDto>>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.Should().OnlyContain(t => t.CustomerId == _factory.TenantA.CustomerId);
        payload.Data!.Select(t => t.Title).Should().NotContain("Other tenant ticket");
    }

    [Fact]
    public async Task Cross_customer_get_returns_404()
    {
        var otherTicketId = await SeedServiceTicketAsync(_factory.TenantB, "Cross-tenant ticket");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync($"/api/v1/customer-portal/service-tickets/{otherTicketId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dealer_persona_cannot_access_service_tickets_endpoint()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

        var response = await client.GetAsync("/api/v1/customer-portal/service-tickets");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedServiceTicketAsync(TenantFixture tenant, string title)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var service = sp.GetRequiredService<IServiceTicketService>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            var ticket = await service.OpenAsync(
                customerId: tenant.CustomerId,
                type: ServiceTicketType.WarrantyClaim,
                priority: ServiceTicketPriority.Low,
                title: title,
                descriptionMd: "Seeded for integration tests.",
                warrantyContractId: null);
            await db.SaveChangesAsync();
            return ticket.Id;
        }
    }
}
