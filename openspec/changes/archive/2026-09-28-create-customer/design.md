## Context

The foundation (see `openspec/specs/health-check`) provides ASP.NET Core controllers, Serilog, a `GlobalExceptionHandler` that maps every unhandled exception to a 500 problem-details response, and DbUp migrations (`Database/Migrations/*.sql`, embedded resources, run at startup against the `Postgres` connection string). The Domain, Application, and Infrastructure projects contain only `.gitkeep` placeholders; Application and Infrastructure reference Domain, Api references all three. There is no data-access package yet (`Dapper`/`Npgsql` are absent, apart from Npgsql arriving transitively via the health-check package). Conventions come from `docs/architecture.md` and ADR-0001/0002/0003: vertical slices per module, CQRS-lite with explicit SQL, no generic repositories, no infrastructure without a requirement.

This is the first real feature slice, so the structure chosen here becomes the template for Products and Orders. Requirements are in `specs/customer-management/spec.md`; motivation and assumptions in `proposal.md`.

## Goals / Non-Goals

**Goals:**
- Deliver `POST /customers` end-to-end through the four layers with the dependency direction from `architecture.md` (Domain has no dependencies; Application depends on Domain; Infrastructure implements Application abstractions).
- Establish reusable, minimal patterns for: command handling, Dapper data access, mapping a business-rule failure to a non-500 HTTP response, and integration testing against PostgreSQL.
- Guarantee email uniqueness under concurrency.

**Non-Goals:**
- No mediator library, generic repository, unit of work, or pipeline behaviors (ADR-0003).
- No read side (`GET`) — queries arrive with the retrieve/list changes.
- No authentication/authorization.
- No changes to the existing `GlobalExceptionHandler` 500 behavior.

## Decisions

### 1. Layering and slice layout

```
Domain/Customers/Customer.cs                    entity + invariants (factory method)
Application/Customers/Commands/CreateCustomer/  CreateCustomerCommand, CreateCustomerHandler, result type
Application/Customers/ICustomerRepository.cs    persistence abstraction (customer-specific, one method: Add)
Infrastructure/Customers/CustomerRepository.cs  Dapper implementation
Infrastructure/Database/Migrations/0002_CreateCustomers.sql
Api/Customers/CustomersController.cs            HTTP contract + request/response DTOs
```

`Customer` is created through a static factory (`Customer.Create(name, email, phone)`) that trims, normalizes, validates, generates the `Guid` id, and sets `IsActive = true`; it throws a domain validation exception (with per-field errors) on invalid data. The constructor is private so an invalid `Customer` cannot exist.

**Why over alternatives:** Keeping rules in the Domain (not only in DTO attributes) matches architecture.md §8 ("validation at the Api boundary and at the Domain level") and keeps the rules testable without HTTP. A public constructor or anemic entity was rejected because it would let other slices create invalid customers.

### 2. No MediatR; the handler is a plain injected class

`CreateCustomerHandler.HandleAsync(command, ct)` is registered with DI and injected into the controller. **Why:** MediatR adds a dependency (and, currently, licensing considerations) for indirection this project doesn't need; CQRS-lite here is a code-organization convention (Commands/Queries folders, separate SQL), which a plain handler satisfies. **Alternative:** MediatR — rejected under ADR-0003; it can be introduced later if cross-cutting pipeline behaviors become a real need.

### 3. Validation lives in the Domain; the API maps it to problem details

`Customer.Create` is the single authority for the spec's validation rules: it trims `name`, `email`, and `phone`, treats a blank `phone` as absent, and collects **all** field errors (required, max length 200/254/30, email format) before failing with a domain validation exception carrying a per-field error dictionary. The controller catches that exception and returns `ValidationProblem(...)` (400, RFC 7807 `ValidationProblemDetails`), the same shape `[ApiController]` produces for binding errors (e.g., malformed JSON), so clients see one error format. Length limits are constants in the Domain.

The request DTO deliberately carries **no** DataAnnotations length/format attributes: those run on the raw, untrimmed value and would reject e.g. a 201-character value that is 200 characters after trimming, contradicting the spec's "trim before validating" rule, and would duplicate every rule in two places. Missing members simply bind as null and are reported by the domain as required.

**Alternatives:** DataAnnotations at the boundary plus domain re-check — rejected (duplicate rules that can disagree, raw-vs-trimmed mismatch above); FluentValidation — rejected: an extra dependency for three fields. Email format uses a deliberately simple check (single `@`, non-empty local and domain parts, no whitespace), implemented explicitly in the Domain so the rule is unit-testable and independent of framework parsing quirks.

