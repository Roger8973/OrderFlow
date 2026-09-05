## 1. Solution & Project Scaffolding

- [x] 1.1 Create the `.sln` and four projects (`OrderFlow.Api` as ASP.NET Core Web API, `OrderFlow.Application`, `OrderFlow.Domain`, `OrderFlow.Infrastructure` as class libraries) with project references matching architecture.md Section 5 (Api → Application/Domain/Infrastructure; Application/Infrastructure → Domain; Domain has no project references) and verify `dotnet build` succeeds.
- [x] 1.2 Add empty module folders per architecture.md Section 6 (`Customers/`, `Products/`, `Orders/`, `Auth/`, `Dashboard/` under Api; the same set minus `Auth` under Domain; `Commands/`/`Queries/` subfolders under each Application module) and verify the folder tree matches the diagram.
- [x] 1.3 Add `OrderFlow.UnitTests` and `OrderFlow.IntegrationTests` projects referencing the relevant solution projects and verify both are picked up by `dotnet test` (even with zero tests).
- [x] 1.4 Add root `.gitignore`, `.editorconfig`, and solution-level `Directory.Build.props` (nullable/warnings-as-errors as desired) and verify a clean build has no warnings from the new baseline.

## 2. Containerized PostgreSQL & API

- [x] 2.1 Add a `Dockerfile` for `OrderFlow.Api` (multi-stage build/publish) and verify `docker build` produces a runnable image.
- [x] 2.2 Add `docker-compose.yml` with an `api` service and a `postgres` service (with a named volume and a healthcheck) and verify `docker compose up` starts both and the API container reports as running.
- [x] 2.3 Wire the API's PostgreSQL connection string from configuration/environment variables (appsettings + Docker Compose environment) and verify the API container can open a connection to the `postgres` service.

## 3. Database Migrations

- [x] 3.1 Add the DbUp package to `OrderFlow.Infrastructure` and an embedded baseline SQL migration (schema/version tracking table only) and verify the assembly builds with the script embedded as a resource.
- [x] 3.2 Run DbUp automatically against the configured connection string on API startup, failing fast (non-zero exit) if migrations fail, and verify starting the API via Docker Compose creates the DbUp version-tracking table in PostgreSQL.

## 4. Logging & Error Handling

- [x] 4.1 Configure Serilog at host startup to write structured JSON to console and verify `docker compose up` shows structured log output for at least one startup log line.
- [x] 4.2 Add global exception-handling middleware that converts unhandled exceptions into an RFC 7807 problem-details response and verify an endpoint that throws returns a problem-details JSON body with a non-2xx status.

## 5. API Documentation

- [x] 5.1 Add Swashbuckle and expose Swagger UI/OpenAPI JSON in Development and in the Docker Compose environment, and verify `/swagger` renders and lists the `/health` endpoint.

## 6. Health Check (per `specs/health-check/spec.md`)

- [x] 6.1 Register ASP.NET Core health checks with a PostgreSQL check (e.g. `AspNetCore.HealthChecks.NpgSql`) against the app's connection string, expose `GET /health` with anonymous access, and verify a request without credentials does not return 401/403.
- [x] 6.2 Verify `GET /health` returns HTTP 200 with overall status "Healthy" when PostgreSQL is reachable (manual or integration-test check against the Docker Compose stack).
- [x] 6.3 Verify `GET /health` returns a non-200 status with the PostgreSQL dependency reported unhealthy when PostgreSQL is stopped/unreachable (e.g. `docker compose stop postgres` then request `/health`).

## 7. Automated Tests

- [x] 7.1 Add an integration test (using `WebApplicationFactory` or equivalent) covering the `/health` healthy scenario from `specs/health-check/spec.md` and verify it passes against a real PostgreSQL instance.
- [x] 7.2 Add an integration test covering the `/health` unhealthy-database scenario from `specs/health-check/spec.md` and verify it passes.
- [x] 7.3 Add a unit test for the problem-details exception-handling middleware and verify it passes.

## 8. CI Pipeline

- [ ] 8.1 Add `.github/workflows/ci.yml` with a PostgreSQL service container, running `dotnet restore`, `dotnet build`, and `dotnet test` on push/PR to `main`/`develop`, and verify the workflow runs green on a pushed branch.
- [ ] 8.2 Confirm the CI run executes the health-check integration tests from Section 7 against the workflow's PostgreSQL service container and that they pass in that environment.
