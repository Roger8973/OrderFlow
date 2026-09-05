## Context

The repository currently has product and architecture documentation only (`docs/product-vision.md`, `docs/architecture.md`, ADRs 0001-0003) — no `.sln`, no projects, no CI. `docs/architecture.md` already fixes the target shape: a modular monolith with vertical slices (ADR-0001), Dapper + PostgreSQL with CQRS-lite and SQL-script migrations (ADR-0002), and complexity introduced only on demand (ADR-0003). This design turns that documented, already-accepted architecture into an actual runnable skeleton — it does not re-litigate those decisions.

## Goals / Non-Goals

**Goals:**
- Produce a solution whose folder/project layout matches `docs/architecture.md` Section 6 exactly, so the first real feature (Customer Management) adds code into already-established locations instead of also having to invent the skeleton.
- Make the system runnable end-to-end locally with one command (API + PostgreSQL via Docker Compose, schema created by migrations on startup).
- Establish, once, the conventions every later vertical slice will reuse: structured logging, problem-details error shape, API documentation, and a build+test CI pipeline.

**Non-Goals:**
- No domain/business logic for Customers, Products, Orders, Auth, or Dashboard — their module folders exist as empty placeholders only; behavior is defined by their own future specs.
- No real authentication/authorization — that is roadmap item 5. The `/health` endpoint is intentionally unauthenticated.
- No production hosting/cloud infrastructure — Docker Compose targets local development and CI only.

## Decisions

**1. Solution layout mirrors architecture.md Section 6 directly.**
`OrderFlow.Api` (ASP.NET Core Web API), `OrderFlow.Application`, `OrderFlow.Domain`, `OrderFlow.Infrastructure` class libraries, each pre-populated with the empty module folders shown in the architecture diagram (`Customers/`, `Products/`, `Orders/`, `Auth/`, `Dashboard/`, with `Auth` omitted from `Domain` since it has no domain entities of its own). One `OrderFlow.UnitTests` and one `OrderFlow.IntegrationTests` project are added at the solution level rather than one test project per module — there is no module code yet to justify splitting, and splitting later is low-cost once a module actually exists (ADR-0003's complexity-on-demand principle applied to test structure too).

**2. Schema migrations via DbUp, run automatically on API startup.**
Versioned, embedded `.sql` scripts applied by DbUp against the configured connection string, executed once when the API host starts. This keeps migrations Dapper-native (no EF Core, per ADR-0002) and requires no extra tool/process beyond what already runs in Docker Compose and CI. Alternative considered: an external migration tool (e.g. Flyway) — rejected, it would add a second runtime dependency to install and run when a .NET-native library integrates directly into the existing startup pipeline.

**3. Health check via ASP.NET Core's built-in health checks middleware.**
`Microsoft.Extensions.Diagnostics.HealthChecks`, with a PostgreSQL check (e.g. `AspNetCore.HealthChecks.NpgSql`) registered against the same connection string as the app, exposed at `GET /health` with no authentication. Alternative considered: a hand-rolled endpoint that pings the database directly — rejected as reinventing a well-supported framework feature the health-check spec's requirements already map onto directly.

**4. Structured logging via Serilog, console/JSON sink only.**
Configured at host startup writing structured JSON to console (container-friendly, picked up by `docker logs`/CI output as-is). No external sink (Seq, ELK, etc.) is added now — per ADR-0003 and architecture.md Phase 4, that's introduced only when a real observability need appears.

**5. API documentation via Swashbuckle (OpenAPI/Swagger UI).**
Enabled in Development and in the Docker Compose local environment, so the API is manually explorable without a separate client — useful both for this portfolio project's reviewers and for exercising `/health` and future endpoints by hand.

**6. Global exception handling maps to RFC 7807 problem details.**
A single exception-handling middleware converts unhandled exceptions into a problem-details response, per architecture.md Section 8. Built once here so every future vertical slice gets a consistent error shape for free instead of each feature reimplementing it.

**7. CI via a single GitHub Actions workflow.**
`.github/workflows/ci.yml` runs on push/PR, starts a PostgreSQL service container, then runs `dotnet restore`, `dotnet build`, and `dotnet test` (unit + integration) against it. Alternative considered: Testcontainers-driven integration tests instead of a shared CI service container — deferred; a single shared Postgres is enough while there are no tests with conflicting data, and this can be revisited (with its own ADR) if integration tests start needing per-test isolation.

## Risks / Trade-offs

- DbUp running at API startup could let the API start against a database that isn't ready yet → Mitigation: Docker Compose `depends_on` with a health condition on the PostgreSQL container, and DbUp fails fast (non-zero exit) rather than letting the API come up in a half-migrated state.
- A single shared PostgreSQL service container in CI could let integration tests leak state across runs as more tests are added later → Mitigation: integration tests are responsible for their own setup/teardown of the rows they touch; revisit with Testcontainers-per-test if this becomes flaky once real integration tests exist.
- Pre-creating empty module folders (Customers, Products, Orders, Auth, Dashboard) ahead of their own specs could invite scope creep into this change → Mitigation: folders contain no code beyond what's needed for the solution to compile (e.g. a placeholder file or none at all); actual module behavior stays out of scope until each module's own change.

## Migration Plan

This change creates the system from nothing — there is no existing deployment or data to migrate. Merging it is the initial creation of a runnable OrderFlow instance (`docker compose up` brings up the API and a fresh PostgreSQL with the baseline schema). Rollback, if needed before any feature work builds on it, is reverting the branch/PR.
