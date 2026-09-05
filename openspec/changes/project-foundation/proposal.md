## Why

OrderFlow currently exists only as product and architecture documentation (`docs/product-vision.md`, `docs/architecture.md`, ADRs 0001-0003) — there is no solution, no runnable API, and no CI. Before the first business feature (Customer Management, per the roadmap) can be built as a vertical slice, the repository needs a working technical base that matches the documented architecture: a modular-monolith solution structure, a containerized PostgreSQL database with a migration mechanism, structured logging, API documentation, a build/test CI pipeline, and a health check so the running system can be verified end-to-end. Building this foundation first means every later feature slice lands on a proven, already-tested base instead of each one having to separately prove the stack works.

## What Changes

- Scaffold the .NET solution using the layout defined in `docs/architecture.md` Section 6: `OrderFlow.Api`, `OrderFlow.Application`, `OrderFlow.Domain`, `OrderFlow.Infrastructure` projects, each with empty `Customers/`, `Products/`, `Orders/`, `Auth/`, `Dashboard/` module folders (Domain excludes `Auth`) ready for future vertical slices, plus corresponding unit/integration test projects.
- Add Docker Compose for a local PostgreSQL instance, plus a Dockerfile for the API, so the whole system runs with a single command.
- Add a SQL-script-based schema migration mechanism (per ADR-0002 — e.g. DbUp) that runs automatically on API startup in Docker Compose, starting from an empty baseline migration.
- Add structured logging (Serilog) configured from day one, per `docs/architecture.md` Section 8.
- Add Swagger/OpenAPI documentation exposed by the API.
- Add a consistent problem-details error response shape for unhandled API errors, per `docs/architecture.md` Section 8.
- Add a `/health` endpoint reporting the liveness of the API and its PostgreSQL dependency.
- Add a GitHub Actions CI pipeline that restores, builds, and runs the solution's tests on every push/PR.

## Capabilities

### New Capabilities
- `health-check`: the `/health` endpoint's contract — what it returns when the API and its PostgreSQL dependency are healthy, and when the database is unreachable.

### Modified Capabilities
(none — this is the first change in the project; no existing specs to modify)

## Impact

- **Affected code**: entire `src/` tree (new — greenfield), `docker-compose.yml`, `Dockerfile`, `.github/workflows/`, SQL migration scripts.
- **Dependencies**: ASP.NET Core, Dapper, Npgsql, a SQL migration runner (e.g. DbUp), Serilog, Swashbuckle (or equivalent) for OpenAPI.
- **Systems**: introduces the project's first CI pipeline and its first containerized runtime environment; no existing systems are affected since this is the project's initial technical scaffolding.
