using System.IO.Compression;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Configuration;
using CoreAlign.API.Hangfire;
using CoreAlign.API.HostedServices;
using CoreAlign.API.Middleware;
using CoreAlign.API.Options;
using CoreAlign.Application;
using CoreAlign.Infrastructure;
using CoreAlign.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load secrets from the configured external vault (Azure KeyVault / AWS SSM) before
// other config is consumed, so vault values override appsettings. No-op unless
// Configuration:VaultProvider is set — dev/test are unaffected.
builder.Configuration.AddVaultConfiguration(builder.Configuration);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Kestrel limits tuned for ERP workload: HTTP/1.1 + HTTP/2 keep-alive, sane
// header caps. Values overridable via Kestrel:* configuration. Default body
// cap is 30 MB to match the generic-file-upload ceiling exposed by
// FileStorageOptions.MaxBytesPerFile; per-endpoint stricter limits (5 MB for
// product images, 1 MB for tenant logos) are enforced via
// [RequestSizeLimit] attributes on the controllers.
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var cfg = context.Configuration;
    options.Limits.MaxConcurrentConnections = cfg.GetValue<long?>("Kestrel:MaxConcurrentConnections") ?? 1000;
    options.Limits.MaxConcurrentUpgradedConnections = cfg.GetValue<long?>("Kestrel:MaxConcurrentUpgradedConnections") ?? 200;
    options.Limits.MaxRequestBodySize = cfg.GetValue<long?>("Kestrel:MaxRequestBodyBytes") ?? 30_000_000L;
    options.Limits.MaxRequestHeadersTotalSize = cfg.GetValue<int?>("Kestrel:MaxRequestHeadersBytes") ?? 32 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(cfg.GetValue<int?>("Kestrel:KeepAliveSeconds") ?? 120);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(cfg.GetValue<int?>("Kestrel:RequestHeadersTimeoutSeconds") ?? 30);
    options.AddServerHeader = false;
    options.ConfigureEndpointDefaults(ep => ep.Protocols = HttpProtocols.Http1AndHttp2);
});

// Serilog: wrap sinks in Async() so file I/O doesn't block the request thread.
// File and Console both go through the async dispatcher; under load this keeps
// log flushes off the hot path entirely.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CoreAlign.API")
    .WriteTo.Async(a => a.Console())
    .WriteTo.Async(a => a.File("logs/corealign-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)));

CoreAlign.API.Observability.SentryStartupExtensions.AddCoreAlignSentry(builder);

CoreAlign.API.Observability.OpenTelemetryConfig.AddCoreAlignOpenTelemetry(builder);

builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(o =>
    o.Filters.Add<CoreAlign.API.Common.CorrelationResultFilter>());

builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Unify wire format with the SPA: camelCase property names, drop null fields,
        // and serialize enums as strings so consumers don't have to mirror the int
        // values. ReferenceHandler.IgnoreCycles is critical because navigation
        // properties (Customer ↔ Orders ↔ Customer) would otherwise loop on serialize.
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });

// Brotli first (better compression ratio), then Gzip fallback for older clients.
// EnableForHttps is opt-in because of BREACH/CRIME concerns — keep off in
// environments serving sensitive cookies over the same compressed body. Today our
// auth cookies are HttpOnly + per-path so the risk surface is small; flip with
// configuration.
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = builder.Configuration.GetValue<bool?>("ResponseCompression:EnableForHttps") ?? true;
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/problem+json",
        "text/plain",
        "text/csv",
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// OutputCache for read-only endpoints that don't depend on user identity.
// Tenant-keyed policy is set per-endpoint via attributes.
builder.Services.AddOutputCache(opts =>
{
    opts.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(30);
    opts.AddPolicy("ShortTenant", b => b
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByHeader("Authorization")
        .Tag("tenant"));
    opts.AddPolicy("LookupTenant", b => b
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader("Authorization")
        .Tag("lookup"));
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
}

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger is dev-only — skip reflection cost (controller scan + OpenAPI doc
// build) in prod where the UI isn't served anyway.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoreAlign API", Version = "v1" });
        // CustomerPortalController.GetInvoices ve MyInvoicesController.ListMy ayni route'a (api/v1/customer-portal/invoices)
        // map ediyor; OpenAPI spec'inde tek action izinli. Conflict'i ilk action'i secerek cozuyoruz —
        // route deduplication gercek fix; bu workaround sadece UI'in calismasini saglar.
        options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        // [FromForm] IFormFile parametreleri (4 controller'da var) Swagger'a [ApiExplorerSettings(IgnoreApi=true)]
        // ile gizlendi — upload'lar Swagger UI'da test edilmez, gercek istemci kullanir.

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
        });
    });
}

