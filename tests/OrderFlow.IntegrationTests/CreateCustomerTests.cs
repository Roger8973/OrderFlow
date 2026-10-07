using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace OrderFlow.IntegrationTests;

public class CreateCustomerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Unique per call so tests share one database without cleanup and can run in parallel.
    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Post_Returns201WithCustomer_WhenAllFieldsProvided()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria Silva", email, phone = "+55 11 99999-0000" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.NotEqual(Guid.Empty, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Maria Silva", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal("+55 11 99999-0000", body.RootElement.GetProperty("phone").GetString());
    }

    [Fact]
    public async Task Post_ReturnsLocationHeader_PointingAtCreatedCustomer()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria", email = NewEmail() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        var id = body.RootElement.GetProperty("id").GetGuid();
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/customers/{id}", response.Headers.Location.AbsolutePath);
    }

    [Fact]
    public async Task Post_Returns201WithNullPhone_WhenPhoneOmitted()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria", email = NewEmail() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("phone").ValueKind);
    }

    [Fact]
    public async Task Post_IgnoresClientSuppliedId()
    {
        using var client = factory.CreateClient();
        var suppliedId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/customers", new { id = suppliedId, name = "Maria", email = NewEmail() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.NotEqual(suppliedId, body.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_CreatesActiveCustomer()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria", email = NewEmail() });

        using var body = await ReadJsonAsync(response);
        Assert.True(body.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Post_TrimsSurroundingWhitespace()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/customers", new { name = "  Maria Silva  ", email = $" {email} " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal("Maria Silva", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Post_ReportsEmailInLowerCase()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria", email = email.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_Returns400_WhenNameMissingOrBlank(string? name)
    {
        using var client = factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/customers", new { name, email });

        await AssertValidationProblemAsync(response, "name");
        Assert.Equal(0, await CountByEmailAsync(email));
    }

    [Fact]
    public async Task Post_Returns400_WhenNameOmitted()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new { email = NewEmail() });

        await AssertValidationProblemAsync(response, "name");
    }

    [Fact]
    public async Task Post_Returns400_WhenEmailMalformed()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria", email = "not-an-email" });

        await AssertValidationProblemAsync(response, "email");
    }

    [Fact]
    public async Task Post_Returns400_WhenFieldsTooLong()
    {
        using var client = factory.CreateClient();
        var email = new string('a', 250) + "@example.com";

        var response = await client.PostAsJsonAsync(
            "/customers",
            new { name = new string('a', 201), email, phone = new string('1', 31) });

        await AssertValidationProblemAsync(response, "name", "email", "phone");
        Assert.Equal(0, await CountByEmailAsync(email));
    }

    [Fact]
    public async Task Post_Returns400WithAllErrors_WhenMultipleFieldsInvalid()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new { name = "   ", email = "not-an-email" });

        await AssertValidationProblemAsync(response, "name", "email");
    }

    [Fact]
    public async Task Post_Returns409_WhenEmailAlreadyInUse()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/customers", new { name = "Maria", email });

        var response = await client.PostAsJsonAsync("/customers", new { name = "Outra Maria", email });

        await AssertConflictAsync(response);
        Assert.Equal(1, await CountByEmailAsync(email));
    }

    [Fact]
    public async Task Post_Returns409_WhenEmailDiffersOnlyByCase()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/customers", new { name = "Maria", email });

        var response = await client.PostAsJsonAsync("/customers", new { name = "Outra Maria", email = email.ToUpperInvariant() });

        await AssertConflictAsync(response);
    }

    [Fact]
    public async Task Post_ReturnsExactlyOne201AndOne409_ForParallelRequestsWithSameEmail()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/customers", new { name = "Maria A", email }),
            client.PostAsJsonAsync("/customers", new { name = "Maria B", email }));

        var statuses = responses.Select(r => r.StatusCode).Order().ToArray();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);
        Assert.Equal(1, await CountByEmailAsync(email));
    }

    [Fact]
    public async Task Post_PersistsCustomerInDatabase()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/customers", new { name = "Maria Silva", email, phone = "123" });

        using var body = await ReadJsonAsync(response);
        var id = body.RootElement.GetProperty("id").GetGuid();

        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT name, email, phone, is_active FROM customers WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("Maria Silva", reader.GetString(0));
        Assert.Equal(email, reader.GetString(1));
        Assert.Equal("123", reader.GetString(2));
        Assert.True(reader.GetBoolean(3));
    }

    [Fact]
    public async Task OpenApiDocument_DescribesCreateCustomerEndpoint()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        var operation = document.RootElement.GetProperty("paths").GetProperty("/customers").GetProperty("post");
        Assert.True(operation.TryGetProperty("requestBody", out _));
        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("409", out _));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, params string[] fields)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        var errors = body.RootElement.GetProperty("errors");
        foreach (var field in fields)
        {
            Assert.True(errors.TryGetProperty(field, out _), $"Expected a validation error for '{field}'.");
        }

        Assert.Equal(fields.Length, errors.EnumerateObject().Count());
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(409, body.RootElement.GetProperty("status").GetInt32());
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Postgres");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private async Task<long> CountByEmailAsync(string email)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM customers WHERE lower(email) = lower(@email)", connection);
        command.Parameters.AddWithValue("email", email);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
