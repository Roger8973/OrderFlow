using Dapper;
using Npgsql;
using OrderFlow.Application.Customers.Queries;

namespace OrderFlow.Infrastructure.Customers;

public sealed class CustomerQueries(NpgsqlDataSource dataSource) : ICustomerQueries
{
    private const string SelectByIdSql = """
        SELECT id AS Id, name AS Name, email AS Email, phone AS Phone, is_active AS IsActive
        FROM customers
        WHERE id = @Id
        """;

    public async Task<CustomerDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CustomerDetails>(new CommandDefinition(
            SelectByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));
    }
}
