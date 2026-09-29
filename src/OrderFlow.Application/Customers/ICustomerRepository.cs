using OrderFlow.Domain.Customers;

namespace OrderFlow.Application.Customers;

public interface ICustomerRepository
{
    /// <summary>
    /// Persists the customer. Returns <c>true</c> if the customer was NOT stored because
    /// its email is already in use by another customer; <c>false</c> if it was stored.
    /// </summary>
    Task<bool> AddAsync(Customer customer, CancellationToken cancellationToken);
}
