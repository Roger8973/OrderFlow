namespace OrderFlow.Api.Customers;

// No validation attributes on purpose: the Domain is the single authority for the rules
// and validates the trimmed values (see design.md, decision 3).
public sealed record CreateCustomerRequest(string? Name, string? Email, string? Phone);
