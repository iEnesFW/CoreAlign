using CoreAlign.Application.Common.Caching;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Options;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using CoreAlign.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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

        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connection = configuration.GetConnectionString("DefaultConnection");
        var isSqlite = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase);

        if (!isSqlite && string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set via user-secrets or environment variable; do not commit it to appsettings.json.");
        }

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
                options.UseNpgsql(connection);
            }
        }

        services.AddDbContext<CoreAlignDbContext>(ConfigureDb);
        services.AddDbContextFactory<CoreAlignDbContext>(ConfigureDb, lifetime: ServiceLifetime.Scoped);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
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
        services.AddScoped<IPricingService, PricingService>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddMemoryCache();
        services.AddSingleton<IDashboardCacheService, DashboardCacheService>();

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
