using OrderFlow.Domain.Customers;

namespace OrderFlow.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerHandler(ICustomerRepository repository)
{
    public async Task<CreateCustomerResult> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var customer = Customer.Create(command.Name, command.Email, command.Phone);

        var emailAlreadyInUse = await repository.AddAsync(customer, cancellationToken);

        return emailAlreadyInUse
            ? CreateCustomerResult.EmailInUse()
            : CreateCustomerResult.Created(customer);
    }
}
