# OrderFlow — Architecture

## 1. Purpose & Scope

This document describes the technical architecture of OrderFlow: the architectural
style, the layers and module boundaries, the data access strategy, and how the
architecture is expected to evolve. It elaborates on the technical/portfolio goals and
Guiding Principles already defined in [product-vision.md](product-vision.md) — it does
not introduce new product decisions.

## 2. Architectural Style

OrderFlow is built as a **Modular Monolith** using **Vertical Slice Architecture**, with
a pragmatic Clean Architecture layering applied *within* each slice.

- **Modular monolith** — a single deployable unit, but internally organized into
  independent feature modules (Customers, Products, Orders, Auth, Dashboard) with clear
  boundaries between them.
- **Vertical slices** — each feature is implemented end-to-end (API endpoint, use case,
  domain rules, persistence) as a self-contained unit, instead of being spread thin
  across horizontal technical layers shared by every feature.
- **Pragmatic Clean Architecture** — within a slice, the Domain has no dependency on
  Infrastructure, and business rules are not coupled to ASP.NET Core or Dapper. This is
  applied without a full DDD toolkit (no generic repositories, specifications, or unit-
  of-work abstractions that the project doesn't need).

This combination gives the project real separation of concerns and testability today,
while keeping deployment and operations as simple as a single service.

## 3. Why Not Microservices (Yet)

Microservices are explicitly out of scope for the MVP (see product-vision.md's
Non-Goals), and this is a deliberate architectural decision, not an oversight:

- **No independent scaling need.** No module has a load profile that justifies scaling
  separately from the others.
- **No independent deployment need.** There is a single team (currently one developer)
  and no requirement to deploy modules on different cadences.
- **Tight domain relationships.** Orders depends on Customers and Products; splitting
  these into services now would introduce network calls and distributed consistency
  problems in exchange for no real benefit.
- **Operational cost isn't justified.** Service discovery, distributed tracing,
  network resilience (retries, circuit breakers), and eventual consistency are real
  costs that only pay off once a genuine scaling or team-topology need exists.

**This is "not now," not "never."** Because the monolith is already organized into
vertical slices with explicit module boundaries (Section 6), a module could be extracted
into its own service later with low friction — *if* a concrete requirement justifies it,
for example:

- a module needs to scale or deploy independently of the rest of the system;
- asynchronous processing is needed (e.g., notifications or inventory updates after an
  order is confirmed) — the first response to this is introducing a message broker and
  worker processes (see Section 9), not a full microservice split.

## 4. Current Architecture (Phase 1)

```
                    ┌──────────────┐
                    │    Client    │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │ ASP.NET API  │
                    └──────┬───────┘
                           │
             ┌─────────────┼─────────────┐
             ▼             ▼             ▼
       Application      Domain     Infrastructure
             │                         │
             └──────────┬──────────────┘
                        ▼
                   PostgreSQL
```

A single ASP.NET Core Web API project, with Application, Domain, and Infrastructure
organized as internal modules (not separate deployables), backed by a single PostgreSQL
database accessed through Dapper.

## 5. Layers & Responsibilities

- **Api** — HTTP entry point. Routing, request/response contracts, input validation at
  the boundary, authentication/authorization enforcement, Swagger documentation.
- **Application** — orchestrates use cases (commands and queries), coordinates Domain
  and Infrastructure, contains no HTTP or SQL concerns.
- **Domain** — entities, value objects, and business rules (e.g., the order lifecycle).
  Has no dependency on Application, Infrastructure, or any framework.
- **Infrastructure** — persistence (Dapper + PostgreSQL), external concerns. Implements
  interfaces defined by Application/Domain.

## 6. Module Boundaries (Vertical Slices)

Each layer is internally organized by feature module, not just by technical concern.
This is what keeps the monolith "modular" in practice, and what would make extracting a
module later (if ever needed) a low-friction move rather than a rewrite:

```
src/
├── OrderFlow.Api/
│   ├── Customers/
│   ├── Products/
│   ├── Orders/
│   ├── Auth/
│   └── Dashboard/
├── OrderFlow.Application/
│   ├── Customers/
│   │   ├── Commands/
│   │   └── Queries/
│   ├── Products/
│   ├── Orders/
│   ├── Auth/
│   └── Dashboard/
├── OrderFlow.Domain/
│   ├── Customers/
│   ├── Products/
│   └── Orders/
└── OrderFlow.Infrastructure/
    ├── Customers/
    ├── Products/
    ├── Orders/
    └── Auth/
```

A module may depend on another module's public interface (e.g., Orders depends on
Customers and Products), but never reaches into another module's internal
implementation details.

