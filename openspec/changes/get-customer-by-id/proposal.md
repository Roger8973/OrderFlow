## Why

Customers can be registered (`POST /customers`) but never read back: a client that has a customer's id has no way to see its current data, and step 1 of João's workflow ("find the customer") is unsupported. Retrieval by id is the next item of the Customer Management roadmap step and the read-side prerequisite for Orders, which will reference customers by id. It also lets `POST /customers` return the `Location` header that the create-customer change deferred until this endpoint existed.

## What Changes

- Add `GET /customers/{id}` returning `200 OK` with the customer (`id`, `name`, `email`, `phone`, `isActive`) — the same shape `POST /customers` already returns.
- Return `404 Not Found` with an RFC 7807 problem-details body when no customer has that id, including when the path segment is not a GUID.
- `POST /customers` now includes a `Location` header pointing at the new customer's `GET /customers/{id}` URL on `201 Created` (additive; the response body is unchanged).
- Introduce the first Query in the Customers slice (CQRS-lite read side) with explicit Dapper `SELECT` SQL, establishing the read pattern later slices (list customers, Products, Orders) will follow.
- Document the new endpoint and its 200/404 responses in the OpenAPI document.
- Add unit and integration tests for the new behavior.

**Assumptions** (recorded here rather than asked, since they are reversible details):

- Inactive customers are still retrievable by id; the response reports `isActive` so callers can decide. (Deactivation does not exist yet, so today every customer is active.)
- A malformed id (not a GUID) is answered as `404`, not `400`: the resource identified by that URL does not exist.
- No authentication is required yet — the Authentication module is a later roadmap step.
- No caching, ETags, or conditional requests (ADR-0003).
- Listing, searching, and deactivating customers remain out of scope (separate changes).

## Capabilities

### New Capabilities
<!-- None. Retrieval extends the existing customer-management capability. -->

### Modified Capabilities
- `customer-management`: adds requirements for retrieving a customer by id (200/404 contract and OpenAPI documentation), and modifies the create-customer API requirement so a `201` response carries a `Location` header for the created customer.

## Impact

- **API**: new `GET /customers/{id}` action on `CustomersController`; `POST /customers` gains a `Location` header; `OrderFlow.Api.http` gains example retrieval requests.
- **Application**: new `GetCustomerById` query, handler, read model, and a query-side data-access abstraction under `OrderFlow.Application/Customers/Queries` (the write-side `ICustomerRepository` is unchanged).
- **Domain**: none — the read side returns a read model, not the `Customer` entity.
- **Infrastructure**: a Dapper implementation of the query abstraction with an explicit parameterized `SELECT` by id. No migration — the existing primary key on `customers.id` serves the lookup.
- **Dependencies**: none.
- **Tests**: new unit tests (handler) and integration tests (200, 404, Location header, OpenAPI) against the existing Docker Compose/CI PostgreSQL.
