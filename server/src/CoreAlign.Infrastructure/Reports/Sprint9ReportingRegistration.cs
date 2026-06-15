using CoreAlign.Application.Jobs;
using CoreAlign.Application.Reports.Custom;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Infrastructure.Reports;

public static class Sprint9ReportingRegistration
{
    public static IServiceCollection AddSprint9Reporting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();
        services.AddScoped<IReportScheduleRepository, ReportScheduleRepository>();
        services.AddScoped<IFieldCatalogService, FieldCatalogService>();
        services.AddScoped<ICustomReportExecutor, CustomReportExecutor>();
        services.AddScoped<ReportScheduleJob>();
        return services;
    }
}
