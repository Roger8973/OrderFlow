namespace OrderFlow.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(string? Name, string? Email, string? Phone);
