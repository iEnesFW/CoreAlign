using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CoreAlign.API.Configuration;

public static class SwaggerStartup
{
    public const string OpenApiRouteTemplate = "openapi/{documentName}.json";
    public const string SwaggerUiRoutePrefix = "swagger";

    public static IServiceCollection AddCoreAlignSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>());
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
            options.SupportNonNullableReferenceTypes();

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>(),
            });

            options.TagActionsBy(api =>
            {
                if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
                    && !string.IsNullOrEmpty(controller))
                {
                    return new[] { controller };
                }
                return new[] { api.GroupName ?? "default" };
            });

            options.DocInclusionPredicate((_, api) =>
            {
                var parameters = api.ActionDescriptor.Parameters;
                foreach (var p in parameters)
                {
                    if (p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile)
                        || p.ParameterType == typeof(IEnumerable<Microsoft.AspNetCore.Http.IFormFile>)
                        || p.ParameterType == typeof(List<Microsoft.AspNetCore.Http.IFormFile>)
                        || p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile[])
                        || p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFileCollection))
                    {
                        return false;
                    }
                }
                return true;
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCoreAlignSwagger(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = OpenApiRouteTemplate;
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = SwaggerUiRoutePrefix;
            options.SwaggerEndpoint("/openapi/v1.json", "CoreAlign API V1");
            options.DocumentTitle = "CoreAlign API";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapGet("/docs", context =>
        {
            context.Response.Redirect("/docs/", permanent: false);
            return Task.CompletedTask;
        }).AllowAnonymous().ExcludeFromDescription();

        return app;
    }

    private sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "1.0.0";
            var description = assembly
                .GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
                ?? "Multi-tenant Turkish SaaS ERP HTTP API.";

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CoreAlign API",
                Version = assemblyVersion,
                Description = description,
                Contact = new OpenApiContact
                {
                    Name = "CoreAlign Engineering",
                    Email = "engineering@corealign.app",
                    Url = new Uri("https://corealign.app/contact"),
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary",
                    Url = new Uri("https://corealign.app/license"),
                },
            });
        }
    }
}
