using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OrderFlow.IntegrationTests;

public class GetCustomerByIdTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Unique per call so tests share one database without cleanup and can run in parallel.
    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Get_Returns200WithCustomer_WhenCustomerExists()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();
        var id = await CreateCustomerAsync(client, new { name = "Maria Silva", email, phone = "+55 11 99999-0000" });

        var response = await client.GetAsync($"/customers/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Maria Silva", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal("+55 11 99999-0000", body.RootElement.GetProperty("phone").GetString());
        Assert.True(body.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Get_ReturnsNullPhone_WhenCustomerHasNoPhone()
    {
        using var client = factory.CreateClient();
        var id = await CreateCustomerAsync(client, new { name = "Maria", email = NewEmail() });

        var response = await client.GetAsync($"/customers/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("phone").ValueKind);
    }

    [Fact]
    public async Task Get_ReflectsNormalizationAppliedAtCreation()
    {
        using var client = factory.CreateClient();
        var email = NewEmail();
        var id = await CreateCustomerAsync(client, new { name = "  Maria Silva  ", email = email.ToUpperInvariant() });

        var response = await client.GetAsync($"/customers/{id}");

        using var body = await ReadJsonAsync(response);
        Assert.Equal("Maria Silva", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Get_ReturnsSameBodyAsCreation_WhenFollowingLocationHeader()
    {
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync(
            "/customers",
            new { name = "Maria Silva", email = NewEmail(), phone = "123" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await created.Content.ReadAsStringAsync();

        var response = await client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var expected = JsonDocument.Parse(createdBody);
        using var actual = await ReadJsonAsync(response);
        Assert.Equal(expected.RootElement.GetRawText(), actual.RootElement.GetRawText());
    }

    [Fact]
    public async Task Get_Returns404WithProblemDetails_WhenNoCustomerHasTheId()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Customer not found.", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_Returns404_WhenIdIsNotAGuid()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/customers/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesGetCustomerByIdEndpoint()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        var operation = document.RootElement.GetProperty("paths").GetProperty("/customers/{id}").GetProperty("get");
        var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("id", parameter.GetProperty("name").GetString());
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("404", out _));
    }

    private static async Task<Guid> CreateCustomerAsync(HttpClient client, object request)
    {
        var response = await client.PostAsJsonAsync("/customers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
