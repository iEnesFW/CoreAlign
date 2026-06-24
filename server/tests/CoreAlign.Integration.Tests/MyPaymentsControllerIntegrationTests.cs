using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Payments.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class MyPaymentsControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public MyPaymentsControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_caller_receives_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customer-portal/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customer_lists_only_own_payments()
    {
        await SeedConfirmedPaymentAsync(_factory.TenantA, amount: 250m);
        await SeedConfirmedPaymentAsync(_factory.TenantB, amount: 999m);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync("/api/v1/customer-portal/payments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<PaymentSummaryDto>>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.Should().OnlyContain(p => p.CustomerId == _factory.TenantA.CustomerId);
    }

    [Fact]
    public async Task Cross_customer_get_returns_404()
    {
        var otherPaymentId = await SeedConfirmedPaymentAsync(_factory.TenantB, amount: 100m);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync($"/api/v1/customer-portal/payments/{otherPaymentId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Initiate_endpoint_is_authenticated_and_routes_to_dispatcher_handler()
    {
        await EnsureIssuedInvoiceWithLineAsync(_factory.TenantA, _factory.TenantA.InvoiceId);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var body = new
        {
            InvoiceId = _factory.TenantA.InvoiceId,
            BillingInfo = (object?)null,
            GatewayName = "non-existent-gateway",
        };

        var response = await client.PostAsJsonAsync("/api/v1/customer-portal/payments/initiate", body);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Initiate_endpoint_rejects_unauthenticated_callers()
    {
        var client = _factory.CreateClient();

        var body = new
        {
            InvoiceId = _factory.TenantA.InvoiceId,
            BillingInfo = (object?)null,
            GatewayName = (string?)null,
        };

        var response = await client.PostAsJsonAsync("/api/v1/customer-portal/payments/initiate", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Initiate_endpoint_rejects_dealer_persona()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

        var body = new
        {
            InvoiceId = _factory.TenantA.InvoiceId,
            BillingInfo = (object?)null,
            GatewayName = (string?)null,
        };

        var response = await client.PostAsJsonAsync("/api/v1/customer-portal/payments/initiate", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dealer_persona_cannot_access_payments_endpoint()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

        var response = await client.GetAsync("/api/v1/customer-portal/payments");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedConfirmedPaymentAsync(TenantFixture tenant, decimal amount)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            var payment = new Payment(
                paymentNumber: $"PMT-{Guid.NewGuid():N}".Substring(0, 14),
                customerId: tenant.CustomerId,
                customerNameSnapshot: $"Customer-{tenant.TenantSlug}",
                direction: PaymentDirection.CustomerReceipt,
                paymentDate: DateTime.UtcNow,
                method: PaymentMethod.BankTransfer,
                amount: amount,
                currency: "TRY");
            payment.TenantId = tenant.TenantId;
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            return payment.Id;
        }
    }

    private async Task EnsureIssuedInvoiceWithLineAsync(TenantFixture tenant, Guid invoiceId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            await db.Invoices
                .Where(i => i.Id == invoiceId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.Status, InvoiceStatus.Issued)
                    .SetProperty(i => i.Total, 100m));
        }
    }
}
