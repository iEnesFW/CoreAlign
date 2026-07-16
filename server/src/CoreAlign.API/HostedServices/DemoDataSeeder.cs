using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Inventory.Commands;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Purchasing;
using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Vendors.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Dev-only one-shot seeder. Bootstraps a demo tenant + admin login, then drives
/// the real MediatR command pipeline so every derived artifact (sub-ledgers, GL
/// journal entries via the outbox, stock movements, dashboard aggregates) is
/// produced exactly as it would be in normal use — i.e. mutually consistent.
/// Idempotent: skips entirely once the demo admin exists.
/// </summary>
public class DemoDataSeeder : BackgroundService
{
    public const string AdminEmail = "admin@demo.local";
    public const string AdminPassword = "Demo!2345";
    public const string CustomerOwnerEmail = "acme.admin@demo.local";
    public const string DealerOwnerEmail = "bayi@demo.local";
    public const string B2BPassword = "Demo!2345";

    // Dev developer'a ekstra tam-yetkili admin hesap (kullanici talebi).
    public const string EnesEmail = "enes.colak996@gmail.com";
    public const string EnesPassword = "Asdqwe123";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public DemoDataSeeder(
        IServiceScopeFactory scopeFactory,
        ILogger<DemoDataSeeder> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    internal static bool IsSeedingEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        var envVar = Environment.GetEnvironmentVariable("DEMO_DATA");
        var envFlag = string.Equals(envVar, "true", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(envVar, "1", StringComparison.Ordinal);
        var configFlag = configuration.GetValue<bool?>("DemoData:Enabled") == true;
        var requested = envFlag || configFlag;

        if (environment.IsProduction() && requested)
        {
            throw new InvalidOperationException(
                "DemoDataSeeder refused to run: demo data seeding is forbidden in Production. " +
                "Unset DEMO_DATA and DemoData:Enabled before deploying.");
        }
        if (environment.IsProduction()) return false;

        return requested;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsSeedingEnabled(_configuration, _environment))
        {
            _logger.LogInformation(
                "Demo data seeding skipped (DEMO_DATA={Env}, DemoData:Enabled={Cfg}).",
                Environment.GetEnvironmentVariable("DEMO_DATA"),
                _configuration.GetValue<bool?>("DemoData:Enabled"));
            return;
        }

        try
        {
            await SeedAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            // Never crash app startup because demo seeding failed.
            _logger.LogError(ex, "Demo data seeding failed.");
        }
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<IUserRepository>();

        // Catalog seeding is idempotent and runs every startup so a fresh module
        // surfaces in dev without wiping the database.
        await SeedModuleCatalogAsync(sp, ct);
        await GlassEnclosureSeeder.SeedGlobalAsync(sp, ct);
        await ProjectTemplateSeeder.SeedSystemTemplatesAsync(sp, ct);
        await PayrollParametersSeed.SeedGlobalAsync(sp, ct);

        if (await users.ExistsByEmailAsync(AdminEmail, ct))
        {
            _logger.LogInformation("Demo data already present ({Email}); skipping seed.", AdminEmail);
            return;
        }

        var tenants = sp.GetRequiredService<ITenantRepository>();
        var roles = sp.GetRequiredService<IRoleRepository>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var sequences = sp.GetRequiredService<IDocumentSequenceRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var mediator = sp.GetRequiredService<IMediator>();
        var now = DateTime.UtcNow;

        // 1) Tenant + admin user (not tenant-owned → no ambient tenant needed yet).
        var slug = await UniqueSlugAsync(tenants, "demo", ct);
        var tenant = new Tenant("Demo Ticaret A.Ş.", slug);
        await tenants.AddAsync(tenant, ct);

        // Global FX rates (TenantId = Guid.Empty) — UI Navbar/FxRateBadge bunlari okuyor.
        var fxRepo = sp.GetRequiredService<CoreAlign.Application.Treasury.Fx.IExchangeRateRepository>();
        var fxToday = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var demoRates = new[] {
            ("USD", 39.50m), ("EUR", 42.85m), ("GBP", 50.10m), ("CHF", 44.20m), ("JPY", 0.265m),
        };
        foreach (var (currency, rate) in demoRates)
        {
            await fxRepo.AddAsync(new CoreAlign.Domain.Entities.Treasury.ExchangeRate
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.Empty,
                Currency = currency,
                RateAgainstTry = rate,
                ValidOnDate = fxToday,
                Source = "DEMO",
                FetchedAtUtc = DateTime.UtcNow,
            }, ct);
        }
        await uow.SaveChangesAsync(ct);

