using OrderFlow.Application.Customers.Queries;
using OrderFlow.Application.Customers.Queries.GetCustomerById;

namespace OrderFlow.UnitTests.Customers;

public class GetCustomerByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDetails_WhenCustomerExists()
    {
        var details = new CustomerDetails(Guid.NewGuid(), "Maria", "maria@example.com", null, true);
        var handler = new GetCustomerByIdHandler(new FakeCustomerQueries(details));

        var result = await handler.HandleAsync(new GetCustomerByIdQuery(details.Id), CancellationToken.None);

        Assert.Same(details, result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenCustomerDoesNotExist()
    {
        var handler = new GetCustomerByIdHandler(new FakeCustomerQueries(null));

        var result = await handler.HandleAsync(new GetCustomerByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class FakeCustomerQueries(CustomerDetails? stored) : ICustomerQueries
    {
        public Task<CustomerDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(stored is not null && stored.Id == id ? stored : null);
    }
}
