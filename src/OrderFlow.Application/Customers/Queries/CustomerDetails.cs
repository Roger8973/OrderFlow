namespace OrderFlow.Application.Customers.Queries;

public sealed record CustomerDetails(Guid Id, string Name, string Email, string? Phone, bool IsActive);
