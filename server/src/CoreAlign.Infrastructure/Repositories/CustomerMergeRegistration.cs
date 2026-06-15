using CoreAlign.Application.Customers.Merge;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Infrastructure.Repositories;

public static class CustomerMergeRegistration
{
    public static IServiceCollection AddCustomerMergeServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerMergeOperationRepository, CustomerMergeOperationRepository>();
        services.AddScoped<ICustomerMergeReassignmentService, CustomerMergeReassignmentService>();
        return services;
    }
}
