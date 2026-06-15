using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests.Infrastructure;

internal sealed class IntegrationSeed
{
    private readonly IServiceProvider _root;

    public IntegrationSeed(IServiceProvider root)
    {
        _root = root;
    }

    public async Task<TenantFixture> SeedTenantAsync(string tenantName, string tenantSlug)
    {
        await using var scope = _root.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var roles = sp.GetRequiredService<IRoleRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var tenant = new Tenant(tenantName, tenantSlug);
        db.Tenants.Add(tenant);

        var tenantAdminRole = await GetOrCreateRoleAsync(db, "TenantAdmin");
        var userRole = await GetOrCreateRoleAsync(db, "User");

        await db.SaveChangesAsync();

        var adminEmail = $"admin@{tenantSlug}.local";
        var admin = new User(tenant.Id, $"admin-{tenantSlug}", adminEmail, hasher.Hash("Test!2345"))
        {
            FirstName = "Tenant",
            LastName = "Admin",
            IsActive = true,
            IsEmailConfirmed = true,
        };
        admin.UserRoles.Add(new UserRole(admin.Id, tenantAdminRole.Id));
        db.Users.Add(admin);

        var customerUserEmail = $"customer@{tenantSlug}.local";
        var customerUserIdentity = new User(tenant.Id, $"cust-{tenantSlug}", customerUserEmail, hasher.Hash("Test!2345"))
        {
            FirstName = "Customer",
            LastName = "User",
            IsActive = true,
            IsEmailConfirmed = true,
        };
        customerUserIdentity.UserRoles.Add(new UserRole(customerUserIdentity.Id, userRole.Id));
        db.Users.Add(customerUserIdentity);

        var dealerUserEmail = $"dealer@{tenantSlug}.local";
        var dealerUserIdentity = new User(tenant.Id, $"deal-{tenantSlug}", dealerUserEmail, hasher.Hash("Test!2345"))
        {
            FirstName = "Dealer",
            LastName = "User",
            IsActive = true,
            IsEmailConfirmed = true,
        };
        dealerUserIdentity.UserRoles.Add(new UserRole(dealerUserIdentity.Id, userRole.Id));
        db.Users.Add(dealerUserIdentity);

        await db.SaveChangesAsync();

        using (TenantContextAccessor.PushTenant(tenant.Id))
        {
            var customer = new Customer(
                name: $"Customer-{tenantSlug}",
                type: CustomerType.Business,
                code: $"C-{tenantSlug}",
                email: $"acct@{tenantSlug}.local",
                defaultCurrency: "TRY");
            customer.TenantId = tenant.Id;
            db.Customers.Add(customer);

            var dealer = new DealerAccount(
                code: $"D-{tenantSlug}",
                name: $"Dealer-{tenantSlug}",
                createdByUserId: admin.Id,
                email: $"deal@{tenantSlug}.local");
            dealer.TenantId = tenant.Id;
            db.DealerAccounts.Add(dealer);

            var product1 = new Product(sku: $"P1-{tenantSlug}", name: $"Product 1 {tenantSlug}", price: 100m);
            product1.TenantId = tenant.Id;
            var product2 = new Product(sku: $"P2-{tenantSlug}", name: $"Product 2 {tenantSlug}", price: 200m);
            product2.TenantId = tenant.Id;
            db.Products.Add(product1);
            db.Products.Add(product2);

            await db.SaveChangesAsync();

            var customerMembership = new CustomerUser(
                userId: customerUserIdentity.Id,
                customerId: customer.Id,
                role: CustomerMembershipRole.CustomerOwner,
                invitedByUserId: admin.Id);
            customerMembership.TenantId = tenant.Id;
            db.CustomerUsers.Add(customerMembership);

            var dealerMembership = new DealerUser(
                userId: dealerUserIdentity.Id,
                dealerAccountId: dealer.Id,
                role: DealerMembershipRole.DealerOwner,
                invitedByUserId: admin.Id);
            dealerMembership.TenantId = tenant.Id;
            db.DealerUsers.Add(dealerMembership);

            var dealerCustomerLink = new DealerCustomerLink(
                dealerAccountId: dealer.Id,
                customerId: customer.Id,
                assignedByUserId: admin.Id);
            dealerCustomerLink.TenantId = tenant.Id;
            db.DealerCustomerLinks.Add(dealerCustomerLink);

            var order = new Order(
                orderNumber: $"ORD-{tenantSlug}-001",
                customerId: customer.Id,
                orderDate: DateTime.UtcNow,
                currency: "TRY",
                notes: "Seeded for tests");
            order.TenantId = tenant.Id;
            db.Orders.Add(order);

            var invoice = new Invoice(
                invoiceNumber: $"INV-{tenantSlug}-001",
                customerId: customer.Id,
                customerNameSnapshot: customer.Name,
                currency: "TRY");
            invoice.TenantId = tenant.Id;
            db.Invoices.Add(invoice);

            var payment = new Payment(
                paymentNumber: $"PAY-{tenantSlug}-001",
                customerId: customer.Id,
                customerNameSnapshot: customer.Name,
                direction: PaymentDirection.CustomerReceipt,
                paymentDate: DateTime.UtcNow,
                method: PaymentMethod.BankTransfer,
                amount: 100m,
                currency: "TRY");
            payment.TenantId = tenant.Id;
            db.Payments.Add(payment);

            var notificationToCustomer = new Notification(
                recipientUserId: customerUserIdentity.Id,
                actorUserId: admin.Id,
                type: "OrderCreated",
                entityType: "Order",
                entityId: order.Id,
                title: "Test order",
                body: "Body");
            notificationToCustomer.TenantId = tenant.Id;
            db.Notifications.Add(notificationToCustomer);

            var notificationToDealer = new Notification(
                recipientUserId: dealerUserIdentity.Id,
                actorUserId: admin.Id,
                type: "InvoiceIssued",
                entityType: "Invoice",
                entityId: invoice.Id,
                title: "Test invoice",
                body: "Body");
            notificationToDealer.TenantId = tenant.Id;
            db.Notifications.Add(notificationToDealer);

            await db.SaveChangesAsync();

            return new TenantFixture
            {
                TenantId = tenant.Id,
                TenantSlug = tenantSlug,
                TenantAdminUserId = admin.Id,
                TenantAdminEmail = adminEmail,
                CustomerId = customer.Id,
                CustomerUserId = customerUserIdentity.Id,
                CustomerUserEmail = customerUserEmail,
                DealerAccountId = dealer.Id,
                DealerUserId = dealerUserIdentity.Id,
                DealerUserEmail = dealerUserEmail,
                Product1Id = product1.Id,
                Product2Id = product2.Id,
                OrderId = order.Id,
                InvoiceId = invoice.Id,
                PaymentId = payment.Id,
                NotificationCustomerId = notificationToCustomer.Id,
                NotificationDealerId = notificationToDealer.Id,
            };
        }
    }

    private static async Task<Role> GetOrCreateRoleAsync(CoreAlignDbContext db, string name)
    {
        var existing = await db.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (existing is not null) return existing;
        var role = new Role { Name = name };
        db.Roles.Add(role);
        return role;
    }
}
