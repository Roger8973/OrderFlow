using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Customers.Commands.CreateCustomer;

namespace OrderFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCustomerHandler>();

        return services;
    }
}
