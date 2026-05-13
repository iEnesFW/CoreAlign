using System.Threading.RateLimiting;
using Asp.Versioning;
using CoreAlign.API.Middleware;
using CoreAlign.API.Options;
using CoreAlign.Application;
using CoreAlign.Infrastructure;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CoreAlign.API")
    .WriteTo.Console()
    .WriteTo.File("logs/corealign-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoreAlign API", Version = "v1" });

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

if (!builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Auth:AutoConfirmEmail"))
{
    throw new InvalidOperationException(
        "Auth:AutoConfirmEmail is enabled outside Development. This bypasses email verification and is forbidden in non-dev environments.");
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("global", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
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

var healthChecks = builder.Services.AddHealthChecks();
var configuredProvider = builder.Configuration["Database:Provider"] ?? "Postgres";
if (!string.Equals(configuredProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    healthChecks.AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "database",
        tags: new[] { "ready" });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
    var dbProvider = builder.Configuration["Database:Provider"] ?? "Postgres";

    try
    {
        if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
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
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ActivityLogMiddleware>();

app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("global");

app.Run();
