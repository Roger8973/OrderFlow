using OrderFlow.Application.Customers;
using OrderFlow.Application.Customers.Commands.CreateCustomer;
using OrderFlow.Domain;
using OrderFlow.Domain.Customers;

namespace OrderFlow.UnitTests.Customers;

public class CreateCustomerHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsCreated_AndStoresCustomer_WhenEmailIsFree()
    {
        var repository = new FakeCustomerRepository(emailAlreadyInUse: false);
        var handler = new CreateCustomerHandler(repository);

        var result = await handler.HandleAsync(
            new CreateCustomerCommand(" Maria ", "Maria@Example.com", null),
            CancellationToken.None);

        Assert.False(result.EmailAlreadyInUse);
        Assert.NotNull(result.Customer);
        Assert.Equal("Maria", result.Customer.Name);
        Assert.Equal("maria@example.com", result.Customer.Email);
        Assert.Same(result.Customer, Assert.Single(repository.Added));
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmailAlreadyInUse_WhenRepositoryReportsDuplicate()
    {
        var handler = new CreateCustomerHandler(new FakeCustomerRepository(emailAlreadyInUse: true));

        var result = await handler.HandleAsync(
            new CreateCustomerCommand("Maria", "maria@example.com", null),
            CancellationToken.None);

        Assert.True(result.EmailAlreadyInUse);
        Assert.Null(result.Customer);
    }

    [Fact]
    public async Task HandleAsync_PropagatesDomainValidationFailure_WithoutTouchingRepository()
    {
        var repository = new FakeCustomerRepository(emailAlreadyInUse: false);
        var handler = new CreateCustomerHandler(repository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => handler.HandleAsync(
            new CreateCustomerCommand("", "not-an-email", null),
            CancellationToken.None));

        Assert.Contains("name", exception.Errors.Keys);
        Assert.Contains("email", exception.Errors.Keys);
        Assert.Empty(repository.Added);
    }

    private sealed class FakeCustomerRepository(bool emailAlreadyInUse) : ICustomerRepository
    {
        public List<Customer> Added { get; } = [];

        public Task<bool> AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            if (!emailAlreadyInUse)
            {
                Added.Add(customer);
            }

            return Task.FromResult(emailAlreadyInUse);
        }
    }
}
