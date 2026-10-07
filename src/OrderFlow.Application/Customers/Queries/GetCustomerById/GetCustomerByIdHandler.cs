namespace OrderFlow.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler(ICustomerQueries queries)
{
    public Task<CustomerDetails?> HandleAsync(GetCustomerByIdQuery query, CancellationToken cancellationToken) =>
        queries.GetByIdAsync(query.Id, cancellationToken);
}
