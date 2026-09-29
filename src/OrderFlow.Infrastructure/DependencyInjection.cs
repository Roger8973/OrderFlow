using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OrderFlow.Application.Customers;
using OrderFlow.Infrastructure.Customers;

namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }
}
