using System.Net;
using System.Net.Http.Json;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class CrossTenantIsolationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public CrossTenantIsolationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AdminOfTenantA() => _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    private static readonly HashSet<HttpStatusCode> AcceptableDeny = new()
    {
        HttpStatusCode.NotFound,
        HttpStatusCode.Forbidden,
    };

    private static readonly HashSet<HttpStatusCode> AcceptableDenyWithValidation = new()
    {
        HttpStatusCode.NotFound,
        HttpStatusCode.Forbidden,
        HttpStatusCode.BadRequest,
        HttpStatusCode.Conflict,
    };

    private static void AssertDenied(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response.StatusCode.Should().NotBe(HttpStatusCode.NoContent);
        AcceptableDeny.Should().Contain(response.StatusCode, "cross-tenant lookups must surface as not-found / forbidden, NOT as 400/409 (those indicate the handler reached the body before the tenant check)");
    }

    private static void AssertDeniedAllowValidation(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response.StatusCode.Should().NotBe(HttpStatusCode.NoContent);
        AcceptableDenyWithValidation.Should().Contain(response.StatusCode);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadCustomerOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadCustomerSummaryOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}/summary");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadCustomerOverviewOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}/overview");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadCustomerAnalyticsOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}/analytics");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ReadingCustomerTransactionsOfTenantB_ReturnsEmpty()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}/transactions");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AssertDenied(response);
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.OrderId.ToString());
        body.Should().NotContain(_factory.TenantB.InvoiceId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_CannotReadCustomerAddressesOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}/addresses");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadOrderOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ReadingOrderInvoicesOfTenantB_ReturnsEmpty()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}/invoices");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AssertDenied(response);
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.InvoiceId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_CannotSubmitOrderOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.PostAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}/submit", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotApproveOrderOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.PostAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}/approve", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotAllocateOrderOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.PostAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}/allocate", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotCancelOrderOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.PostAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}/cancel", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDeleteOrderOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.DeleteAsync($"/api/v1/Orders/{_factory.TenantB.OrderId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadInvoiceOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Invoices/{_factory.TenantB.InvoiceId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotMarkInvoicePaidOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.PostAsync($"/api/v1/Invoices/{_factory.TenantB.InvoiceId}/mark-paid", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotGenerateInvoiceFromTenantBOrder()
    {
        var client = AdminOfTenantA();
        var response = await client.PostAsync($"/api/v1/Invoices/from-order/{_factory.TenantB.OrderId}", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ReadingCreditNotesOfTenantBInvoice_ReturnsEmpty()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Invoices/{_factory.TenantB.InvoiceId}/credit-notes");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AssertDenied(response);
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.InvoiceId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_ReadingCreditedByLineOfTenantBInvoice_ReturnsEmpty()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Invoices/{_factory.TenantB.InvoiceId}/credited-by-line");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AssertDenied(response);
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.InvoiceId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_CannotIssueCreditNoteOnTenantBInvoice()
    {
        var client = AdminOfTenantA();
        var body = new
        {
            lines = new[] { new { invoiceLineId = Guid.NewGuid(), quantity = 1m } },
            reason = (string?)null,
            operationId = Guid.NewGuid(),
        };
        var response = await client.PostAsJsonAsync(
            $"/api/v1/Invoices/{_factory.TenantB.InvoiceId}/credit-notes",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadProductOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Products/{_factory.TenantB.Product1Id}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CustomerUsersFilteredByTenantBCustomer_ReturnsEmpty()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/customer-users?customerId={_factory.TenantB.CustomerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.CustomerUserId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_DealerCustomerLinksFilteredByTenantBDealer_ReturnsEmpty()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/dealer-customer-links?dealerAccountId={_factory.TenantB.DealerAccountId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.CustomerId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_DealerAccountsListDoesNotIncludeTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/dealer-accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.DealerAccountId.ToString());
        body.Should().Contain(_factory.TenantA.DealerAccountId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_ListsOnlyOwnCustomers()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/Customers?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.CustomerId.ToString());
        body.Should().Contain(_factory.TenantA.CustomerId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_ListsOnlyOwnOrders()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/Orders?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.OrderId.ToString());
        body.Should().Contain(_factory.TenantA.OrderId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_ListsOnlyOwnInvoices()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/Invoices?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.InvoiceId.ToString());
        body.Should().Contain(_factory.TenantA.InvoiceId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_ListsOnlyOwnProducts()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/Products?page=1&pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.Product1Id.ToString());
        body.Should().NotContain(_factory.TenantB.Product2Id.ToString());
        body.Should().Contain(_factory.TenantA.Product1Id.ToString());
    }

    [Fact]
    public async Task UnauthenticatedRequest_IsRejected()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/Customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadCustomerStatementOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantB.CustomerId}/statement?format=json");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotAttachTagToCustomerOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeTagId = Guid.NewGuid();
        var response = await client.PostAsync(
            $"/api/v1/Customers/{_factory.TenantB.CustomerId}/tags/{fakeTagId}",
            content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDetachTagFromCustomerOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeTagId = Guid.NewGuid();
        var response = await client.DeleteAsync(
            $"/api/v1/Customers/{_factory.TenantB.CustomerId}/tags/{fakeTagId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotMergeCustomersOfTenantB()
    {
        var client = AdminOfTenantA();
        var body = new
        {
            operationId = Guid.NewGuid(),
            sourceCustomerId = _factory.TenantB.CustomerId,
            targetCustomerId = _factory.TenantB.CustomerId,
            sourceUpdatedAtUtc = DateTime.UtcNow,
            targetUpdatedAtUtc = DateTime.UtcNow,
            notes = (string?)null,
        };
        var response = await client.PostAsJsonAsync("/api/v1/Customers/merge", body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotMergeTenantBSourceIntoTenantATarget()
    {
        var client = AdminOfTenantA();
        var body = new
        {
            operationId = Guid.NewGuid(),
            sourceCustomerId = _factory.TenantB.CustomerId,
            targetCustomerId = _factory.TenantA.CustomerId,
            sourceUpdatedAtUtc = DateTime.UtcNow,
            targetUpdatedAtUtc = DateTime.UtcNow,
            notes = (string?)null,
        };
        var response = await client.PostAsJsonAsync("/api/v1/Customers/merge", body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotListProductVariantsOfTenantBProduct()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync(
            $"/api/v1/products/{_factory.TenantB.Product1Id}/variants");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AssertDenied(response);
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.Product1Id.ToString());
    }

    [Fact]
    public async Task TenantAdminA_CannotCreateProductVariantOnTenantBProduct()
    {
        var client = AdminOfTenantA();
        var body = new
        {
            sku = "VAR-XB-001",
            barcode = (string?)null,
            variantAttributesJson = "{}",
            priceOverride = (decimal?)null,
            stockQuantity = 0m,
            isActive = true,
        };
        var response = await client.PostAsJsonAsync(
            $"/api/v1/products/{_factory.TenantB.Product1Id}/variants",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotUpdateProductVariantOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeVariantId = Guid.NewGuid();
        var body = new
        {
            sku = "VAR-XB-002",
            barcode = (string?)null,
            variantAttributesJson = "{}",
            priceOverride = (decimal?)null,
            isActive = true,
        };
        var response = await client.PutAsJsonAsync(
            $"/api/v1/products/{_factory.TenantB.Product1Id}/variants/{fakeVariantId}",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDeleteProductVariantOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeVariantId = Guid.NewGuid();
        var response = await client.DeleteAsync(
            $"/api/v1/products/{_factory.TenantB.Product1Id}/variants/{fakeVariantId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotListProductImagesOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync(
            $"/api/v1/products/{_factory.TenantB.Product1Id}/images");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            AssertDenied(response);
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.Product1Id.ToString());
    }

    [Fact]
    public async Task TenantAdminA_CannotRunCustomReportOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeReportId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/reports/custom/{fakeReportId}/run?format=json");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotUpdateReportScheduleOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeScheduleId = Guid.NewGuid();
        var body = new
        {
            name = "Cross-tenant-attempt",
            reportKey = "inventory-stock-on-hand",
            cron = "0 0 * * *",
            format = "pdf",
            timezone = "UTC",
            recipients = new[] { "evil@example.com" },
            parametersJson = "{}",
            isActive = true,
        };
        var response = await client.PutAsJsonAsync(
            $"/api/v1/reports/schedules/{fakeScheduleId}",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDeleteReportScheduleOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeScheduleId = Guid.NewGuid();
        var response = await client.DeleteAsync($"/api/v1/reports/schedules/{fakeScheduleId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ListReportSchedulesDoesNotLeakTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/reports/schedules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.TenantId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_ListCustomReportsDoesNotLeakTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/reports/custom");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.TenantId.ToString());
    }

    [Fact]
    public async Task TenantAdminA_CannotDownloadReportUnderTenantBSlugCannotElevatePersona()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync(
            "/api/v1/reports/inventory-stock-on-hand?format=pdf");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsByteArrayAsync();
            body.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task TenantAdminA_CannotReadPaymentOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/Payments/{_factory.TenantB.PaymentId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotConfirmPaymentOfTenantB()
    {
        var client = AdminOfTenantA();
        var body = new { id = _factory.TenantB.PaymentId, postedByUserId = (Guid?)null };
        var response = await client.PostAsJsonAsync(
            $"/api/v1/Payments/{_factory.TenantB.PaymentId}/confirm",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotApplyPaymentOfTenantB()
    {
        var client = AdminOfTenantA();
        var body = new
        {
            id = _factory.TenantB.PaymentId,
            invoiceId = _factory.TenantB.InvoiceId,
            amount = 1m,
        };
        var response = await client.PostAsJsonAsync(
            $"/api/v1/Payments/{_factory.TenantB.PaymentId}/apply",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotVoidPaymentOfTenantB()
    {
        var client = AdminOfTenantA();
        var body = new { id = _factory.TenantB.PaymentId, reason = "evil" };
        var response = await client.PostAsJsonAsync(
            $"/api/v1/Payments/{_factory.TenantB.PaymentId}/void",
            body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDownloadInvoicePdfOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/invoices/{_factory.TenantB.InvoiceId}/pdf");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDownloadCreditNotePdfOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/invoices/{_factory.TenantB.InvoiceId}/credit-note/pdf");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDownloadOrderPdfOfTenantB()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/orders/{_factory.TenantB.OrderId}/pdf");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDownloadShipmentPackingSlipOfTenantB()
    {
        var client = AdminOfTenantA();
        var fakeShipmentId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/shipments/{fakeShipmentId}/packing-slip/pdf");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReadMrpPeggingOfUnknownPlanRun()
    {
        var client = AdminOfTenantA();
        var fakePlanRunId = Guid.NewGuid();
        var fakeComponentId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/mrp/pegging/{fakePlanRunId}/{fakeComponentId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotReleasePlannedOrdersOfUnknownPlanRun()
    {
        var client = AdminOfTenantA();
        var fakePlanRunId = Guid.NewGuid();
        var body = new
        {
            plannedOrderIds = new[] { Guid.NewGuid() },
            operationId = Guid.NewGuid(),
        };
        var response = await client.PostAsJsonAsync($"/api/v1/mrp/plan/{fakePlanRunId}/release", body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotFirmUnknownPlannedOrder()
    {
        var client = AdminOfTenantA();
        var fakePlannedOrderId = Guid.NewGuid();
        var body = new { operationId = Guid.NewGuid(), overrideQuantity = (decimal?)null, overrideDueDateUtc = (DateTime?)null };
        var response = await client.PostAsJsonAsync($"/api/v1/mrp/planned-orders/{fakePlannedOrderId}/firm", body);
        AssertDeniedAllowValidation(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDismissUnknownMrpActionMessage()
    {
        var client = AdminOfTenantA();
        var fakeMessageId = Guid.NewGuid();
        var response = await client.PostAsync($"/api/v1/mrp/action-messages/{fakeMessageId}/dismiss", content: null);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotFirmRealPlannedOrderOwnedByTenantB()
    {
        var (_, plannedOrderId) = await SeedTenantBPlanRunAsync(horizonDays: 31);

        var client = AdminOfTenantA();
        var body = new { operationId = Guid.NewGuid(), overrideQuantity = (decimal?)null, overrideDueDateUtc = (DateTime?)null };
        var response = await client.PostAsJsonAsync($"/api/v1/mrp/planned-orders/{plannedOrderId}/firm", body);

        AssertDeniedAllowValidation(response);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var stored = await db.Set<CoreAlign.Domain.Entities.Mrp.MrpPlannedOrder>()
                .FirstAsync(o => o.Id == plannedOrderId);
            stored.IsFirmed.Should().BeFalse("Tenant A must not be able to mutate Tenant B's planned order");
        }
    }

    [Fact]
    public async Task TenantAdminA_CannotReadRealMrpPeggingOwnedByTenantB()
    {
        var (planRunId, _) = await SeedTenantBPlanRunAsync(horizonDays: 32);

        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/mrp/pegging/{planRunId}/{Guid.NewGuid()}");

        AssertDenied(response);
    }

    private async Task<(Guid PlanRunId, Guid PlannedOrderId)> SeedTenantBPlanRunAsync(int horizonDays)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var run = new CoreAlign.Domain.Entities.Mrp.MrpPlanRun(
                $"MRP-CT-{Guid.NewGuid():N}"[..12],
                DateTime.UtcNow,
                CoreAlign.Domain.Enums.MrpBucketKind.Day,
                horizonDays,
                _factory.TenantB.TenantAdminUserId);

            run.AddPlannedOrder(new CoreAlign.Domain.Entities.Mrp.MrpPlannedOrder(
                _factory.TenantB.Product1Id,
                0,
                10m,
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow,
                null,
                5m,
                CoreAlign.Domain.Enums.LotSizingPolicy.LotForLot));

            db.Set<CoreAlign.Domain.Entities.Mrp.MrpPlanRun>().Add(run);
            await db.SaveChangesAsync();

            return (run.Id, run.PlannedOrders.First().Id);
        }
    }

    [Fact]
    public async Task TenantAdminA_CannotDownloadFeedbackAttachmentOfTenantB()
    {
        var feedbackId = await SeedTenantBFeedbackWithAttachmentAsync();

        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/feedback/{feedbackId}/attachment");

        AssertDenied(response);
    }

    private async Task<Guid> SeedTenantBFeedbackWithAttachmentAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var ticket = new CoreAlign.Domain.Entities.FeedbackTicket(
                CoreAlign.Domain.Enums.FeedbackType.Bug,
                "Cross-tenant feedback",
                "Should never be reachable by Tenant A.",
                CoreAlign.Domain.Enums.FeedbackPriority.Medium);
            ticket.AttachFile("tenant-b/feedback-attachments/secret.png", "secret.png", "image/png");

            db.Set<CoreAlign.Domain.Entities.FeedbackTicket>().Add(ticket);
            await db.SaveChangesAsync();

            return ticket.Id;
        }
    }

    [Fact]
    public async Task TenantAdminA_CannotReadGlassProjectTemplateOfTenantB()
    {
        var templateId = await SeedTenantBGlassTemplateAsync();
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/glass-enclosure/project-templates/{templateId}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_CannotDeleteGlassProjectTemplateOfTenantB()
    {
        var templateId = await SeedTenantBGlassTemplateAsync();
        var client = AdminOfTenantA();
        var response = await client.DeleteAsync($"/api/v1/glass-enclosure/project-templates/{templateId}");
        AssertDenied(response);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var stillThere = await db.Set<CoreAlign.Domain.Entities.GlassEnclosure.GlassProjectTemplate>()
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == templateId);
        stillThere.Should().BeTrue("Tenant A must not be able to delete Tenant B's template");
    }

    private async Task<Guid> SeedTenantBGlassTemplateAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var template = new CoreAlign.Domain.Entities.GlassEnclosure.GlassProjectTemplate(
                "Tenant B template",
                Guid.NewGuid(),
                """{"walls":[{}],"slabs":[],"runs":[]}""",
                1,
                0,
                0);

            db.Set<CoreAlign.Domain.Entities.GlassEnclosure.GlassProjectTemplate>().Add(template);
            await db.SaveChangesAsync();

            return template.Id;
        }
    }
}