## 7. Data Access Strategy

- **Dapper + PostgreSQL** for all data access (see the rationale to be recorded in
  ADR-0002) — explicit SQL, no change tracking, minimal abstraction overhead.
- **CQRS-lite**, applied consistently across every vertical slice: Commands perform
  writes, Queries perform reads, each with its own explicit SQL — even where the same
  table is involved. This keeps read models free to be shaped for the API/dashboard
  without distorting the write model.
- **Migrations** run through versioned SQL scripts (or a lightweight runner such as
  DbUp), applied automatically in the Docker Compose / CI environment.

## 8. Cross-Cutting Concerns

These are part of Phase 1 — they are baseline engineering practice, not future
complexity:

- **Authentication** — JWT-based, evolving into role-based authorization (Admin,
  Salesperson, Manager) as defined in the roadmap.
- **Validation** — enforced at the Api boundary (request contracts) and at the Domain
  level (business invariants).
- **Error handling** — a consistent problem-details response shape across the API.
- **Health checks** — `/health` endpoint covering the API and its PostgreSQL
  dependency, for use in Docker Compose and CI.
- **Logging** — structured logging from day one (e.g., Serilog), so that Phase 4
  (Section 9) is about adding metrics/tracing on top of an existing foundation, not
  introducing logging from scratch.

## 9. Architecture Evolution

The system intentionally starts as a modular monolith. Additional infrastructure is
introduced only when a concrete business or non-functional requirement justifies its
complexity — never in anticipation of hypothetical future needs.

| Requirement that appears | Architectural response |
|---|---|
| Confirming an order must trigger a notification and/or an inventory update | Introduce a message broker (e.g., RabbitMQ) and dedicated worker processes consuming order events — not a service split |
| The API must sustain a specific high request volume | Introduce a load balancer and run multiple API instances behind it; revisit session/state assumptions |
| Multiple API instances need shared, low-latency state | Introduce a distributed cache (e.g., Redis) |
| Production issues require deeper insight into system behavior | Add metrics, distributed tracing, and dashboards on top of the existing structured logging |
| A module needs to scale or deploy independently of the rest | Reconsider extracting that module (already isolated as a vertical slice) into its own service |

**Phase 2 — Asynchronous processing** (triggered by a real notification/inventory
requirement):

```
ASP.NET API → OrderFlow (Order Confirmed) → Message Broker → Notification Worker
                                                            → Inventory Worker
```

**Phase 3 — Horizontal scaling** (triggered by a real load requirement):

```
Load Balancer → API instances (N) → PostgreSQL
                                   → Redis (shared cache/state, if needed)
```

**Phase 4 — Observability** (triggered by a real operational need):

```
API → Logs (already in Phase 1) + Metrics + Traces + Health Checks (already in Phase 1)
```

Each of these phases must be preceded by its own spec explaining the requirement that
justifies it — they are not scheduled work, they are conditional responses.

## 10. Related Documents

- [product-vision.md](product-vision.md) — product goals, roadmap, and guiding
  principles this document elaborates on.
- `docs/decisions/` — Architecture Decision Records with the detailed rationale behind
  specific choices (to be created):
  - ADR-0001: Modular Monolith with Vertical Slices (over microservices)
  - ADR-0002: Dapper over EF Core
  - ADR-0003: Complexity introduced on demand
- `specs/` — feature specs, starting with `001-project-foundation.md`, each describing
  one increment of the system built on top of this architecture.
