## Context

The create-customer change delivered the write side of the Customers slice: `Customer` entity (`OrderFlow.Domain/Customers`, private constructor, `Customer.Create` factory), `CreateCustomerHandler` + `ICustomerRepository` (single `AddAsync`) in `OrderFlow.Application/Customers`, a Dapper `CustomerRepository` over a shared `NpgsqlDataSource`, and `CustomersController` with `POST /customers` returning `CustomerResponse` with no `Location` header (the controller comment says so explicitly). `Application/Customers/Queries/` is still a `.gitkeep` placeholder. The `customers` table already has `id uuid PRIMARY KEY`.

`docs/architecture.md` and ADR-0002 prescribe CQRS-lite: Commands and Queries each have their own explicit SQL, even over the same table, and read models are shaped independently of the write model. ADR-0003 forbids generic repositories and speculative infrastructure. This is the first Query in the codebase, so it sets the read-side template for list-customers, Products, and Orders. Requirements are in `specs/customer-management/spec.md`; motivation in `proposal.md`.

## Goals / Non-Goals

**Goals:**
- Establish a minimal, reusable read-side pattern (query + handler + read model + query-side data access) that mirrors the existing command pattern.
- Add the `Location` header to `POST /customers` using the new route, without changing the response body.

**Non-Goals:**
- No changes to the `Customer` entity or the write-side `ICustomerRepository`.
- No shared "not found" exception or global 404 mapping — one endpoint needs it today (ADR-0003).
- No caching, ETags, or projection/field selection.

## Decisions

### 1. Slice layout

```
Application/Customers/Queries/GetCustomerById/GetCustomerByIdQuery.cs     record(Guid Id)
Application/Customers/Queries/GetCustomerById/GetCustomerByIdHandler.cs   returns CustomerDetails?
Application/Customers/Queries/CustomerDetails.cs                          read model record
Application/Customers/Queries/ICustomerQueries.cs                         query-side data access
Infrastructure/Customers/CustomerQueries.cs                               Dapper implementation
Api/Customers/CustomersController.cs                                      new GET action + Location on POST
```

The handler is a plain DI-registered class (same as `CreateCustomerHandler`, no mediator), registered in `AddApplication()`; `ICustomerQueries` is registered in `AddInfrastructure(...)`. `CustomerDetails` and `ICustomerQueries` sit at `Customers/Queries/` (not inside `GetCustomerById/`) because the upcoming list-customers query will reuse them.

The handler is thin (it forwards to `ICustomerQueries.GetByIdAsync`). It exists anyway so the controller depends only on use-case handlers on both sides, keeping one consistent shape per slice.

### 2. The read side returns a read model, not the `Customer` entity

`ICustomerQueries.GetByIdAsync(Guid id, CancellationToken)` returns `CustomerDetails?` (`Id`, `Name`, `Email`, `Phone`, `IsActive`), mapped directly by Dapper from an explicit `SELECT id AS Id, name AS Name, email AS Email, phone AS Phone, is_active AS IsActive FROM customers WHERE id = @Id` (aliases instead of global `MatchNamesWithUnderscores`, as in the write side).

**Why:** this is what architecture.md/ADR-0002 prescribe, and it avoids adding a "reconstitute from storage" path to `Customer` whose private constructor currently guarantees every instance passed creation rules. **Alternative:** add `GetByIdAsync` to `ICustomerRepository` returning `Customer` via a new `Customer.Rehydrate(...)` — rejected: mixes reads into the write abstraction and weakens the entity's invariant guarantee for no benefit (nothing mutates the customer here). That path can be added later when a command (e.g., deactivate) genuinely needs to load the entity.

### 3. Not found is a `null` result, mapped to 404 in the controller

The handler returns `null` when no row matches; the controller returns `Problem(statusCode: 404, title: "Customer not found.")`, consistent with the 409 problem-details shape. Not-found is an expected outcome, so it does not use exceptions or the global 500 handler. A dedicated result type (as in create-customer) is unnecessary because there is only one non-success outcome and `null` expresses it.

### 4. Routing: `{id:guid}` constraint makes malformed ids 404

The action is `[HttpGet("{id:guid}", Name = ...)]` with `Guid id`. With the route constraint, `GET /customers/not-a-guid` matches no route and ASP.NET Core returns 404 — satisfying the spec without custom parsing. **Alternative:** unconstrained `{id}` with manual parsing → 400 — rejected per the proposal's assumption (the URL identifies no resource).

Note: a constraint-miss 404 is produced by routing and has an empty body (no problem details), unlike the "well-formed but unknown id" 404. The spec only requires problem details for the unknown-id case, so this is acceptable; if a uniform body becomes desirable, `UseStatusCodePages` with `AddProblemDetails()` can be added later in one place.

### 5. `Location` on `POST /customers`

Replace `StatusCode(201, body)` with `CreatedAtAction(nameof(GetById), new { id = customer.Id }, body)` (or `CreatedAtRoute` with the named route) so the URL is generated from the actual route and cannot drift. Remove the "No Location header" comment. The body and status code are unchanged, so existing clients are unaffected. Add `[ProducesResponseType<CustomerResponse>(200)]` and `[ProducesResponseType<ProblemDetails>(404)]` to the GET action for OpenAPI.

`CustomerResponse` gains a `From(CustomerDetails)` factory alongside `From(Customer)`, so both endpoints return the identical JSON shape (the spec's "same shape" requirement) from one DTO.

### 6. Testing

- **Unit:** `GetCustomerByIdHandler` with a fake `ICustomerQueries` — returns the details when found, `null` when not.
- **Integration** (new `GetCustomerByIdTests`, same `WebApplicationFactory<Program>` + shared PostgreSQL approach and unique-email strategy as `CreateCustomerTests`): create via `POST`, then `GET` by id → 200 with expected fields; null phone; normalized name/email; `GET` via the `Location` URL equals the POST body; unknown GUID → 404 problem details; `not-a-guid` → 404; OpenAPI documents `GET /customers/{id}` with `id` parameter and 200/404. Extend `CreateCustomerTests` with a `Location` header assertion.

## Risks / Trade-offs

- **[Two 404 shapes: routing miss (empty body) vs unknown id (problem details)]** → Accepted and documented above; spec only constrains the unknown-id case. Cheap to unify later via status-code pages.
- **[Read model duplicates the entity's field list]** → Intentional under CQRS-lite; the read model is free to diverge (e.g., add `createdAt`) without touching the entity.
- **[Thin pass-through handler]** → Small ceremony cost in exchange for a uniform slice shape; revisit if it repeats without ever gaining logic.
- **[`CreatedAtAction` link generation depends on action/route names]** → Covered by the integration test that follows the `Location` header.

## Migration Plan

No schema migration; the primary key index on `customers.id` serves the lookup. Deploy is a normal API release; rollback is redeploying the previous image (the only externally visible additions are the new endpoint and the `Location` header).