if (!builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Auth:AutoConfirmEmail"))
{
    throw new InvalidOperationException(
        "Auth:AutoConfirmEmail is enabled outside Development. This bypasses email verification and is forbidden in non-dev environments.");
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PersonaPolicies.Customer, p => p
        .RequireAuthenticatedUser()
        .RequireClaim(PersonaPolicies.PersonaClaimType, PersonaPolicies.CustomerPersonaValue));
    options.AddPolicy(PersonaPolicies.Dealer, p => p
        .RequireAuthenticatedUser()
        .RequireClaim(PersonaPolicies.PersonaClaimType, PersonaPolicies.DealerPersonaValue));
    options.AddPolicy(PersonaPolicies.Tenant, p => p
        .RequireAuthenticatedUser()
        .RequireClaim(PersonaPolicies.PersonaClaimType, PersonaPolicies.TenantPersonaValue));
    options.AddPolicy(PersonaPolicies.PlatformAdmin, p => p
        .RequireAuthenticatedUser()
        .RequireRole(PersonaPolicies.PlatformAdminRole));
    options.AddPolicy(CoreAlign.Application.Authorization.CustomerPortalPolicies.SelfService, p => p
        .RequireAuthenticatedUser()
        .RequireClaim(PersonaPolicies.PersonaClaimType, PersonaPolicies.CustomerPersonaValue));

    options.AddPolicy(CoreAlign.Application.Authorization.AdminPolicies.ProviderConfig, p => p
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx =>
            ctx.User.IsInRole(CoreAlign.Application.Authorization.AdminPolicies.TenantAdminRole)
            || ctx.User.HasClaim(
                CoreAlign.Application.Authorization.AdminPolicies.PermissionClaimType,
                CoreAlign.Application.Authorization.AdminPolicies.ProviderConfigPermission)));

    options.AddPolicy(CoreAlign.Application.Authorization.PaymentPolicies.Charge, p => p
        .RequireAuthenticatedUser());
    options.AddPolicy(CoreAlign.Application.Authorization.PaymentPolicies.Refund, p => p
        .RequireAuthenticatedUser()
        .RequireRole(
            CoreAlign.Application.Authorization.PaymentPolicies.TenantAdminRole,
            CoreAlign.Application.Authorization.PaymentPolicies.FinanceManagerRole));

    options.AddPolicy(CoreAlign.API.Controllers.FxRatesPolicies.AdminFxSync, p => p
        .RequireAuthenticatedUser()
        .RequireRole("TenantAdmin"));

    foreach (var entry in CoreAlign.Application.GlassEnclosure.Authorization.GlassEnclosurePolicies.PolicyRoleMap)
    {
        var allowedRoles = entry.Value;
        options.AddPolicy(entry.Key, p => p
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => allowedRoles.Any(r => ctx.User.IsInRole(r))));
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Emit a Retry-After header + the standard ApiResponse envelope on throttle so
    // clients back off correctly and parse the error like every other API failure.
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (!response.HasStarted)
        {
            await response.WriteAsJsonAsync(
                CoreAlign.Application.Common.ApiResponse<object>.Failure(
                    "Too many requests. Please retry later.", StatusCodes.Status429TooManyRequests),
                cancellationToken);
        }
    };

    // Helper: build a composite partition key so a single attacker can't bypass
    // the limit by simply walking through endpoints. We mix IP + path + the
    // authenticated user id (when present) — that way a logged-in user's bucket
    // moves with them across IPs (mobile network swap) and an anonymous attacker
    // gets per-(ip,path) buckets that don't pollute each other.
    static string CompositePartitionKey(HttpContext ctx, string scope)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon-ip";
        var userId = ctx.User.FindFirst("sub")?.Value
                     ?? ctx.User.FindFirst("nameid")?.Value
                     ?? ctx.User.FindFirst("id")?.Value;
        var subject = userId ?? ip;
        var path = ctx.Request.Path.Value?.TrimEnd('/') ?? "/";
        return $"{scope}|{subject}|{path}";
    }

    // Tighter sliding window for unauthenticated auth endpoints (login, refresh,
    // forgot-password, etc.). Each (subject + path) gets its own bucket so
    // password-spraying one account from many IPs and credential-stuffing many
    // accounts from one IP are both throttled.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: CompositePartitionKey(httpContext, "auth"),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Higher cap for general API traffic; still partitioned by user where
    // possible so a single user can't burn through quota for a whole IP block.
    options.AddPolicy("global", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: CompositePartitionKey(httpContext, "global"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Tight cap for the anonymous client-error ingest endpoint so a flood can't
    // bloat error_logs. Partitioned by (ip + path); login-page errors still report.
    options.AddPolicy("client-errors", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: CompositePartitionKey(httpContext, "client-errors"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Tight per-(subject + path) cap for the anonymous AI Helper endpoint so a
    // flood can't drive expensive embedding + LLM inference. Composite key throttles
    // both a single IP and a single authenticated user.
    options.AddPolicy("ai-helper", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: CompositePartitionKey(httpContext, "ai-helper"),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int?>("AiHelper:AuthedRateLimitPerMinute") ?? 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>();
        policy.WithOrigins(corsOptions?.AllowedOrigins ?? Array.Empty<string>())
              .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With")
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              .AllowCredentials();
    });
});

// Split health checks: /health/live is process-only (cheap, suitable for k8s
// liveness probes every few seconds), /health/ready hits the DB and is suitable
// for readiness/ALB checks at a slower cadence.
var healthChecks = builder.Services.AddHealthChecks();
var configuredProvider = builder.Configuration["Database:Provider"] ?? "Postgres";
if (!string.Equals(configuredProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    healthChecks.AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "database",
        tags: new[] { "ready" });
}

// ActivityLog channel + background consumer: keeps audit writes off the request
// thread. Channel is bounded so a misbehaving downstream DB can't OOM the host.
builder.Services.AddSingleton<IActivityLogChannel, ActivityLogChannel>();
builder.Services.AddHostedService<ActivityLogWorker>();

// Partition rollover: keeps the RANGE-partitioned leaf tables extended a few months
// ahead so new rows stay in prunable monthly partitions instead of the DEFAULT one.
// Self-contained (not a Hangfire job) so it runs even before the recurring-job host
// is wired. No-ops on non-Postgres providers.
builder.Services.AddHostedService<CoreAlign.API.HostedServices.PartitionMaintenanceHostedService>();

// Demo seeding gate: DEMO_DATA=true env veya DemoData:Enabled config gerekir.
// Production'da gate her zaman reddedilir (startup'ta throw — bkz. DemoDataSeeder.IsSeedingEnabled).
// IsSeedingEnabled Production + flag kombinasyonunda exception firlatir, boylece yanlislikla deploy
// edilen demo seed konfigurasyonu app'i hemen kapatir (silent run yerine fail-fast).
CoreAlign.API.HostedServices.DemoDataSeeder.IsSeedingEnabled(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<CoreAlign.API.HostedServices.DemoDataSeeder>();
builder.Services.AddHostedService<CoreAlign.API.HostedServices.PayrollSystemDataSeeder>();
builder.Services.AddHostedService<CoreAlign.API.HostedServices.GibCodeSystemDataSeeder>();
builder.Services.AddHostedService<CoreAlign.API.HostedServices.GlassPlateSystemDataSeeder>();

// Hangfire recurring-job host. Real environments use PostgreSQL storage (Hangfire
// creates its own "hangfire" schema). Skipped under the test flag
// (Hangfire:UseMemoryStorage) — the integration tests drive the outbox via the inline
// OutboxDrainBehavior, not the background server, and there is no in-memory storage
// package wired for tests. RecurringJobsRegistration.RegisterAll runs after Build.
var hangfireEnabled = !builder.Configuration.GetValue<bool>("Hangfire:UseMemoryStorage");
if (hangfireEnabled)
{
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)));
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