        var admin = new User(tenant.Id, "demoadmin", AdminEmail, hasher.Hash(AdminPassword))
        {
            FirstName = "Demo",
            LastName = "Yönetici",
            IsActive = true,
            IsEmailConfirmed = true,
        };
        await users.AddAsync(admin, ct);
        var adminRole = await roles.GetByNameAsync("TenantAdmin", ct);
        if (adminRole is not null)
        {
            admin.UserRoles.Add(new UserRole(admin.Id, adminRole.Id));
        }

        // Developer'in tam-yetkili kisisel hesabi — demo tenant ile ayni admin role.
        var enes = new User(tenant.Id, "enescolak", EnesEmail, hasher.Hash(EnesPassword))
        {
            FirstName = "Enes",
            LastName = "Çolak",
            IsActive = true,
            IsEmailConfirmed = true,
        };
        await users.AddAsync(enes, ct);
        if (adminRole is not null)
        {
            enes.UserRoles.Add(new UserRole(enes.Id, adminRole.Id));
        }

        await uow.SaveChangesAsync(ct);

        // 2) Everything else runs as the demo tenant.
        using (TenantContextAccessor.PushTenant(tenant.Id))
        {
            // Document sequences consumed by sales/invoice/shipment/payment handlers
            // (PO / vendor-payment / journal sequences auto-create themselves).
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.CustomerCode, "CST", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.ProductSku, "SKU", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.GlassProjectCode, "GP", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.PurchaseOrderNumber, "PO", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.VendorPaymentNumber, "VP", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.JournalNumber, "JE", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.PurchaseRequisitionNumber, "PR", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.OrderNumber, "ORD", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.InvoiceNumber, "INV", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.CreditNoteNumber, "CRN", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.DebitNoteNumber, "DBN", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.ShipmentNumber, "SHP", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.PaymentNumber, "PAY", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.SubscriptionOrderNumber, "SUB", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.QuoteNumber, "QUO", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.ReturnRequestNumber, "RTN", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.StockCountNumber, "STC", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.GoodsReceiptNumber, "GRN", now.Year), ct);
            await sequences.AddAsync(new DocumentSequence(DocumentSequenceType.ProductionJobNumber, "JOB", now.Year), ct);
            await uow.SaveChangesAsync(ct);

            // Chart of accounts FIRST — GL auto-posting silently no-ops without it.
            await mediator.Send(new SeedTurkishChartOfAccountsCommand(), ct);
            await mediator.Send(new SeedStandardUnitsOfMeasureCommand(), ct);

            // Master data
            var wh = await mediator.Send(new CreateWarehouseCommand("WH-01", "Ana Depo", WarehouseType.Main, IsDefault: true), ct);
            var tax = await mediator.Send(new CreateTaxRateCommand("KDV20", "Hesaplanan KDV %20", 20m), ct);
            var term = await mediator.Send(new CreatePaymentTermCommand("NET30", "30 Gün Vade", 30), ct);
            var priceList = await mediator.Send(new CreatePriceListCommand("STD", "Standart Fiyat Listesi", "TRY", IsDefault: true), ct);
            var brand = await mediator.Send(new CreateBrandCommand("ACME", "Acme"), ct);
            var category = await mediator.Send(new CreateProductCategoryCommand("GEN", "Genel Ürünler"), ct);
            var group = await mediator.Send(new CreateCustomerGroupCommand("BAYI", "Bayiler", DefaultPriceListId: priceList.Id), ct);

            // Products (sku, name, sell price, cost)
            var productSpecs = new[]
            {
                ("SKU-1001", "Çelik Vida M6 (Kutu)", 120m, 70m),
                ("SKU-1002", "Alüminyum Profil 2m", 340m, 210m),
                ("SKU-1003", "Endüstriyel Yağ 5L", 480m, 300m),
                ("SKU-1004", "Conta Seti", 95m, 48m),
                ("SKU-1005", "Rulman 6203", 65m, 32m),
            };
            var products = new List<(Guid Id, decimal Cost, decimal Price)>();
            foreach (var (sku, name, price, cost) in productSpecs)
            {
                var p = await mediator.Send(new CreateProductCommand(
                    Sku: sku, Name: name, BrandId: brand.Id, CategoryId: category.Id,
                    Price: price, ListPrice: price, StandardCost: cost, Currency: "TRY",
                    TaxRateId: tax.Id), ct);
                products.Add((p.Id, cost, price));
                // Opening stock at the default warehouse (200 units @ cost).
                await mediator.Send(new ReceiveStockCommand(
                    p.Id, wh.Id, 200m, cost, null, null, null, "OPENING", "Açılış stoğu"), ct);
            }

            // Customers — Acme Holding is the anchor for the B2B demo seed.
            var customerSpecs = new[]
            {
                ("Acme Holding", "acme@demo.local"),
                ("Yıldız Mühendislik Ltd.", "yildiz@demo.local"),
                ("Marmara İnşaat A.Ş.", "marmara@demo.local"),
                ("Ege Otomotiv San.", "ege@demo.local"),
                ("Anadolu Tarım Koop.", "anadolu@demo.local"),
            };
            var customers = new List<Guid>();
            foreach (var (name, email) in customerSpecs)
            {
                var c = await mediator.Send(new CreateCustomerCommand(
                    Name: name, Email: email, DefaultCurrency: "TRY",
                    PaymentTermsId: term.Id, PriceListId: priceList.Id, CustomerGroupId: group.Id,
                    CreditLimit: 250_000m), ct);
                customers.Add(c.Id);
            }
            var acmeCustomerId = customers[0];

            // Vendors
            var vendorSpecs = new[]
            {
                ("Bağlantı Elemanları Tic.", "baglanti@demo.local"),
                ("Metal Profil Sanayi", "metalprofil@demo.local"),
                ("Kimya Tedarik Ltd.", "kimya@demo.local"),
            };
            var vendors = new List<Guid>();
            foreach (var (name, email) in vendorSpecs)
            {
                var v = await mediator.Send(new CreateVendorCommand(
                    Name: name, Type: "Business", Email: email, DefaultCurrency: "TRY",
                    PaymentTermsId: term.Id), ct);
                vendors.Add(v.Id);
            }

            await SeedPurchasingAsync(mediator, vendors, products, admin.Id, now, ct);
            await SeedSalesAsync(mediator, customers, products, wh.Id, tax.Id, admin.Id, now, ct);

            // Sonraki seed adimlarini izole et — biri patlasa digerleri etkilenmesin.
            // GlassEnclosureSeeder: hardware kit duplicate FK/constraint bug; debug sonra.
            try { await GlassEnclosureSeeder.SeedTenantAsync(sp, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "GlassEnclosure tenant seed skipped (non-critical)"); }

            try { await GrantAllModulesAsync(sp, now, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "GrantAllModules skipped (non-critical)"); }

            try { await SeedB2BIdentityAsync(sp, tenant.Id, admin.Id, acmeCustomerId, customers, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "B2B identity seed skipped (non-critical)"); }
        }

        _logger.LogInformation(
            "Demo data seeded for tenant '{Slug}'. Login: {Email} / {Password}",
            tenant.Slug, AdminEmail, AdminPassword);
    }

    private async Task SeedPurchasingAsync(
        IMediator mediator, List<Guid> vendors, List<(Guid Id, decimal Cost, decimal Price)> products,
        Guid adminId, DateTime now, CancellationToken ct)
    {
        // ~9 purchase flows across the last few weeks; payment coverage cycles full/partial/none
        // so the payables balance and aging buckets are populated.
        const int poCount = 9;
        var coverage = new[] { 1.0m, 0.4m, 0m };
        for (var i = 0; i < poCount; i++)
        {
            try
            {
                var vendorId = vendors[i % vendors.Count];
                var product = products[i % products.Count];
                var qty = 40m + (i % 5) * 10m;
                var orderDate = now.AddDays(-24 + i * 2);
                var subtotal = qty * product.Cost;
                var taxAmount = Math.Round(subtotal * 0.20m, 2);
                var total = subtotal + taxAmount;

                var po = await mediator.Send(new CreatePurchaseOrderCommand(
                    VendorId: vendorId, OrderDate: orderDate, Currency: "TRY",
                    Lines: new List<PurchaseOrderLineInput> { new(product.Id, qty, product.Cost, 20m) }), ct);
                await mediator.Send(new SubmitPurchaseOrderCommand(po.Id), ct);
                await mediator.Send(new ApprovePurchaseOrderCommand(po.Id, adminId), ct);
                await mediator.Send(new ReceivePurchaseOrderCommand(
                    po.Id, new List<ReceiptLineInput> { new(po.Lines[0].Id, qty) },
                    IdempotencyKey: Guid.NewGuid().ToString("N")), ct);

                var bill = await mediator.Send(new CreateVendorBillCommand(
                    VendorId: vendorId, BillNumber: $"VB-{now.Year}-{i + 1:D4}", BillDate: orderDate.AddDays(2),
                    Currency: "TRY", Subtotal: subtotal, TaxAmount: taxAmount,
                    DueDate: orderDate.AddDays(32), PurchaseOrderId: po.Id), ct);
                await mediator.Send(new PostVendorBillCommand(bill.Id), ct);

                var pay = Math.Round(total * coverage[i % coverage.Length], 2);
                if (pay > 0m)
                {
                    var paymentDate = orderDate.AddDays(7);
                    if (paymentDate > now) { paymentDate = now; }
                    await mediator.Send(new CreateVendorPaymentCommand(
                        VendorId: vendorId, Amount: pay, PaymentDate: paymentDate,
                        Currency: "TRY", Method: "BankTransfer", VendorBillId: bill.Id), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Demo purchasing flow {Index} failed.", i);
            }
        }
    }

    private async Task SeedSalesAsync(
        IMediator mediator, List<Guid> customers, List<(Guid Id, decimal Cost, decimal Price)> products,
        Guid warehouseId, Guid taxRateId, Guid adminId, DateTime now, CancellationToken ct)
    {
        // ~30 orders spread across the last 30 days, cycling the demo customers/products.
        // Status mix (by i % 10) so "orders by status" has variety:
        //   0 -> Draft, 1 -> Submitted, 2 -> Approved, 3 -> Shipped (no invoice),
        //   4..9 -> fully invoiced. Payment mix (by i % 3): full / half / unpaid.
        const int orderCount = 30;
        for (var i = 0; i < orderCount; i++)
        {
            try
            {
                var customerId = customers[i % customers.Count];
                var product = products[i % products.Count];
                var qty = 3m + (i % 9);
                var orderDate = now.AddDays(-(orderCount - 1 - i)); // oldest first, newest = today
                var order = await mediator.Send(new CreateOrderCommand(
                    OrderNumber: $"ORD-{now.Year}-{i + 1:D4}",
                    CustomerId: customerId,
                    OrderDate: orderDate,
                    Currency: "TRY",
                    Notes: null,
                    Lines: new List<OrderLineInput>
                    {
                        new(product.Id, qty, product.Price, TaxRatePercent: 20m, TaxRateId: taxRateId),
                    }), ct);

                var bucket = i % 10;
                if (bucket == 0)
                {
                    continue; // leave as Draft
                }

                await mediator.Send(new SubmitOrderCommand(order.Id), ct);
                if (bucket == 1)
                {
                    continue; // leave as Submitted
                }

                await mediator.Send(new ApproveOrderCommand(order.Id, adminId), ct);
                if (bucket == 2)
                {
                    continue; // leave as Approved
                }

                await mediator.Send(new AllocateOrderCommand(order.Id, warehouseId), ct);

                var shipment = await mediator.Send(new CreateShipmentCommand(
                    order.Id, warehouseId,
                    order.Lines.Select(l => new ShipmentLineInput(l.Id, l.Quantity)).ToList()), ct);
                await mediator.Send(new PickShipmentCommand(shipment.Id), ct);
                await mediator.Send(new PackShipmentCommand(shipment.Id), ct);
                await mediator.Send(new DispatchShipmentCommand(
                    shipment.Id, "Aras Kargo", $"TRK{now.Year}{i + 1:D4}", null, 0m), ct);

                if (bucket == 3)
                {
                    continue; // shipped but not invoiced
                }

                var invoice = await mediator.Send(new GenerateInvoiceFromOrderCommand(order.Id, 30), ct);

                var fraction = (i % 3) switch { 0 => 1.0m, 1 => 0.5m, _ => 0m };
                var amount = Math.Round(invoice.Total * fraction, 2);
                if (amount > 0m)
                {
                    var paymentDate = orderDate.AddDays(5);
                    if (paymentDate > now) { paymentDate = now; }
                    await mediator.Send(new CreatePaymentCommand(
                        CustomerId: customerId,
                        PaymentDate: paymentDate,
                        Method: PaymentMethod.BankTransfer,
                        Amount: amount,
                        Currency: "TRY",
                        AutoConfirm: true,
                        Applications: new List<PaymentApplyLine> { new(invoice.Id, amount) }), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Demo sales flow {Index} failed.", i);
            }
        }
    }

    private static readonly (string Code, string Name, string? Category, string? IconKey, int SortOrder, bool IsCore)[] ModuleCatalog =
    {
        ("Dashboard", "Dashboard", "Insights", "layout-dashboard", 0, true),
        ("Billing", "Billing & Subscriptions", "Administration", "credit-card", 1, true),
        ("Customers", "Customers", "Sales", "users", 10, false),
        ("Sales", "Sales", "Sales", "shopping-cart", 11, false),
        ("Vendors", "Vendors", "Operations", "truck", 20, false),
        ("Purchasing", "Purchasing", "Operations", "package", 21, false),
        ("Products", "Products", "Catalog", "box", 30, false),
        ("Inventory", "Inventory", "Operations", "warehouse", 31, false),
        ("Accounting", "Accounting", "Finance", "calculator", 40, false),
        ("Reports", "Reports", "Insights", "bar-chart", 50, false),
        ("GlassEnclosure", "Cam Mekan", "Manufacturing", "square-stack", 60, false),
        ("Settings", "Settings", "Administration", "settings", 90, false),
    };

    private static readonly (string Code, string DisplayLabel, int DurationDays, decimal Price)[] DefaultPlans =
    {
        ("Monthly", "Aylık", 30, 99m),
        ("Yearly", "Yıllık", 365, 999m),
    };

    private async Task SeedModuleCatalogAsync(IServiceProvider sp, CancellationToken ct)
    {
        var modules = sp.GetRequiredService<IModuleRepository>();
        var plans = sp.GetRequiredService<IModulePricePlanRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var spec in ModuleCatalog)
        {
            var existing = await modules.GetByCodeAsync(spec.Code, ct);
            if (existing is null)
            {
                var module = new Domain.Entities.Module(spec.Code, spec.Name, description: null, spec.Category, spec.IconKey, spec.SortOrder, isActive: true, isCore: spec.IsCore);
                await modules.AddAsync(module, ct);
                anyChange = true;
            }
        }
        if (anyChange) await uow.SaveChangesAsync(ct);

        // Re-load with ids assigned, then seed price plans (core modules need none).
        var allModules = await modules.ListAsync(activeOnly: false, ct);
        foreach (var module in allModules.Where(m => !m.IsCore))
        {
            var existingPlans = await plans.ListByModuleAsync(module.Id, activeOnly: false, ct);
            var existingByCode = existingPlans.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < DefaultPlans.Length; i++)
            {
                var spec = DefaultPlans[i];
                if (existingByCode.ContainsKey(spec.Code)) continue;
                var plan = new ModulePricePlan(module.Id, spec.Code, spec.DisplayLabel, spec.DurationDays, spec.Price, "TRY", isActive: true, sortOrder: i);
                await plans.AddAsync(plan, ct);
                anyChange = true;
            }
        }
        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private async Task GrantAllModulesAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var modules = sp.GetRequiredService<IModuleRepository>();
        var tenantModules = sp.GetRequiredService<ITenantModuleRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var dbContext = sp.GetRequiredService<CoreAlignDbContext>();

        var allModules = await modules.ListAsync(activeOnly: false, ct);
        var existingByModuleId = await dbContext.TenantModules.AsNoTracking().ToDictionaryAsync(t => t.ModuleId, ct);

        foreach (var module in allModules)
        {
            if (existingByModuleId.ContainsKey(module.Id)) continue;
            var endUtc = module.IsCore ? (DateTime?)null : now.AddDays(365);
            var grant = new TenantModule(module.Id, now, endUtc, TenantModuleSource.Granted, "Demo seeding");
            await tenantModules.AddAsync(grant, ct);
        }
        await uow.SaveChangesAsync(ct);
    }

    private async Task SeedB2BIdentityAsync(
        IServiceProvider sp,
        Guid tenantId,
        Guid adminId,
        Guid acmeCustomerId,
        List<Guid> allCustomers,
        CancellationToken ct)
    {
        var users = sp.GetRequiredService<IUserRepository>();
        var customerUsers = sp.GetRequiredService<ICustomerUserRepository>();
        var dealerAccounts = sp.GetRequiredService<IDealerAccountRepository>();
        var dealerUsers = sp.GetRequiredService<IDealerUserRepository>();
        var links = sp.GetRequiredService<IDealerCustomerLinkRepository>();
        var roles = sp.GetRequiredService<IRoleRepository>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var defaultRole = await roles.GetByNameAsync("User", ct);
        var changed = false;

        var customerOwnerUser = await users.GetByEmailAsync(CustomerOwnerEmail, ct);
        if (customerOwnerUser is null)
        {
            customerOwnerUser = new User(tenantId, "acme.admin", CustomerOwnerEmail, hasher.Hash(B2BPassword))
            {
                FirstName = "Acme",
                LastName = "Owner",
                IsActive = true,
                IsEmailConfirmed = true,
            };
            if (defaultRole is not null)
            {
                customerOwnerUser.UserRoles.Add(new UserRole(customerOwnerUser.Id, defaultRole.Id));
            }
            await users.AddAsync(customerOwnerUser, ct);
            changed = true;
        }

        var dealerOwnerUser = await users.GetByEmailAsync(DealerOwnerEmail, ct);
        if (dealerOwnerUser is null)
        {
            dealerOwnerUser = new User(tenantId, "bayi.demo", DealerOwnerEmail, hasher.Hash(B2BPassword))
            {
                FirstName = "Demo",
                LastName = "Bayi",
                IsActive = true,
                IsEmailConfirmed = true,
            };
            if (defaultRole is not null)
            {
                dealerOwnerUser.UserRoles.Add(new UserRole(dealerOwnerUser.Id, defaultRole.Id));
            }
            await users.AddAsync(dealerOwnerUser, ct);
            changed = true;
        }

        if (changed) await uow.SaveChangesAsync(ct);

        var existingCustomerMembership = await customerUsers.GetByUserAndCustomerAsync(customerOwnerUser.Id, acmeCustomerId, ct);
        if (existingCustomerMembership is null)
        {
            var membership = new CustomerUser(customerOwnerUser.Id, acmeCustomerId, CustomerMembershipRole.CustomerOwner, adminId);
            await customerUsers.AddAsync(membership, ct);
            changed = true;
        }

        var dealer = await dealerAccounts.GetByCodeAsync("BAYI-001", ct);
        if (dealer is null)
        {
            dealer = new DealerAccount(
                code: "BAYI-001",
                name: "Demo Bayi",
                createdByUserId: adminId,
                legalName: "Demo Bayi Ticaret Ltd.",
                email: "info@demobayi.local",
                phone: "+90 555 010 9999");
            await dealerAccounts.AddAsync(dealer, ct);
            changed = true;
        }

        if (changed) await uow.SaveChangesAsync(ct);

        var existingDealerMembership = await dealerUsers.GetByUserAndDealerAsync(dealerOwnerUser.Id, dealer.Id, ct);
        if (existingDealerMembership is null)
        {
            var membership = new DealerUser(dealerOwnerUser.Id, dealer.Id, DealerMembershipRole.DealerOwner, adminId);
            await dealerUsers.AddAsync(membership, ct);
            changed = true;
        }

        var linkCandidateCustomerIds = new List<Guid> { acmeCustomerId };
        if (allCustomers.Count > 1) linkCandidateCustomerIds.Add(allCustomers[1]);

        foreach (var customerId in linkCandidateCustomerIds.Distinct())
        {
            var existingLink = await links.GetByDealerAndCustomerAsync(dealer.Id, customerId, ct);
            if (existingLink is null)
            {
                await links.AddAsync(new DealerCustomerLink(dealer.Id, customerId, adminId), ct);
                changed = true;
            }
        }

        if (changed) await uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "B2B identity demo seeded. Customer owner: {CustomerOwner}, Dealer owner: {DealerOwner}, Password: {Password}",
            CustomerOwnerEmail, DealerOwnerEmail, B2BPassword);
    }

    private static async Task<string> UniqueSlugAsync(ITenantRepository tenants, string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var attempt = 0;
        while (await tenants.SlugExistsAsync(slug, ct))
        {
            attempt++;
            slug = $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(40, baseSlug.Length + 7)];
            if (attempt > 5) break;
        }
        return slug;
    }
}
