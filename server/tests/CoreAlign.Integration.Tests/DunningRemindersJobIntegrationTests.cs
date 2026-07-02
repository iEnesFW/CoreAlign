using System.Text.Json;
using CoreAlign.Application.Dunning;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class DunningRemindersJobIntegrationTests
{
    private const string TemplateKey = "Dunning.QuoteExpiringReminder";

    private readonly CoreAlignWebApiFactory _factory;

    public DunningRemindersJobIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Job_dispatches_one_reminder_per_record_and_dedupes_on_rerun()
    {
        var recipientUserId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            var templates = scope.ServiceProvider.GetRequiredService<INotificationTemplateRepository>();

            var tenant = new Tenant("Dunning-Job-Tenant", $"dunning-job-{Guid.NewGuid():N}");
            var tenantId = tenant.Id;
            db.Set<Tenant>().Add(tenant);

            var customer = new Customer("Dunning Job Customer") { TenantId = tenantId };
            db.Set<Customer>().Add(customer);

            if (!await templates.ExistsAsync(null, TemplateKey, NotificationChannel.InApp, "tr", CancellationToken.None))
            {
                db.Set<NotificationTemplate>().Add(new NotificationTemplate(
                    null,
                    TemplateKey,
                    NotificationChannel.InApp,
                    "tr",
                    "Teklif {{quoteNumber}} süresi doluyor",
                    "{{quoteNumber}} numaralı teklifin süresi {{validUntil}} tarihinde doluyor."));
            }

            var product = new Product("SKU-DUN-Q", "Dunning Quote Item") { TenantId = tenantId };
            db.Set<Product>().Add(product);

            var quote = new Quote(
                "QUO-DUN-1",
                customer.Id,
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(3),
                "TRY")
            {
                TenantId = tenantId
            };
            var line = new QuoteLine(product.Id, "SKU-DUN-Q", "Dunning Quote Item", 1m, 100m) { TenantId = tenantId };
            line.ApplyPricing(1m, 100m, 100m, 0m, 0m, false, 0m, null, false, 0m, null, null, 1m, null, null);
            quote.ReplaceLines(new[] { line });
            quote.MarkSent();
            db.Set<Quote>().Add(quote);

            db.Set<DunningSetting>().Add(new DunningSetting(
                DunningType.QuoteExpiringReminder,
                isEnabled: true,
                sendInApp: true,
                sendEmail: false,
                recipientUserIdsJson: JsonSerializer.Serialize(new[] { recipientUserId }))
            {
                TenantId = tenantId
            });

            await db.SaveChangesAsync();
        }

        await RunJobAsync();
        (await CountRemindersAsync(recipientUserId)).Should().Be(1);

        await RunJobAsync();
        (await CountRemindersAsync(recipientUserId)).Should().Be(1, "a rerun must be suppressed by the dispatcher idempotency hash");
    }

    private async Task RunJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<DunningRemindersJob>();
        await job.RunAsync(CancellationToken.None);
    }

    private async Task<int> CountRemindersAsync(Guid recipientUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        return await db.Set<NotificationMessage>()
            .IgnoreQueryFilters()
            .CountAsync(m => m.UserId == recipientUserId && m.TemplateKey == TemplateKey);
    }
}
