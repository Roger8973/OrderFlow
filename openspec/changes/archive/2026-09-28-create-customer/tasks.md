## 1. Database and dependencies

- [x] 1.1 Add `Dapper` and `Npgsql` package references to `OrderFlow.Infrastructure` and verify `dotnet build` succeeds with no warnings (warnings are errors)
- [x] 1.2 Add embedded migration `Database/Migrations/0002_CreateCustomers.sql` creating `customers` (`id uuid` PK, `name`, `email`, `phone` nullable, `is_active` default true, `created_at` timestamptz default now()) and unique index `ux_customers_email` on `lower(email)`; verify by starting the API against Docker Compose PostgreSQL and inspecting the table and index with `psql \d customers`

## 2. Domain

- [x] 2.1 Add `Customer` in `OrderFlow.Domain/Customers` with a private constructor and `Customer.Create(name, email, phone)` that generates the id, trims inputs, lower-cases email, treats blank phone as null, sets `IsActive = true`, and exposes the 200/254/30 length limits as constants
- [x] 2.2 Add a domain validation exception carrying per-field errors; `Customer.Create` collects all errors (required name, required/well-formed email, max lengths) and throws once; verify with unit tests in `OrderFlow.UnitTests` covering: valid create, trimming, lower-cased email, blank/missing/oversized name, malformed/oversized email, oversized phone, blank phone → null, multiple simultaneous errors, and always-active

## 3. Application

- [x] 3.1 Define `ICustomerRepository` (single `AddAsync` method returning whether the email was already in use) and the `CreateCustomerCommand`/`CreateCustomerHandler`/result type in `OrderFlow.Application/Customers`; the handler calls `Customer.Create` and returns a created-or-`EmailAlreadyInUse` result; verify with unit tests using a fake repository for both outcomes and for domain validation failures propagating
- [x] 3.2 Add an `AddApplication()` service registration extension and verify the handler resolves from DI (covered by the integration tests in task 5)

## 4. Infrastructure

- [x] 4.1 Implement `CustomerRepository` with Dapper and explicit parameterized `INSERT` SQL over a shared `NpgsqlDataSource`; translate PostgreSQL error `23505` on `ux_customers_email` (only) into the email-already-in-use outcome and let every other exception propagate; verify with integration tests in task 5
- [x] 4.2 Add an `AddInfrastructure(connectionString)` registration extension (registering `NpgsqlDataSource` and `ICustomerRepository`) and wire `AddApplication()`/`AddInfrastructure(...)` in `Program.cs` using the existing `Postgres` connection string; verify the API still starts and `GET /health` still returns 200

## 5. API

- [x] 5.1 Add request/response DTOs (no validation attributes; `id` is not part of the request) and `CustomersController` with `POST /customers`: 201 with `CustomerResponse` (no `Location` header), 400 `ValidationProblemDetails` on domain validation failure, 409 problem details on duplicate email; add `[ProducesResponseType]` for 201/400/409; verify manually via `OrderFlow.Api.http` requests against a running API
- [x] 5.2 Replace the stale `weatherforecast` sample request in `OrderFlow.Api/OrderFlow.Api.http` with `POST /customers` example requests (valid, invalid, duplicate) and verify the file's requests run

## 6. Integration tests

- [x] 6.1 Add integration tests (`WebApplicationFactory<Program>` against the Docker Compose/CI PostgreSQL, unique generated email per test) and verify `dotnet test` passes for: 201 with all fields; 201 without phone (null phone); client-supplied `id` ignored; `isActive` true; whitespace trimming; lower-cased email; 400 for missing/blank name, malformed email, oversized fields, and multiple simultaneous invalid fields (errors reported per field, nothing persisted)
- [x] 6.2 Add integration tests for uniqueness and persistence and verify they pass: 409 on duplicate email, 409 on case-different duplicate, exactly one 201 and one 409 for two parallel requests with the same email, and the created row present in the `customers` table
- [x] 6.3 Add an integration test that fetches the Swagger/OpenAPI document and asserts `POST /customers` documents the request body and 201, 400, and 409 responses

## 7. Final verification

- [x] 7.1 Run `dotnet build --configuration Release` and `dotnet test --configuration Release` from the solution root (with Docker Compose PostgreSQL running) and verify all existing and new tests pass
- [x] 7.2 Run `docker compose up --build`, create a customer with `POST http://localhost:8080/customers`, restart the `api` container, and verify re-posting the same email returns 409 (persistence across restart)
- [x] 7.3 Run `openspec validate create-customer --strict` and verify it passes
