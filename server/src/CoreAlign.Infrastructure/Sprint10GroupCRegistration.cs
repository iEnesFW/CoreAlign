using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Infrastructure;

public static class Sprint10GroupCRegistration
{
    public static IServiceCollection AddSprint10GroupCInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        return services;
    }
}
