using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Common.Caching;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Application.Lookups;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Infrastructure.EInvoice;
using CoreAlign.Infrastructure.Options;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Providers;
using CoreAlign.Infrastructure.Providers.EFatura;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Infrastructure.Providers.EFatura.Foriba;
using CoreAlign.Infrastructure.Providers.EFatura.GibPortal;
using CoreAlign.Infrastructure.Providers.EFatura.Nilvera;
using CoreAlign.Infrastructure.Providers.Payment;
using CoreAlign.Infrastructure.Providers.Payment.Iyzico;
using CoreAlign.Infrastructure.Providers.Payment.PayTR;
using CoreAlign.Infrastructure.Providers.Payment.Stripe;
using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Infrastructure.Catalog;
using CoreAlign.Infrastructure.Repositories;
using CoreAlign.Infrastructure.Repositories.Pricing;
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
        services.AddSprint9GroupCInfrastructure();
        services.AddSprint10GroupCInfrastructure();

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
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IProductSubstituteRepository, ProductSubstituteRepository>();
        services.AddScoped<CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.IBomRecomputedOutbox,
            CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.BomRecomputedOutbox>();
        services.AddScoped<CoreAlign.Application.Common.Outbox.IOutboxMessageHandler,
            CoreAlign.Application.GlassEnclosure.WorkOrderRevisions.BomRecomputedOutboxHandler>();
        services.AddScoped<CoreAlign.Application.Common.Outbox.IOutboxMessageHandler,
            CoreAlign.Application.Providers.EFatura.Handlers.EFaturaWebhookEventHandler>();
        services.AddScoped<CoreAlign.Application.Common.Outbox.IOutboxMessageHandler,
            CoreAlign.Application.Providers.Payment.Handlers.PaymentWebhookEventHandler>();

        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IPortalScopeService, PortalScopeService>();
        services.AddScoped<CoreAlign.Application.Common.ICurrentCustomerAccessor, CurrentCustomerAccessor>();
        services.AddScoped<IUserMembershipService, UserMembershipService>();
        services.AddScoped<IB2BAuthorizationService, B2BAuthorizationService>();
        services.AddScoped<CoreAlign.Application.Documents.IDocumentService, CoreAlign.Application.Documents.DocumentService>();
        services.AddScoped<CoreAlign.Application.Documents.IDocumentRenderer, CoreAlign.Infrastructure.Documents.QuestPdfDocumentRenderer>();
        services.AddScoped<CoreAlign.Application.Reports.Common.IReportRenderer, CoreAlign.Infrastructure.Reports.QuestPdfReportRenderer>();
        services.AddScoped<CoreAlign.Application.Reports.Common.IReportRenderer, CoreAlign.Infrastructure.Reports.ClosedXmlReportRenderer>();
        services.AddScoped<CoreAlign.Application.Reports.Common.IReportFileFactory, CoreAlign.Application.Reports.Common.ReportFileFactory>();
        services.AddScoped<CoreAlign.Application.Reports.Common.IReportDataReader, CoreAlign.Infrastructure.Reports.ReportDataReader>();
        services.AddScoped<IDealerCommissionLedgerRepository, DealerCommissionLedgerEntryRepository>();
        services.AddScoped<CoreAlign.Application.Jobs.IMaintenanceDataAccess, MaintenanceDataAccess>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddSingleton<IAuditFieldRedactor, DefaultAuditFieldRedactor>();
        services.AddScoped<IOutboxSignal, OutboxSignal>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IWorkOrderRevisionService, WorkOrderRevisionService>();

        services.AddScoped<IGlassProjectRepository, GlassProjectRepository>();
        services.AddScoped<IGlassProjectRunRepository, GlassProjectRunRepository>();
        services.AddScoped<IRunConnectionRepository, RunConnectionRepository>();
        services.AddScoped<IGlassProjectPanelRepository, GlassProjectPanelRepository>();
        services.AddScoped<IGlassProjectSceneRepository, GlassProjectSceneRepository>();
        services.AddScoped<IGlassProjectChangeLogRepository, GlassProjectChangeLogRepository>();
        services.AddScoped<IGlassProjectAttachmentRepository, GlassProjectAttachmentRepository>();
        services.AddScoped<IGlassProjectBOMLineRepository, GlassProjectBOMLineRepository>();
        services.AddScoped<IGlassProjectCuttingPlanRepository, GlassProjectCuttingPlanRepository>();
        services.AddScoped<IGlassProjectQuoteSnapshotRepository, GlassProjectQuoteSnapshotRepository>();
        services.AddScoped<IGlassProjectShareTokenRepository, GlassProjectShareTokenRepository>();
        services.AddScoped<IFieldSurveyRepository, FieldSurveyRepository>();
        services.AddScoped<IGlassWorkOrderRepository, GlassWorkOrderRepository>();
        services.AddScoped<IGlassProjectOrderLinkRepository, GlassProjectOrderLinkRepository>();
        services.AddScoped<IGlassNotificationLogRepository, GlassNotificationLogRepository>();
        services.AddScoped<IGlassWorkOrderRevisionRepository, GlassWorkOrderRevisionRepository>();
        services.AddScoped<IGlassEnclosureSettingsRepository, GlassEnclosureSettingsRepository>();
        services.AddScoped<IGlassNotificationTemplateRepository, GlassNotificationTemplateRepository>();
        services.AddScoped<IGlassTypeRepository, GlassTypeRepository>();
        services.AddScoped<IClimateZoneRepository, ClimateZoneRepository>();
        services.AddScoped<IColorOptionRepository, ColorOptionRepository>();
        services.AddScoped<IHardwareItemRepository, HardwareItemRepository>();
        services.AddScoped<IHardwareKitRepository, HardwareKitRepository>();
        services.AddScoped<IProfileItemRepository, ProfileItemRepository>();
        services.AddScoped<IProfileSystemRepository, ProfileSystemRepository>();
        services.AddScoped<IWindZoneRepository, WindZoneRepository>();
        services.AddScoped<IProjectTemplateRepository, ProjectTemplateRepository>();
        services.AddScoped<IProjectTemplateReviewRepository, ProjectTemplateReviewRepository>();
        services.AddScoped<IProjectTemplateInstallRepository, ProjectTemplateInstallRepository>();

        services.AddScoped<IBrandVendorRepository, BrandVendorRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICustomerDealerProductVisibilityRepository, CustomerDealerProductVisibilityRepository>();
        services.AddScoped<ICustomerTagLinkRepository, CustomerTagLinkRepository>();
        services.AddScoped<ICustomerUserRepository, CustomerUserRepository>();
        services.AddScoped<IDealerAccountRepository, DealerAccountRepository>();
        services.AddScoped<IDealerCustomerLinkRepository, DealerCustomerLinkRepository>();
        services.AddScoped<IDealerUserRepository, DealerUserRepository>();
        services.AddScoped<IDiscountRuleRepository, DiscountRuleRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IGLPostingMappingRepository, GLPostingMappingRepository>();
        services.AddScoped<IModulePricePlanRepository, ModulePricePlanRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOrderRevisionRepository, OrderRevisionRepository>();
        services.AddScoped<IOrderTemplateRepository, OrderTemplateRepository>();
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();
        services.AddScoped<IPaymentAttemptRepository, PaymentAttemptRepository>();
        services.AddScoped<IPaymentSessionRepository, PaymentSessionRepository>();
        services.AddScoped<IPricingDiscountRuleRepository, PricingDiscountRuleRepository>();
        services.AddScoped<IProcessedWebhookEventRepository, ProcessedWebhookEventRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IStockCountRepository, StockCountRepository>();
        services.AddScoped<ISubscriptionOrderRepository, SubscriptionOrderRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITaxAggregationRepository, TaxAggregationRepository>();
        services.AddScoped<ITaxDeclarationRepository, TaxDeclarationRepository>();
        services.AddScoped<ITaxRuleRepository, TaxRuleRepository>();
        services.AddScoped<ITenantModuleRepository, TenantModuleRepository>();
        services.AddScoped<ITwoFactorBackupCodeRepository, TwoFactorBackupCodeRepository>();
        services.AddScoped<ITwoFactorChallengeRepository, TwoFactorChallengeRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IDataSubjectRequestRepository, DataSubjectRequestRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();
        services.AddScoped<CoreAlign.Application.Privacy.IPiiAnonymizer, CoreAlign.Application.Privacy.PiiAnonymizer>();
        services.AddScoped<CoreAlign.Application.Privacy.IDataSubjectRequestService, CoreAlign.Application.Privacy.DataSubjectRequestService>();
        services.AddScoped<CoreAlign.Application.Privacy.IRetentionPolicyService, CoreAlign.Application.Privacy.RetentionPolicyService>();
        services.AddScoped<CoreAlign.Infrastructure.Privacy.IRetentionPolicyExecutor, CoreAlign.Infrastructure.Privacy.RetentionPolicyExecutor>();
        services.AddHostedService<CoreAlign.Infrastructure.Privacy.RetentionPolicyJob>();
        services.AddScoped<IUserNotificationPreferenceRepository, UserNotificationPreferenceRepository>();
        services.AddScoped<ITenantIdentityProviderRepository, TenantIdentityProviderRepository>();
        services.AddScoped<IExternalUserBindingRepository, ExternalUserBindingRepository>();
        services.AddScoped<CoreAlign.Application.Sso.ITenantIdentityProviderService,
            CoreAlign.Infrastructure.Sso.TenantIdentityProviderService>();
        services.AddScoped<CoreAlign.Application.Sso.ISsoLoginService,
            CoreAlign.Infrastructure.Sso.SsoLoginService>();
        services.AddScoped<CoreAlign.Application.Sso.IOidcDiscoveryClient,
            CoreAlign.Infrastructure.Sso.OidcDiscoveryClient>();
        services.AddScoped<CoreAlign.Application.Sso.ISamlMetadataClient,
            CoreAlign.Infrastructure.Sso.SamlMetadataClient>();
        services.AddHttpClient(CoreAlign.Infrastructure.Sso.OidcDiscoveryClient.HttpClientName,
            c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(CoreAlign.Infrastructure.Sso.SamlMetadataClient.HttpClientName,
            c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IVendorBillRepository, VendorBillRepository>();
        services.AddScoped<IVendorPaymentApplicationRepository, VendorPaymentApplicationRepository>();
        services.AddScoped<IVendorPaymentRepository, VendorPaymentRepository>();

        services.AddScoped<ISkuTemplateProvider, TenantSkuTemplateProvider>();

        services.AddCustomerMergeServices();

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

        services.AddHttpClient(ForibaEFaturaProvider.HttpClientName, (sp, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<ForibaEFaturaProvider>();
        services.AddScoped<IEFaturaProvider, ForibaEFaturaProvider>(sp => sp.GetRequiredService<ForibaEFaturaProvider>());

        services.AddHttpClient(GibPortalDirectProvider.HttpClientName, (sp, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<GibPortalDirectProvider>();
        services.AddScoped<IEFaturaProvider, GibPortalDirectProvider>(sp => sp.GetRequiredService<GibPortalDirectProvider>());

        services.AddHttpClient(NilveraEFaturaProvider.HttpClientName, (sp, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<NilveraEFaturaProvider>();
        services.AddScoped<IEFaturaProvider, NilveraEFaturaProvider>(sp => sp.GetRequiredService<NilveraEFaturaProvider>());
        services.AddScoped<NilveraTokenManager>();
        services.AddScoped<NilveraWebhookVerifier>();

        services.AddScoped<CoreAlign.Application.Fx.IFxRateProvider, CoreAlign.Infrastructure.Fx.FxRateProvider>();

        services.AddScoped<CoreAlign.Application.Fx.IFxSourceProvider, CoreAlign.Infrastructure.Fx.TcmbFxSourceProvider>();
        services.AddScoped<CoreAlign.Application.Fx.IFxSourceProvider, CoreAlign.Infrastructure.Fx.ManualFxProvider>();
        services.AddScoped<CoreAlign.Application.Fx.IFxSourceProvider, CoreAlign.Infrastructure.Fx.EcbFxProvider>();
        services.AddScoped<CoreAlign.Infrastructure.Fx.EcbFxProvider>();
        services.AddScoped<CoreAlign.Infrastructure.Fx.TenantOverrideFxProvider>();
        services.AddScoped<CoreAlign.Application.Fx.ITenantFxPreferences, CoreAlign.Infrastructure.Fx.TenantFxPreferences>();
        services.AddScoped<CoreAlign.Application.Fx.IFxRateResolver, CoreAlign.Infrastructure.Fx.FxRateResolver>();
        services.AddScoped<CoreAlign.Application.Fx.IFxRateResolverDetailed>(sp =>
            (CoreAlign.Infrastructure.Fx.FxRateResolver)sp.GetRequiredService<CoreAlign.Application.Fx.IFxRateResolver>());
        services.AddHttpClient(CoreAlign.Infrastructure.Fx.EcbFxProvider.HttpClientName, c =>
        {
            c.Timeout = TimeSpan.FromSeconds(20);
        });

        // TCMB FX ingest pipeline (Sprint10 / Phase 40):
        //   - ITcmbFxClient → TcmbFxClient (typed HttpClient, base URL TCMB feed)
        //   - TcmbFxIngestJob (Hangfire RecurringJob ve TriggerTcmbFxPollHandler concrete tip olarak inject ediyor)
        //   - PostFxRevaluationJob (Hangfire RecurringJob concrete tip olarak inject ediyor)
        services.AddHttpClient<CoreAlign.Application.Treasury.Fx.ITcmbFxClient,
            CoreAlign.Infrastructure.Services.TcmbFxClient>(c =>
        {
            c.BaseAddress = new Uri("https://www.tcmb.gov.tr/");
            c.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddScoped<CoreAlign.Application.Treasury.Fx.TcmbFxIngestJob>();
        services.AddScoped<CoreAlign.Application.Treasury.Fx.PostFxRevaluationJob>();
        services.AddScoped<CoreAlign.Application.Treasury.Fx.IFxOpenBalanceReader,
            CoreAlign.Infrastructure.Services.FxOpenBalanceReader>();

        // PlatformFxAuditRepositories: ayni dosyada tanimli ucu de DbContext'e bagimli scoped.
        // Eksik kayit edildiklerinde MediatR handler'lari (ListExchangeRatesHandler,
        // ListPlatformTenantsHandler, GetEntityAuditTimelineHandler) DI validation'da basarisiz olur.
        services.AddScoped<CoreAlign.Application.Platform.Tenants.IPlatformTenantRepository,
            CoreAlign.Infrastructure.Repositories.PlatformTenantRepository>();
        services.AddScoped<CoreAlign.Application.Treasury.Fx.IExchangeRateRepository,
            CoreAlign.Infrastructure.Repositories.ExchangeRateRepository>();
        services.AddScoped<CoreAlign.Application.Compliance.Audit.IEntityAuditLogRepository,
            CoreAlign.Infrastructure.Repositories.EntityAuditLogRepository>();
        services.AddScoped<CoreAlign.Application.Compliance.Audit.IAuditLogExportService,
            CoreAlign.Infrastructure.Compliance.AuditLogExportService>();
        services.AddScoped<CoreAlign.Application.Compliance.Audit.IScheduledAuditExportConfigRepository,
            CoreAlign.Infrastructure.Compliance.ScheduledAuditExportConfigRepository>();
        services.AddScoped<CoreAlign.Application.Compliance.Audit.ScheduledAuditExportJob>();

        // Transactional outbox enqueuer'lari (her biri IOutboxRepository + IOutboxSignal'a
        // baglanip event'i isleme kuyrugunu yazar). Implementation'lar Application katmaninda.
        services.AddScoped<CoreAlign.Application.Common.Outbox.IGLPostingOutbox,
            CoreAlign.Application.Common.Outbox.GLPostingOutbox>();
        services.AddScoped<CoreAlign.Application.Common.Outbox.ISecurityAlertOutbox,
            CoreAlign.Application.Common.Outbox.SecurityAlertOutbox>();
        services.AddScoped<CoreAlign.Application.Billing.ISubscriptionActivatedOutbox,
            CoreAlign.Application.Billing.SubscriptionActivatedOutbox>();
        services.AddScoped<CoreAlign.Application.Orders.Revisions.IOrderRevisionOutbox,
            CoreAlign.Application.Orders.Revisions.OrderRevisionOutbox>();
        services.AddScoped<CoreAlign.Application.B2B.PortalComments.IOrderCommentPostedOutbox,
            CoreAlign.Application.B2B.PortalComments.OrderCommentPostedOutbox>();
        services.AddScoped<CoreAlign.Application.B2B.DealerOrderFlow.IDealerOrderApprovalOutbox,
            CoreAlign.Application.B2B.DealerOrderFlow.DealerOrderApprovalOutbox>();
        services.AddScoped<CoreAlign.Application.Collaboration.ICommentPostedOutbox,
            CoreAlign.Application.Collaboration.CommentPostedOutbox>();
        services.AddScoped<CoreAlign.Application.Common.Email.IEmailQueuedOutbox,
            CoreAlign.Application.Common.Email.EmailQueuedOutbox>();
        services.AddScoped<CoreAlign.Application.EInvoice.IEInvoiceSubmissionOutbox,
            CoreAlign.Application.EInvoice.EInvoiceSubmissionOutbox>();

        // Auth servisleri
        services.AddScoped<CoreAlign.Application.Auth.Services.ITwoFactorService,
            CoreAlign.Infrastructure.Services.TwoFactorService>();
        services.AddHttpClient<CoreAlign.Application.Auth.Services.IPwnedPasswordsService,
            CoreAlign.Infrastructure.Services.HibpPwnedPasswordsService>(c =>
        {
            c.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        // PasswordPolicyService IPasswordHistoryRepository (scoped, DbContext'e bagli) inject ediyor →
        // Singleton olamaz, Scoped yap. Aksi halde Singleton/Scoped lifetime mismatch hatasi atar.
        services.AddScoped<CoreAlign.Application.Auth.Services.IPasswordPolicyService,
            CoreAlign.Application.Auth.Services.PasswordPolicyService>();

        // Billing modul aktivasyonu + payment gateway registry
        services.AddScoped<CoreAlign.Application.Billing.IActiveModulesService,
            CoreAlign.Infrastructure.Services.ActiveModulesService>();
        services.AddSingleton<CoreAlign.Application.Billing.Payments.IPaymentGatewayRegistry,
            CoreAlign.Application.Billing.Payments.PaymentGatewayRegistry>();

        // Bulk import (Excel/CSV) - session ve row reader
        // BulkImportSessionStore ITenantContext (scoped) inject ediyor → Singleton olamaz.
        // BulkImportRowReader stateless (sadece Stream parse) → Singleton uygun.
        services.AddScoped<CoreAlign.Application.Imports.IBulkImportSessionStore,
            CoreAlign.Infrastructure.Services.BulkImportSessionStore>();
        services.AddSingleton<CoreAlign.Application.Imports.IBulkImportRowReader,
            CoreAlign.Infrastructure.Services.Imports.BulkImportRowReader>();
        services.AddScoped<CoreAlign.Application.Imports.Customers.CustomerBulkImporter>();
        services.AddScoped<CoreAlign.Application.Imports.Products.ProductBulkImporter>();
        services.AddScoped<CoreAlign.Application.Imports.GLAccounts.GLAccountBulkImporter>();

        // Purchasing: Three-way match reader (PO + GR + Invoice eslestir)
        services.AddScoped<CoreAlign.Domain.Interfaces.IThreeWayMatchReader,
            CoreAlign.Infrastructure.Repositories.ThreeWayMatchReader>();

        // Installation Acceptance servisi (MediatR command handler'lar ctor'da inject ediyor)
        services.AddScoped<CoreAlign.Application.Installation.IInstallationAcceptanceService,
            CoreAlign.Application.Installation.InstallationAcceptanceService>();

        // Privacy / GDPR
        services.AddScoped<CoreAlign.Application.Privacy.IUserAnonymizer,
            CoreAlign.Application.Privacy.UserAnonymizer>();
        services.AddSingleton<CoreAlign.Application.Privacy.IPrivacyHasher,
            CoreAlign.Infrastructure.Services.HmacPrivacyHasher>();
        services.AddScoped<CoreAlign.Application.Privacy.IPrivacyEraseService,
            CoreAlign.Infrastructure.Repositories.PrivacyEraseService>();
        services.AddScoped<CoreAlign.Application.Privacy.IPrivacyDataReader,
            CoreAlign.Infrastructure.Repositories.PrivacyDataReader>();
        services.AddScoped<CoreAlign.Application.Privacy.IDataSubjectRequestLog,
            CoreAlign.Infrastructure.Repositories.DataSubjectRequestLog>();

        // F3.1 Warranty + Maintenance module: repositories + daily expiry notifier
        // that emits outbox events for contracts expiring within 30 days.
        services.AddScoped<IWarrantyContractRepository, WarrantyContractRepository>();
        services.AddScoped<IMaintenanceScheduleRepository, MaintenanceScheduleRepository>();
        services.AddScoped<IServiceTicketRepository, ServiceTicketRepository>();
        services.AddHostedService<CoreAlign.Infrastructure.Warranty.WarrantyExpiryNotifier>();

        services.AddScoped<IInstallationAcceptanceRepository, InstallationAcceptanceRepository>();
        services.AddScoped<IPunchListRepository, PunchListRepository>();

        // F3.4 MRP Lite — purchase requisitions, MRP service, weekly background run,
        // outbox handler for MrpSuggestionsCreated downstream notifications.
        services.AddScoped<IPurchaseRequisitionRepository, PurchaseRequisitionRepository>();
        services.AddScoped<CoreAlign.Application.Mrp.IMrpService, CoreAlign.Infrastructure.Mrp.MrpService>();
        services.AddScoped<CoreAlign.Application.Common.Outbox.IOutboxMessageHandler,
            CoreAlign.Application.Mrp.MrpSuggestionsCreatedOutboxHandler>();
        services.AddHostedService<CoreAlign.Infrastructure.Mrp.MrpWeeklyJob>();

        services.AddSingleton<CoreAlign.Infrastructure.Mrp.Planning.ILotSizingCalculator,
            CoreAlign.Infrastructure.Mrp.Planning.LotSizingCalculator>();
        services.AddSingleton<CoreAlign.Infrastructure.Mrp.Planning.IDemandForecaster,
            CoreAlign.Infrastructure.Mrp.Planning.DemandForecaster>();
        services.AddSingleton<CoreAlign.Infrastructure.Mrp.Planning.IActionMessageGenerator,
            CoreAlign.Infrastructure.Mrp.Planning.ActionMessageGenerator>();
        services.AddSingleton<CoreAlign.Application.Mrp.Planning.IMrpPlanningEngine,
            CoreAlign.Infrastructure.Mrp.Planning.MrpPlanningEngine>();
        services.AddSingleton<CoreAlign.Application.Mrp.Planning.IMrpChangeImpactAnalyzer,
            CoreAlign.Infrastructure.Mrp.Planning.MrpChangeImpactAnalyzer>();
        services.AddScoped<CoreAlign.Application.Mrp.Planning.IMrpPlanningDataLoader,
            CoreAlign.Infrastructure.Mrp.Planning.MrpPlanningDataLoader>();
        services.AddScoped<CoreAlign.Application.Mrp.IAbcUsageDataLoader,
            CoreAlign.Infrastructure.Mrp.AbcUsageDataLoader>();

        // MRP T5 distribution (DRP) overlay — read-only, transient. The planner is a pure
        // value-object transform (singleton); the loader reads tenant stock+demand (scoped).
        services.AddSingleton<CoreAlign.Application.Mrp.Distribution.IDistributionPlanner,
            CoreAlign.Application.Mrp.Distribution.DistributionPlanner>();
        services.AddScoped<CoreAlign.Application.Mrp.Distribution.IDistributionDataLoader,
            CoreAlign.Infrastructure.Mrp.Distribution.DistributionDataLoader>();

        // MRP T6 Rough-Cut Capacity Planning (RCCP) — infinite-capacity load vs capacity.
        // The calculator is a pure value-object transform (singleton); the loader reads
        // tenant routing + active work centers and buckets production orders (scoped).
        services.AddSingleton<CoreAlign.Application.Mrp.Capacity.ICrpCalculator,
            CoreAlign.Application.Mrp.Capacity.CrpCalculator>();
        services.AddScoped<CoreAlign.Application.Mrp.Capacity.ICapacityLoadDataLoader,
            CoreAlign.Infrastructure.Mrp.Capacity.CapacityLoadDataLoader>();

        // MRP T1 workbench (Group B): plan-run persistence + preview/commit/release
        // orchestration. The single scoped service satisfies both the planning
        // (preview/drill) and workbench (commit/release) contracts.
        services.AddScoped<IMrpPlanRunRepository, MrpPlanRunRepository>();
        services.AddScoped<IPlannedProductionOrderRepository, PlannedProductionOrderRepository>();
        services.AddScoped<IWorkCenterRepository, WorkCenterRepository>();
        services.AddScoped<CoreAlign.Infrastructure.Mrp.MrpPlanningService>();
        services.AddScoped<CoreAlign.Application.Mrp.IMrpPlanningService>(sp =>
            sp.GetRequiredService<CoreAlign.Infrastructure.Mrp.MrpPlanningService>());
        services.AddScoped<CoreAlign.Application.Mrp.IMrpWorkbenchService>(sp =>
            sp.GetRequiredService<CoreAlign.Infrastructure.Mrp.MrpPlanningService>());

        // Dev-only MRP demo seeder. The transport (POST /mrp/dev/seed-demo) is gated
        // to the Development environment at the controller; registering the service
        // unconditionally is harmless because it is only reachable via that endpoint.
        services.AddScoped<CoreAlign.Application.Mrp.IMrpDemoSeeder,
            CoreAlign.Infrastructure.Mrp.MrpDemoSeeder>();

        // F1.8 IProviderRegistry pattern: all IEFaturaProvider registrations above
        // feed the generic registry so tenant-resolved provider lookup works.
        services.AddDataProtection();
        services.AddScoped<ITenantProviderConfigResolver, TenantProviderConfigResolver>();
        services.AddScoped<IProviderCredentialProtector, DataProtectionCredentialProtector>();
        services.AddScoped<ITenantProviderConfigRepository, TenantProviderConfigRepository>();
        services.AddScoped<IProviderWebhookInboxRepository, ProviderWebhookInboxRepository>();
        services.AddScoped<IProviderRegistry<IEFaturaProvider>, ProviderRegistry<IEFaturaProvider>>();

        // F2.1 dispatcher + webhook verifiers + reconciliation job + gateway adapter.
        services.AddScoped<IEFaturaDispatcher, EFaturaDispatcher>();
        services.AddScoped<IWebhookSignatureVerifier, WebhookSignatureVerifierComposer>();
        services.AddScoped<IProviderWebhookVerifier, NilveraWebhookVerifierAdapter>();
        services.AddScoped<IProviderWebhookVerifier, ForibaWebhookVerifierAdapter>();
        services.AddHostedService<EFaturaReconciliationJob>();

        // F2.2 payment providers — F1.8 IPaymentProvider pattern with tenant-isolated credentials.
        services.AddHttpClient(IyzicoPaymentProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(45));
        services.AddScoped<IyzicoPaymentProvider>();
        services.AddScoped<IPaymentProvider, IyzicoPaymentProvider>(sp => sp.GetRequiredService<IyzicoPaymentProvider>());
        services.AddScoped<IProviderWebhookVerifier, IyzicoWebhookVerifier>();

        services.AddHttpClient(StripePaymentProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(45));
        services.AddScoped<StripePaymentProvider>();
        services.AddScoped<IPaymentProvider, StripePaymentProvider>(sp => sp.GetRequiredService<StripePaymentProvider>());
        services.AddScoped<IProviderWebhookVerifier, StripeWebhookVerifier>();

        services.AddHttpClient(PayTRPaymentProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(45));
        services.AddScoped<PayTRPaymentProvider>();
        services.AddScoped<IPaymentProvider, PayTRPaymentProvider>(sp => sp.GetRequiredService<PayTRPaymentProvider>());
        services.AddScoped<IProviderWebhookVerifier, PayTRWebhookVerifier>();

        // F2.2 payment orchestration: registry, dispatcher, ledger repository, reconciliation job.
        services.AddScoped<IProviderRegistry<IPaymentProvider>, ProviderRegistry<IPaymentProvider>>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IPaymentDispatcher, PaymentDispatcher>();
        services.AddHostedService<PaymentReconciliationJob>();

        // F1.9 bridge: IElectronicInvoiceGateway consumers now flow through the
        // F2.1 dispatcher so we don't run two parallel e-invoice pipelines.
        services.AddScoped<IElectronicInvoiceGateway, EFaturaProviderGatewayAdapter>();

        CoreAlign.Infrastructure.Storage.StorageRegistration.AddVirusScanningStorage(services, configuration);
        CoreAlign.Infrastructure.Caching.CachingRegistration.AddDistributedCaching(services, configuration);
        CoreAlign.Infrastructure.Storage.StorageProviderRegistration.AddStorageProvider(services, configuration);
        CoreAlign.Infrastructure.Reports.Sprint9ReportingRegistration.AddSprint9Reporting(services, configuration);

        services.AddScoped<CoreAlign.Application.GlassEnclosure.Cutting.IGlass2DNestingOptimizer,
            CoreAlign.Infrastructure.GlassEnclosure.Cutting.MaxRectsGlass2DOptimizer>();

        // F4.1 Notification subsystem registration: providers, repositories, registries, options
        services.Configure<CoreAlign.Infrastructure.Options.SmtpEmailOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.SmtpEmailOptions.SectionName));
        services.Configure<CoreAlign.Infrastructure.Options.SendGridOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.SendGridOptions.SectionName));
        services.Configure<CoreAlign.Infrastructure.Options.NetgsmSmsOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.NetgsmSmsOptions.SectionName));
        services.Configure<CoreAlign.Infrastructure.Options.TwilioOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.TwilioOptions.SectionName));
        services.Configure<CoreAlign.Infrastructure.Options.FcmPushOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.FcmPushOptions.SectionName));
        services.Configure<CoreAlign.Infrastructure.Options.WebPushOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.WebPushOptions.SectionName));
        services.Configure<CoreAlign.Infrastructure.Options.MetaWhatsAppOptions>(configuration.GetSection(CoreAlign.Infrastructure.Options.MetaWhatsAppOptions.SectionName));

        services.AddHttpClient();

        services.AddScoped<CoreAlign.Application.Notifications.Providers.IEmailProvider,
            CoreAlign.Infrastructure.Notifications.Email.SmtpEmailProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.IEmailProvider,
            CoreAlign.Infrastructure.Notifications.Email.SendGridEmailProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.ISmsProvider,
            CoreAlign.Infrastructure.Notifications.Sms.NetgsmSmsProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.ISmsProvider,
            CoreAlign.Infrastructure.Notifications.Sms.TwilioSmsProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.IPushNotificationProvider,
            CoreAlign.Infrastructure.Notifications.Push.FcmPushProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.IPushNotificationProvider,
            CoreAlign.Infrastructure.Notifications.Push.WebPushProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.IWhatsAppProvider,
            CoreAlign.Infrastructure.Notifications.WhatsApp.MetaWhatsAppProvider>();
        services.AddScoped<CoreAlign.Application.Notifications.Providers.IWhatsAppProvider,
            CoreAlign.Infrastructure.Notifications.WhatsApp.TwilioWhatsAppProvider>();

        services.AddScoped<IProviderRegistry<CoreAlign.Application.Notifications.Providers.IEmailProvider>,
            ProviderRegistry<CoreAlign.Application.Notifications.Providers.IEmailProvider>>();
        services.AddScoped<IProviderRegistry<CoreAlign.Application.Notifications.Providers.ISmsProvider>,
            ProviderRegistry<CoreAlign.Application.Notifications.Providers.ISmsProvider>>();
        services.AddScoped<IProviderRegistry<CoreAlign.Application.Notifications.Providers.IPushNotificationProvider>,
            ProviderRegistry<CoreAlign.Application.Notifications.Providers.IPushNotificationProvider>>();
        services.AddScoped<IProviderRegistry<CoreAlign.Application.Notifications.Providers.IWhatsAppProvider>,
            ProviderRegistry<CoreAlign.Application.Notifications.Providers.IWhatsAppProvider>>();

        services.AddScoped<CoreAlign.Application.Notifications.Repositories.INotificationMessageRepository,
            CoreAlign.Infrastructure.Repositories.NotificationMessageRepository>();
        services.AddScoped<CoreAlign.Application.Notifications.Repositories.INotificationTemplateRepository,
            CoreAlign.Infrastructure.Repositories.NotificationTemplateRepository>();
        services.AddScoped<CoreAlign.Application.Notifications.Repositories.INotificationPreferenceRepository,
            CoreAlign.Infrastructure.Repositories.NotificationPreferenceRepository>();
        services.AddScoped<CoreAlign.Application.Notifications.Repositories.IUserDeviceTokenRepository,
            CoreAlign.Infrastructure.Repositories.UserDeviceTokenRepository>();

        // F4.5 Whitelabel customization repository
        services.AddScoped<CoreAlign.Application.Whitelabel.ITenantThemeRepository,
            CoreAlign.Infrastructure.Repositories.TenantThemeRepository>();

        // F4.6 BI / Reporting Advanced — data sources, export providers, services
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.SalesDataSource>();
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.InventoryDataSource>();
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.ARDataSource>();
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.APDataSource>();
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.WarrantyDataSource>();
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.ServiceTicketDataSource>();
        services.AddScoped<CoreAlign.Application.BI.DataSources.IBIDataSourceAggregator,
            CoreAlign.Infrastructure.BI.DataSources.CashDataSource>();
        services.AddScoped<CoreAlign.Application.BI.IExportProvider,
            CoreAlign.Infrastructure.BI.Export.CsvExportProvider>();
        services.AddScoped<CoreAlign.Application.BI.IExportProvider,
            CoreAlign.Infrastructure.BI.Export.ExcelExportProvider>();
        services.AddScoped<CoreAlign.Application.BI.IExportProvider,
            CoreAlign.Infrastructure.BI.Export.PdfExportProvider>();
        services.AddScoped<CoreAlign.Application.BI.IBIReportService,
            CoreAlign.Infrastructure.BI.BIReportService>();
        services.AddScoped<CoreAlign.Application.BI.IDashboardService,
            CoreAlign.Infrastructure.BI.DashboardService>();
        services.AddScoped<CoreAlign.Application.BI.ISavedReportService,
            CoreAlign.Infrastructure.BI.SavedReportService>();

        return services;
    }
}
