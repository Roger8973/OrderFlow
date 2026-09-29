using Dapper;
using Npgsql;
using OrderFlow.Application.Customers;
using OrderFlow.Domain.Customers;

namespace OrderFlow.Infrastructure.Customers;

public sealed class CustomerRepository(NpgsqlDataSource dataSource) : ICustomerRepository
{
    private const string UniqueViolation = "23505";
    private const string EmailIndexName = "ux_customers_email";

    private const string InsertSql = """
        INSERT INTO customers (id, name, email, phone, is_active)
        VALUES (@Id, @Name, @Email, @Phone, @IsActive)
        """;

    public async Task<bool> AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertSql,
                new
                {
                    customer.Id,
                    customer.Name,
                    customer.Email,
                    customer.Phone,
                    customer.IsActive,
                },
                cancellationToken: cancellationToken));

            return false;
        }
        catch (PostgresException ex) when (ex.SqlState == UniqueViolation && ex.ConstraintName == EmailIndexName)
        {
            return true;
        }
    }
}
