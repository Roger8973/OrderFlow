using OrderFlow.Domain.Customers;

namespace OrderFlow.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerResult
{
    private CreateCustomerResult(Customer? customer)
    {
        Customer = customer;
    }

    public Customer? Customer { get; }

    public bool EmailAlreadyInUse => Customer is null;

    public static CreateCustomerResult Created(Customer customer) => new(customer);

    public static CreateCustomerResult EmailInUse() => new(null);
}
