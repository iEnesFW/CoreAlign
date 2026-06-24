using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreAlign.Application.Tests.Invoices;

public class GetCreditedQuantitiesByLineIntegrationTests
{
    private readonly InMemoryDatabaseRoot _root = new();

    [Fact]
    public async Task Aggregates_credited_quantity_from_persisted_credit_note_lines()
    {
        var tenantId = Guid.NewGuid();
        var dbName = $"credited-by-line-{Guid.NewGuid():N}";

        var (origin, originLine) = await SeedInvoiceWithCreditNoteAsync(dbName, tenantId, creditedQuantity: 4m);

        await using var queryDb = CreateContext(dbName, tenantId);
        var handler = new GetCreditedQuantitiesByLineQueryHandler(new InvoiceRepository(queryDb));

        var result = await handler.Handle(new GetCreditedQuantitiesByLineQuery(origin.Id), default);

        result.Should().ContainSingle();
        result[0].InvoiceLineId.Should().Be(originLine.Id);
        result[0].CreditedQuantity.Should().Be(4m);
    }

    [Fact]
    public async Task Returns_empty_for_invoice_owned_by_another_tenant()
    {
        var ownerTenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var dbName = $"credited-by-line-xt-{Guid.NewGuid():N}";

        var (origin, _) = await SeedInvoiceWithCreditNoteAsync(dbName, ownerTenant, creditedQuantity: 4m);

        await using var queryDb = CreateContext(dbName, otherTenant);
        var handler = new GetCreditedQuantitiesByLineQueryHandler(new InvoiceRepository(queryDb));

        var result = await handler.Handle(new GetCreditedQuantitiesByLineQuery(origin.Id), default);

        result.Should().BeEmpty("the global tenant filter must hide another tenant's credited quantities");
    }

    private async Task<(Invoice Origin, InvoiceLine OriginLine)> SeedInvoiceWithCreditNoteAsync(
        string dbName,
        Guid tenantId,
        decimal creditedQuantity)
    {
        var origin = new Invoice("INV-1", Guid.NewGuid(), "Acme", "TRY") { TenantId = tenantId };
        var originLine = new InvoiceLine(Guid.NewGuid(), "SKU-1", "Widget", 10m, 5m) { TenantId = tenantId };
        origin.ReplaceLines(new[] { originLine });
        origin.Issue("INV-1");

        var creditLine = new InvoiceLine(
            originLine.ProductId ?? Guid.Empty,
            originLine.ProductSku,
            originLine.ProductName,
            creditedQuantity,
            originLine.UnitPrice)
        {
            TenantId = tenantId,
        };
        creditLine.ApplyPricing(
            quantity: creditedQuantity,
            unitPrice: originLine.UnitPrice,
            lineDiscountPercent: 0m,
            lineDiscountAmount: 0m,
            taxRatePercent: 0m,
            taxRateId: null,
            isTaxInclusive: false,
            withholdingRatePercent: 0m,
            uomId: null,
            uomCode: null,
            description: null,
            revenueAccountCode: null,
            costCenter: null,
            project: null,
            originOrderLineId: originLine.Id);
        var creditNote = Invoice.IssueCreditNote(origin, "CN-1", DateTime.UtcNow, new[] { creditLine }, null, null, null);

        await using var seedDb = CreateContext(dbName, tenantId);
        seedDb.Invoices.Add(origin);
        seedDb.Invoices.Add(creditNote);
        await seedDb.SaveChangesAsync();

        return (origin, originLine);
    }

    private CoreAlignDbContext CreateContext(string dbName, Guid tenantId)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CurrentTenantId.Returns(tenantId);
        tenantContext.HasTenant.Returns(true);
        tenantContext.RequireTenantId().Returns(tenantId);

        var publisher = Substitute.For<IPublisher>();

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase(dbName, _root)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new CoreAlignDbContext(options, tenantContext, publisher);
        db.Database.EnsureCreated();
        return db;
    }
}
