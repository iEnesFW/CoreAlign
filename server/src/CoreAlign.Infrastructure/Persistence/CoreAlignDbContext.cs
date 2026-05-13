using System.Linq.Expressions;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
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

    public Guid CurrentTenantIdOrEmpty => _tenantContext.CurrentTenantId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreAlignDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        modelBuilder.ApplySnakeCaseNaming();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchPendingDomainEventsAsync(cancellationToken);

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