// Migrations are run out-of-band by default (CI/CD step or one-off job). The
// app only auto-migrates when explicitly invoked with --migrate or via the
// Database:AutoMigrate flag (kept on for dev). This prevents replicas from
// racing each other on startup and from holding a long migration lock under
// the request pipeline.
var shouldAutoMigrate = args.Contains("--migrate")
    || builder.Configuration.GetValue<bool?>("Database:AutoMigrate") == true
    || app.Environment.IsDevelopment();

if (shouldAutoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
    var dbProvider = builder.Configuration["Database:Provider"] ?? "Postgres";

    try
    {
        // Npgsql multiplexing only supports async command execution, so the
        // startup migration must use the async APIs.
        if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                var failedMigration = pendingMigrations.FirstOrDefault();
                app.Logger.LogCritical(ex, "FATAL MIGRATION ERROR: The database migration failed! The error likely occurred while trying to apply the migration: '{FailedMigration}'", failedMigration ?? "Unknown (or seed data)");
                throw;
            }
        }
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "28P01")
    {
        Log.Fatal(
            "PostgreSQL authentication failed (28P01). Two options:\n" +
            "  1) Switch to SQLite for dev: dotnet user-secrets set \"Database:Provider\" \"Sqlite\" --project server/src/CoreAlign.API\n" +
            "  2) Provide the real password: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=corealign;Username=postgres;Password=YOUR_PASSWORD\" --project server/src/CoreAlign.API");
        throw;
    }
    catch (Npgsql.NpgsqlException ex)
    {
        Log.Fatal(ex, "Could not connect to PostgreSQL. Verify the database is running and the connection string is correct.");
        throw;
    }

    if (args.Contains("--migrate"))
    {
        // CI invoked us purely to migrate — exit cleanly so the deployment can
        // proceed to start the long-running replicas.
        return;
    }
}

