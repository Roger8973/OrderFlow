using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Customers.Commands.CreateCustomer;
using OrderFlow.Application.Customers.Queries.GetCustomerById;

namespace OrderFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<GetCustomerByIdHandler>();

        return services;
    }
}
