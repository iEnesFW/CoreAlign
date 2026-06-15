using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Infrastructure;

public static class Sprint9GroupCRegistration
{
    public static IServiceCollection AddSprint9GroupCInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        return services;
    }
}