### 4. Duplicate email → 409 via a Result, backed by a unique index

`CreateCustomerHandler` returns a result (success with the customer, or `EmailAlreadyInUse`) instead of throwing for the expected duplicate case; the controller maps it to `409` problem details. Expected business outcomes are not exceptional, so they should not flow through the global 500 handler.

Uniqueness is enforced by the database, not by a check-then-insert:
- Migration `0002_CreateCustomers.sql` creates `customers` with `id uuid primary key`, `name`, `email`, `phone`, `is_active boolean not null default true`, `created_at timestamptz not null default now()`, and `CREATE UNIQUE INDEX ux_customers_email ON customers (lower(email))`. Email is also stored lower-cased by the domain, so the index and normalization agree.
- `CustomerRepository.Add` executes a single parameterized `INSERT` and translates PostgreSQL error `23505` (unique violation) on that index into the "email already in use" outcome. A pre-insert `SELECT EXISTS` is deliberately **not** used: it adds a round trip and still races.

**Why:** this is the only approach that satisfies the spec's concurrent-duplicates scenario. **Alternative:** application-level lock or serializable transaction — rejected as more complex and slower than a unique index.

### 5. Dapper data access

Add `Dapper` and `Npgsql` to `OrderFlow.Infrastructure`. The repository receives an `NpgsqlDataSource` registered once in DI from the existing `ConnectionStrings:Postgres` value (pooling, single configuration source) and opens a connection per call. Snake_case columns are mapped explicitly in the SQL (`AS` aliases / parameter names) rather than enabling global `MatchNamesWithUnderscores`, so the SQL stays fully explicit per ADR-0002. Infrastructure exposes a single `AddInfrastructure(connectionString)`-style registration extension, and Application exposes an `AddApplication()` one, so `Program.cs` stays a short composition root.

**Alternative:** registering the health check's data source implicitly / new `NpgsqlConnection` per call — rejected; a shared `NpgsqlDataSource` is the current Npgsql-recommended approach.

### 6. HTTP contract details

- `POST /customers` returns `201 Created` with a `CustomerResponse` body (`id`, `name`, `email`, `phone`, `isActive`). It uses `Created(...)`/`StatusCode(201, body)` **without** a `Location` header because no `GET /customers/{id}` exists yet; the retrieve change adds it. Documented with `[ProducesResponseType]` for 201/400/409.
- Request and response DTOs live in `OrderFlow.Api/Customers` and are separate from the Domain entity (the API contract must not leak entity internals). A client-supplied `id` is simply not a member of the request DTO, so it is ignored by model binding.
- The 409 uses `Problem(...)`/`ProblemDetails` with a stable `title`, consistent with the existing `AddProblemDetails()` shape.

### 7. Testing

- **Unit (`OrderFlow.UnitTests`):** `Customer.Create` rules (valid, trim, lower-casing, blank/oversized/malformed fields, blank phone → null, always active); `CreateCustomerHandler` with a fake `ICustomerRepository` (success and duplicate-email result).
- **Integration (`OrderFlow.IntegrationTests`):** `WebApplicationFactory<Program>` against the PostgreSQL instance already used by the health-check tests (Docker Compose locally, service container in CI; migrations run at startup). Covers 201, 400 (multiple fields), 409 (including case-different duplicate), the concurrent-duplicate scenario (two parallel requests → one 201, one 409), and that the persisted row exists. Tests use unique generated emails instead of truncating tables, so they are order-independent and can run in parallel with the health-check tests without cleanup coupling.
- The OpenAPI scenario is verified by fetching the Swagger document from the test host and asserting the `POST /customers` operation and response codes.

## Risks / Trade-offs

- **[Duplicate detection depends on PostgreSQL error code 23505 and the index name]** → Match on `SqlState == "23505"` and the constraint/index name `ux_customers_email`, so an unrelated unique violation (e.g., a primary-key collision) is not misreported as a duplicate email; cover with an integration test.
- **[Email format check is intentionally simple]** (accepts some technically odd addresses) → Acceptable for MVP; strict RFC 5322 validation is not a requirement and can be tightened in the Domain in one place.
- **[Integration tests share one database]** → Unique emails per test and no table truncation; a future change can introduce Testcontainers if isolation becomes a problem (not needed now, per ADR-0003).
- **[Pattern set here is copied by later slices]** → Kept deliberately minimal (no base classes, no mediator) so it is cheap to revise after the second slice reveals what actually repeats.
- **[Domain validation exception → 400 mapping in the controller]** → Only the create endpoint needs it today; if it repeats in Products, promote the mapping to the shared exception-handling layer at that point rather than pre-building it.
