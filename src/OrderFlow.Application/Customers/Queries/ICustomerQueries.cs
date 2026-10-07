namespace OrderFlow.Application.Customers.Queries;

public interface ICustomerQueries
{
    Task<CustomerDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
