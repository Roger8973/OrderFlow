## Why

Customers are the first business entity in OrderFlow's roadmap (step 2) and a prerequisite for Orders: João, the salesperson, must be able to register a customer before creating an order for them. The project foundation is in place (API, PostgreSQL, DbUp migrations, health check) but has no application schema and no business endpoints yet, so this change delivers the first vertical slice end-to-end.

## What Changes

- Add `POST /customers` to register a new customer, returning `201 Created` with the created customer.
- Introduce the `Customer` domain entity, enforcing its invariants (required name, well-formed email, optional phone) and starting every customer as **active**.
- Enforce email uniqueness (case-insensitive): a second customer with the same email is rejected with `409 Conflict`, including under concurrent requests (backed by a database unique index, not only an application check).
- Return `400 Bad Request` with problem details (RFC 7807) for invalid input, consistent with the existing problem-details error shape.
- Add the first application schema migration (`customers` table) through the existing DbUp pipeline.
- Introduce the first Dapper-based persistence and the first Command in the Customers vertical slice (CQRS-lite), establishing the pattern later slices (retrieve, list, deactivate, Products, Orders) will follow.
- Add unit tests (domain rules, command handler) and integration tests (endpoint against PostgreSQL).

**Assumptions** (recorded here rather than asked, since they are reversible details):

- Customer fields: `name` (required, max 200 chars), `email` (required, max 254 chars, unique), `phone` (optional, max 30 chars). No address or tax-ID fields until a real need appears (see ADR-0003).
- The customer id is a server-generated GUID; clients cannot supply it.
- Email is trimmed and compared case-insensitively (stored lower-cased).
- No authentication is required yet — the Authentication module is a later roadmap step.
- The `201` response omits a `Location` header because `GET /customers/{id}` does not exist yet; it will be added by the retrieve-customer change.
- Retrieve, list, and deactivate are out of scope for this change (separate changes, per the roadmap).

## Capabilities

### New Capabilities
- `customer-management`: Registering customers — the create-customer API contract, validation rules, email uniqueness, and default active state. Later customer changes (retrieve, list, deactivate) will extend this same capability.

### Modified Capabilities
<!-- None. The existing `health-check` requirements are unchanged. -->

## Impact

- **API**: new `POST /customers` endpoint (`OrderFlow.Api/Customers`), documented in Swagger.
- **Application**: `CreateCustomer` command and handler (`OrderFlow.Application/Customers/Commands`) plus a customer persistence abstraction.
- **Domain**: `Customer` entity (`OrderFlow.Domain/Customers`).
- **Infrastructure**: Dapper-based customer persistence (`OrderFlow.Infrastructure/Customers`) and migration `0002_CreateCustomers.sql` under `Database/Migrations`.
- **Dependencies**: adds `Dapper` and `Npgsql` (the latter is already a transitive dependency of the health check package) to `OrderFlow.Infrastructure`.
- **Tests**: new unit and integration tests; integration tests require the PostgreSQL instance already provided by Docker Compose and the CI workflow.
- **Docs**: none required; `docs/architecture.md` already describes this structure.