// Middleware pipeline — order matters. ForwardedHeaders must come before
// anything that inspects RemoteIpAddress; ExceptionHandling must be early enough
// to catch failures inside CORS/RateLimit; ResponseCompression must run after
// the response body is produced but before it ships, so it sits between the
// exception handler and the routing layer.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseSerilogRequestLogging(opts =>
{
    // Keep 2xx noise at Debug so prod doesn't drown in per-request structured
    // logs; warnings/errors stay at their natural level.
    opts.GetLevel = (ctx, _, ex) =>
    {
        if (ex != null) return Serilog.Events.LogEventLevel.Error;
        if (ctx.Response.StatusCode >= 500) return Serilog.Events.LogEventLevel.Error;
        if (ctx.Response.StatusCode >= 400) return Serilog.Events.LogEventLevel.Warning;
        return Serilog.Events.LogEventLevel.Debug;
    };
});
app.UseResponseCompression();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();

if (app.Configuration.GetSection("OpenTelemetry").GetValue<bool?>("MetricsEnabled") ?? true)
{
    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}

app.UseAuthentication();
app.UseAuthorization();

if (hangfireEnabled)
{
    // Dashboard behind the existing role-based authorization filter; RegisterAll
    // schedules the recurring jobs (outbox drain, token/log cleanup, FX ingest, etc.).
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    });
    RecurringJobsRegistration.RegisterAll(app.Services);
}

app.UseMiddleware<EtagMiddleware>();
app.UseOutputCache();
app.UseMiddleware<ActivityLogMiddleware>();
app.UseMiddleware<SubdomainTenantResolverMiddleware>();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
// Back-compat: legacy /health → /health/ready
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
app.MapControllers().RequireRateLimiting("global");

app.Run();
