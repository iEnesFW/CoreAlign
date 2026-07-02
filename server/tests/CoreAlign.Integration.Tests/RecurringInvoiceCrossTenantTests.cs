using System.Net;
using CoreAlign.Domain.Entities.Invoices;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class RecurringInvoiceCrossTenantTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public RecurringInvoiceCrossTenantTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AdminOfTenantA() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    private static readonly HashSet<HttpStatusCode> AcceptableDeny = new()
    {
        HttpStatusCode.NotFound,
        HttpStatusCode.Forbidden,
    };

    private static void AssertDenied(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response.StatusCode.Should().NotBe(HttpStatusCode.NoContent);
        AcceptableDeny.Should().Contain(response.StatusCode);
    }

    private async Task<Guid> SeedTenantBTemplateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        var template = new RecurringInvoiceTemplate(
            name: "TenantB retainer",
            customerId: _factory.TenantB.CustomerId,
            currency: "TRY",
            createdByUserId: _factory.TenantB.TenantAdminUserId,
            frequency: RecurrenceFrequency.Monthly,
            intervalCount: 1,
            anchorDayOfMonth: 1,
            anchorDayOfWeek: null,
            startDate: new DateOnly(2026, 1, 1),
            endDate: null,
            maxOccurrences: null,
            dueDays: 30,
            paymentTermsId: null,
            headerDiscountPercent: null,
            headerDiscountAmount: null,
            shippingCost: null,
            roundingAdjustment: null,
            autoConfirm: true,
            publicNotes: null,
            internalNotes: null)
        {
            TenantId = _factory.TenantB.TenantId,
        };
        template.ReplaceLines(new[]
        {
            new RecurringInvoiceTemplateLine(
                productId: _factory.TenantB.Product1Id,
                productSku: "SEED",
                productName: "Seed line",
                description: null,
                quantity: 1m,
                unitPrice: 100m),
        });

        db.RecurringInvoiceTemplates.Add(template);
        await db.SaveChangesAsync();
        return template.Id;
    }

    [Fact]
    public async Task TenantAdminA_CannotReadRecurringInvoiceOfTenantB()
    {
        var id = await SeedTenantBTemplateAsync();
        var response = await AdminOfTenantA().GetAsync($"/api/v1/recurring-invoices/{id}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotRunNowRecurringInvoiceOfTenantB()
    {
        var id = await SeedTenantBTemplateAsync();
        var response = await AdminOfTenantA().PostAsync($"/api/v1/recurring-invoices/{id}/run-now", null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotPauseRecurringInvoiceOfTenantB()
    {
        var id = await SeedTenantBTemplateAsync();
        var response = await AdminOfTenantA().PostAsync($"/api/v1/recurring-invoices/{id}/pause", null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotCancelRecurringInvoiceOfTenantB()
    {
        var id = await SeedTenantBTemplateAsync();
        var response = await AdminOfTenantA().PostAsync($"/api/v1/recurring-invoices/{id}/cancel", null);
        AssertDenied(response);
    }

    [Fact]
    public async Task UnknownRecurringInvoiceId_ReturnsNotFound()
    {
        var response = await AdminOfTenantA().GetAsync($"/api/v1/recurring-invoices/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
