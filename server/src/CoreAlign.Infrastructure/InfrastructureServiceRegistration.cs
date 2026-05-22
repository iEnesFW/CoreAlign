using CoreAlign.Application.Common.Caching;
using CoreAlign.Application.Lookups;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using CoreAlign.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CoreAlign.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContextAccessor>();

        // Disable WS-Federation claim name mapping once at startup so JWT claim
        // names ("sub", "tenant_id", ...) reach handlers verbatim instead of being
        // rewritten to ClaimTypes.NameIdentifier-style URIs on every validation.
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connection = configuration.GetConnectionString("DefaultConnection");
        var isSqlite = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase);

        if (!isSqlite && string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set via user-secrets or environment variable; do not commit it to appsettings.json.");
        }

        // Tuned Npgsql connection string: explicit pool/command-timeout bounds and
        // multiplexing so a few slow tenants can't starve the pool. The values are
        // overridable from configuration (Database:MaxPoolSize, Database:CommandTimeoutSeconds).
        string BuildPostgresConnectionString()
        {
            var maxPool = configuration.GetValue<int?>("Database:MaxPoolSize") ?? 100;
            var minPool = configuration.GetValue<int?>("Database:MinPoolSize") ?? 1;
            var cmdTimeout = configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 30;
            var multiplex = configuration.GetValue<bool?>("Database:Multiplexing") ?? true;

            var builder = new NpgsqlConnectionStringBuilder(connection)
            {
                MaxPoolSize = maxPool,
                MinPoolSize = minPool,
                CommandTimeout = cmdTimeout,
                Multiplexing = multiplex,
                Timeout = configuration.GetValue<int?>("Database:ConnectTimeoutSeconds") ?? 15,
            };
            return builder.ConnectionString;
        }

        var postgresConn = isSqlite ? null : BuildPostgresConnectionString();

        void ConfigureDb(DbContextOptionsBuilder options)
        {
            if (isSqlite)
            {
                var sqliteConn = string.IsNullOrWhiteSpace(connection) || !connection.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                    ? "Data Source=corealign.db"
                    : connection;
                options.UseSqlite(sqliteConn);
            }
            else
            {
                options.UseNpgsql(postgresConn);
            }
        }

        // Single registration: the scoped DbContext resolves from the factory so we
        // no longer have AddDbContext + AddDbContextFactory both building the model.
        // We can't use AddPooledDbContextFactory because the context constructor
        // depends on scoped services (ITenantContext, IPublisher).
        services.AddDbContextFactory<CoreAlignDbContext>(ConfigureDb, lifetime: ServiceLifetime.Scoped);
        services.AddScoped<CoreAlignDbContext>(sp => sp
            .GetRequiredService<IDbContextFactory<CoreAlignDbContext>>()
            .CreateDbContext());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ILookupQueryService, LookupQueryService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ILoginAuditLogRepository, LoginAuditLogRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDashboardStatsRepository, DashboardStatsRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ICustomerTransactionRepository, CustomerTransactionRepository>();
        services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddScoped<ICustomerContactRepository, CustomerContactRepository>();
        services.AddScoped<IProductComponentRepository, ProductComponentRepository>();

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<ICustomerGroupRepository, CustomerGroupRepository>();
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<IPaymentTermRepository, PaymentTermRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();

        services.AddScoped<IStockItemRepository, StockItemRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IStockAllocationRepository, StockAllocationRepository>();
        services.AddScoped<IStockReasonCodeRepository, StockReasonCodeRepository>();
        services.AddScoped<ILotRepository, LotRepository>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ICustomerLedgerRepository, CustomerLedgerRepository>();
        services.AddScoped<IAccountingPeriodRepository, AccountingPeriodRepository>();
        services.AddScoped<ICustomerProductPriceRepository, CustomerProductPriceRepository>();
        services.AddScoped<IGLAccountRepository, GLAccountRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IVendorAddressRepository, VendorAddressRepository>();
        services.AddScoped<IVendorContactRepository, VendorContactRepository>();
        services.AddScoped<IVendorBankAccountRepository, VendorBankAccountRepository>();
        services.AddScoped<IVendorLedgerRepository, VendorLedgerRepository>();
        services.AddScoped<ITenantSettingRepository, TenantSettingRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IPricingService, PricingService>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        // Stateless and thread-safe — register as singleton to skip per-request allocation.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Cap the memory-cache footprint so a multi-tenant burst can't consume
        // unbounded heap. Each entry is registered with an explicit Size so this
        // limit is enforced (default Size is 0 — entries without Size bypass the cap).
        services.AddMemoryCache(opts =>
        {
            opts.SizeLimit = configuration.GetValue<long?>("Cache:MemorySizeLimit") ?? 10_000;
        });
        services.AddSingleton<IDashboardCacheService, DashboardCacheService>();
        services.AddSingleton<ILookupCacheService, LookupCacheService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwt = jwtOptionsAccessor.Value;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
