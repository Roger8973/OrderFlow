using OrderFlow.Application.Customers.Queries;
using OrderFlow.Domain.Customers;

namespace OrderFlow.Api.Customers;

public sealed record CustomerResponse(Guid Id, string Name, string Email, string? Phone, bool IsActive)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Phone, customer.IsActive);

    public static CustomerResponse From(CustomerDetails customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Phone, customer.IsActive);
}
