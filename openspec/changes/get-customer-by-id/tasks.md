## 1. Application (read side)

- [x] 1.1 Add `CustomerDetails` read model (`Id`, `Name`, `Email`, `Phone`, `IsActive`) and `ICustomerQueries` (single `GetByIdAsync(Guid, CancellationToken)` returning `CustomerDetails?`) under `OrderFlow.Application/Customers/Queries`, replacing the `.gitkeep`; verify `dotnet build` succeeds with no warnings
- [x] 1.2 Add `GetCustomerByIdQuery` and `GetCustomerByIdHandler` under `Customers/Queries/GetCustomerById` and register the handler in `AddApplication()`; verify with unit tests in `OrderFlow.UnitTests/Customers` using a fake `ICustomerQueries`: returns the details when found, `null` when not found

## 2. Infrastructure

- [x] 2.1 Implement `CustomerQueries` in `OrderFlow.Infrastructure/Customers` with Dapper over the shared `NpgsqlDataSource` and an explicit parameterized `SELECT ... FROM customers WHERE id = @Id` using column aliases (no global underscore matching); register it as `ICustomerQueries` in `AddInfrastructure(...)`; verify via the integration tests in task 4.1 and that the API still starts and `GET /health` returns 200

## 3. API

- [x] 3.1 Add `CustomerResponse.From(CustomerDetails)` and a `GET /customers/{id:guid}` action on `CustomersController` returning 200 with `CustomerResponse`, or 404 `Problem(...)` with title "Customer not found." when the handler returns null; add `[ProducesResponseType]` for 200 and 404; verify manually with a request against a running API
- [x] 3.2 Change `POST /customers` to return 201 via `CreatedAtAction`/`CreatedAtRoute` pointing at the new GET action (body unchanged) and remove the "No Location header" comment; verify the `Location` header manually and via task 4.2
- [x] 3.3 Add `GET /customers/{id}` example requests (existing id, unknown GUID, non-GUID) to `OrderFlow.Api/OrderFlow.Api.http` and verify they return 200/404/404 against a running API

## 4. Integration tests

- [x] 4.1 Add `GetCustomerByIdTests` (`WebApplicationFactory<Program>`, shared PostgreSQL, unique email per test, customers created via `POST`) and verify `dotnet test` passes for: 200 with all fields and `isActive` true; 200 with null `phone`; trimmed `name` and lower-cased `email`; `GET` on the `Location` URL returns a body equal to the POST body; 404 with problem-details body for an unknown GUID; 404 for `/customers/not-a-guid`
- [x] 4.2 Extend `CreateCustomerTests` to assert a 201 response has a `Location` header whose path is `/customers/{id}` matching the body's `id`, and verify it passes
- [x] 4.3 Add an OpenAPI test asserting `GET /customers/{id}` documents the `id` path parameter and the 200 and 404 responses, and verify the existing `POST /customers` OpenAPI test still passes

## 5. Final verification

- [x] 5.1 Run `dotnet build --configuration Release` and `dotnet test --configuration Release` from the solution root (with Docker Compose PostgreSQL running) and verify all existing and new tests pass
- [x] 5.2 Run `docker compose up --build`, create a customer with `POST http://localhost:8080/customers`, follow its `Location` header with `GET`, and verify a 200 with the same body; verify an unknown id returns 404
- [x] 5.3 Run `openspec validate get-customer-by-id --strict` and verify it passes
