using System.Linq.Expressions;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Persistence;

public class CoreAlignDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IPublisher _publisher;

    public CoreAlignDbContext(
        DbContextOptions<CoreAlignDbContext> options,
        ITenantContext tenantContext,
        IPublisher publisher)
        : base(options)
    {
        _tenantContext = tenantContext;
        _publisher = publisher;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<LoginAuditLog> LoginAuditLogs => Set<LoginAuditLog>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<CustomerTransaction> CustomerTransactions => Set<CustomerTransaction>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<ProductComponent> ProductComponents => Set<ProductComponent>();

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockAllocation> StockAllocations => Set<StockAllocation>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<StockReasonCode> StockReasonCodes => Set<StockReasonCode>();

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentApplication> PaymentApplications => Set<PaymentApplication>();
    public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();

    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<CustomerProductPrice> CustomerProductPrices => Set<CustomerProductPrice>();
    public DbSet<GLAccount> GLAccounts => Set<GLAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorAddress> VendorAddresses => Set<VendorAddress>();
    public DbSet<VendorContact> VendorContacts => Set<VendorContact>();
    public DbSet<VendorBankAccount> VendorBankAccounts => Set<VendorBankAccount>();
    public DbSet<VendorLedgerEntry> VendorLedgerEntries => Set<VendorLedgerEntry>();

    public DbSet<TenantSetting> TenantSettingsStore => Set<TenantSetting>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    // Global reference lookups (not tenant-scoped).
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    // Auth, audit, compliance, two-factor, sessions
    public DbSet<TwoFactorBackupCode> TwoFactorBackupCodes => Set<TwoFactorBackupCode>();
    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<EntityAuditLog> EntityAuditLogs => Set<EntityAuditLog>();
    public DbSet<DataSubjectRequest> DataSubjectRequests => Set<DataSubjectRequest>();
    public DbSet<CoreAlign.Domain.Entities.Privacy.RetentionPolicy> RetentionPolicies => Set<CoreAlign.Domain.Entities.Privacy.RetentionPolicy>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Sales: revisions, quotes, returns, templates
    public DbSet<OrderRevision> OrderRevisions => Set<OrderRevision>();
    public DbSet<OrderTemplate> OrderTemplates => Set<OrderTemplate>();
    public DbSet<OrderTemplateLine> OrderTemplateLines => Set<OrderTemplateLine>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnRequestLine> ReturnRequestLines => Set<ReturnRequestLine>();
    public DbSet<DealerCommissionLedgerEntry> DealerCommissionLedgerEntries => Set<DealerCommissionLedgerEntry>();

    // Inventory: counts, substitutes
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<ProductSubstitute> ProductSubstitutes => Set<ProductSubstitute>();

    // Purchasing / AP
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<VendorBill> VendorBills => Set<VendorBill>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();
    public DbSet<VendorPaymentApplication> VendorPaymentApplications => Set<VendorPaymentApplication>();

    // Accounting
    public DbSet<GLPostingMapping> GLPostingMappings => Set<GLPostingMapping>();
    public DbSet<TaxDeclaration> TaxDeclarations => Set<TaxDeclaration>();
    public DbSet<TaxDeclarationLine> TaxDeclarationLines => Set<TaxDeclarationLine>();

    // Pricing rules
    public DbSet<CoreAlign.Domain.Entities.Pricing.DiscountRule> PricingDiscountRules => Set<CoreAlign.Domain.Entities.Pricing.DiscountRule>();
    public DbSet<TaxRule> PricingTaxRules => Set<TaxRule>();

    // Outbox + webhooks + provider configs
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<ProviderWebhookInbox> ProviderWebhookInbox => Set<ProviderWebhookInbox>();
    public DbSet<TenantProviderConfig> TenantProviderConfigs => Set<TenantProviderConfig>();

    // Tags + collaboration
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CustomerTagLink> CustomerTagLinks => Set<CustomerTagLink>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<FeedbackTicket> FeedbackTickets => Set<FeedbackTicket>();

    // B2B (dealer / customer portal)
    public DbSet<DealerAccount> DealerAccounts => Set<DealerAccount>();
    public DbSet<DealerUser> DealerUsers => Set<DealerUser>();
    public DbSet<DealerCustomerLink> DealerCustomerLinks => Set<DealerCustomerLink>();
    public DbSet<CustomerUser> CustomerUsers => Set<CustomerUser>();
    public DbSet<CustomerDealerProductVisibility> CustomerDealerProductVisibilities => Set<CustomerDealerProductVisibility>();

    // Billing modules + subscription orders + payments
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<ModulePricePlan> ModulePricePlans => Set<ModulePricePlan>();
    public DbSet<SubscriptionOrder> SubscriptionOrders => Set<SubscriptionOrder>();
    public DbSet<SubscriptionOrderItem> SubscriptionOrderItems => Set<SubscriptionOrderItem>();
    public DbSet<PaymentSession> PaymentSessions => Set<PaymentSession>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<CoreAlign.Domain.Entities.Payments.PaymentTransaction> PaymentTransactions => Set<CoreAlign.Domain.Entities.Payments.PaymentTransaction>();

    // Glass enclosure module
    public DbSet<BrandVendor> GlassBrandVendors => Set<BrandVendor>();
    public DbSet<ClimateZone> ClimateZones => Set<ClimateZone>();
    public DbSet<ColorOption> GlassColorOptions => Set<ColorOption>();
    public DbSet<CoreAlign.Domain.Entities.GlassEnclosure.DiscountRule> GlassDiscountRules => Set<CoreAlign.Domain.Entities.GlassEnclosure.DiscountRule>();
    public DbSet<GlassEnclosureSettings> GlassEnclosureSettingsStore => Set<GlassEnclosureSettings>();
    public DbSet<FieldSurvey> GlassFieldSurveys => Set<FieldSurvey>();
    public DbSet<HardwareItem> GlassHardwareItems => Set<HardwareItem>();
    public DbSet<HardwareKit> GlassHardwareKits => Set<HardwareKit>();
    public DbSet<GlassNotificationLog> GlassNotificationLogs => Set<GlassNotificationLog>();
    public DbSet<GlassNotificationTemplate> GlassNotificationTemplates => Set<GlassNotificationTemplate>();
    public DbSet<ProfileItem> GlassProfileItems => Set<ProfileItem>();
    public DbSet<ProfileSystem> GlassProfileSystems => Set<ProfileSystem>();
    public DbSet<GlassProject> GlassProjects => Set<GlassProject>();
    public DbSet<GlassProjectAttachment> GlassProjectAttachments => Set<GlassProjectAttachment>();
    public DbSet<GlassProjectBOMLine> GlassProjectBOMLines => Set<GlassProjectBOMLine>();
    public DbSet<GlassProjectChangeLog> GlassProjectChangeLogs => Set<GlassProjectChangeLog>();
    public DbSet<GlassProjectCuttingPlan> GlassProjectCuttingPlans => Set<GlassProjectCuttingPlan>();
    public DbSet<GlassProjectOrderLink> GlassProjectOrderLinks => Set<GlassProjectOrderLink>();
    public DbSet<GlassProjectPanel> GlassProjectPanels => Set<GlassProjectPanel>();
    public DbSet<GlassProjectQuoteSnapshot> GlassProjectQuoteSnapshots => Set<GlassProjectQuoteSnapshot>();
    public DbSet<GlassProjectRun> GlassProjectRuns => Set<GlassProjectRun>();
    public DbSet<GlassProjectScene> GlassProjectScenes => Set<GlassProjectScene>();
    public DbSet<GlassProjectShareToken> GlassProjectShareTokens => Set<GlassProjectShareToken>();
    public DbSet<GlassType> GlassTypes => Set<GlassType>();
    public DbSet<GlassWorkOrder> GlassWorkOrders => Set<GlassWorkOrder>();
    public DbSet<GlassWorkOrderRevision> GlassWorkOrderRevisions => Set<GlassWorkOrderRevision>();
    public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
    public DbSet<ProjectTemplateReview> ProjectTemplateReviews => Set<ProjectTemplateReview>();
    public DbSet<ProjectTemplateInstall> ProjectTemplateInstalls => Set<ProjectTemplateInstall>();
    public DbSet<RunConnection> GlassRunConnections => Set<RunConnection>();
    public DbSet<WindZone> WindZones => Set<WindZone>();

    // F3.1 Warranty + Maintenance module
    public DbSet<WarrantyContract> WarrantyContracts => Set<WarrantyContract>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();

    // F3.2 Installation acceptance protocol (post-install QC checklist + customer signature)
    public DbSet<InstallationAcceptance> InstallationAcceptances => Set<InstallationAcceptance>();
    public DbSet<PunchListItem> PunchListItems => Set<PunchListItem>();

    // F4.1 Notification subsystem (multi-channel: email/sms/push/whatsapp/in-app)
    public DbSet<CoreAlign.Domain.Entities.Notifications.NotificationMessage> NotificationMessages => Set<CoreAlign.Domain.Entities.Notifications.NotificationMessage>();
    public DbSet<CoreAlign.Domain.Entities.Notifications.NotificationTemplate> NotificationTemplates => Set<CoreAlign.Domain.Entities.Notifications.NotificationTemplate>();
    public DbSet<CoreAlign.Domain.Entities.Notifications.NotificationPreference> NotificationPreferences => Set<CoreAlign.Domain.Entities.Notifications.NotificationPreference>();
    public DbSet<CoreAlign.Domain.Entities.Notifications.UserDeviceToken> UserDeviceTokens => Set<CoreAlign.Domain.Entities.Notifications.UserDeviceToken>();

    // F4.5 Whitelabel customization (tenant theme + multi-asset references)
    public DbSet<CoreAlign.Domain.Entities.Whitelabel.TenantTheme> TenantThemes => Set<CoreAlign.Domain.Entities.Whitelabel.TenantTheme>();
    public DbSet<CoreAlign.Domain.Entities.Whitelabel.TenantThemeAsset> TenantThemeAssets => Set<CoreAlign.Domain.Entities.Whitelabel.TenantThemeAsset>();

    // F4.6 BI / Reporting Advanced (dashboard widgets + saved reports + report runs audit)
    public DbSet<CoreAlign.Domain.Entities.Reporting.DashboardWidget> DashboardWidgets => Set<CoreAlign.Domain.Entities.Reporting.DashboardWidget>();
    public DbSet<CoreAlign.Domain.Entities.Reporting.SavedReport> SavedReports => Set<CoreAlign.Domain.Entities.Reporting.SavedReport>();
    public DbSet<CoreAlign.Domain.Entities.Reporting.ReportRun> ReportRuns => Set<CoreAlign.Domain.Entities.Reporting.ReportRun>();

    // F5.1 SSO — tenant-scoped SAML 2.0 / OIDC identity provider config + external user bindings.
    public DbSet<CoreAlign.Domain.Entities.Sso.TenantIdentityProvider> TenantIdentityProviders => Set<CoreAlign.Domain.Entities.Sso.TenantIdentityProvider>();
    public DbSet<CoreAlign.Domain.Entities.Sso.ExternalUserBinding> ExternalUserBindings => Set<CoreAlign.Domain.Entities.Sso.ExternalUserBinding>();

    public Guid CurrentTenantIdOrEmpty => _tenantContext.CurrentTenantId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreAlignDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        modelBuilder.ApplySoftDeleteFilters();
        modelBuilder.ApplySnakeCaseNaming();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchPendingDomainEventsAsync(cancellationToken);

        foreach (var entry in ChangeTracker.Entries<IHasConcurrencyToken>().Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.BumpConcurrencyToken();
        }

        var tenantId = _tenantContext.CurrentTenantId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty && tenantId.HasValue)
                {
                    entry.Entity.TenantId = tenantId.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchPendingDomainEventsAsync(CancellationToken cancellationToken)
    {
        const int maxIterations = 8;

        for (var i = 0; i < maxIterations; i++)
        {
            var pending = ChangeTracker
                .Entries<TenantEntity>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToArray();

            if (pending.Length == 0) return;

            var events = pending.SelectMany(e => e.DomainEvents).ToArray();
            foreach (var entity in pending) entity.ClearDomainEvents();

            foreach (var ev in events)
            {
                await _publisher.Publish(ev, cancellationToken);
            }
        }
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantProperty = Expression.Property(parameter, nameof(ITenantOwned.TenantId));
            var contextTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantIdOrEmpty));
            var body = Expression.Equal(tenantProperty, contextTenantId);
            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
