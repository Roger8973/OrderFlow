# OrderFlow

Order management API for small and medium businesses: customers, products and the
full order lifecycle in one place.

![CI](https://github.com/Roger8973/OrderFlow/actions/workflows/ci.yml/badge.svg)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![PostgreSQL 17](https://img.shields.io/badge/PostgreSQL-17-336791)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

> Also a portfolio project: built incrementally, feature by feature, with
> Spec-Driven Development (OpenSpec) and AI assistance.

## Why

Small businesses often track sales in spreadsheets and chats, which leads to
duplicated orders, pricing errors and no change history. OrderFlow centralizes
customers, products and orders in a single, simple, reliable application.
See [docs/product-vision.md](docs/product-vision.md).

## Tech stack

- C# / .NET 10, ASP.NET Core
- PostgreSQL 17 with Dapper (see [ADR-0002](docs/decisions/0002-dapper-over-ef-core.md))
- Docker / Docker Compose
- GitHub Actions CI
- Unit and integration tests

## Architecture

Modular monolith with vertical slices and pragmatic Clean Architecture.
Details in [docs/architecture.md](docs/architecture.md) and the
[decision records](docs/decisions/).

```
src/
  OrderFlow.Api             # HTTP endpoints, composition root
  OrderFlow.Application     # use cases
  OrderFlow.Domain          # business rules
  OrderFlow.Infrastructure  # Dapper, Postgres, migrations
tests/
  OrderFlow.UnitTests
  OrderFlow.IntegrationTests
```

## Getting started

```bash
docker compose up --build
```

- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

Without Docker: start a PostgreSQL instance, set `ConnectionStrings__Postgres`, then run:

```bash
dotnet run --project src/OrderFlow.Api
```

## Tests

```bash
dotnet test
```

## Status

- [x] Project foundation and health check
- [x] Customer management: create customer
- [ ] Products
- [ ] Orders
- [ ] Auth
- [ ] Dashboard

## How it's built

Every feature starts as a spec in [`openspec/`](openspec/) (proposal, design,
tasks) before any code, and is delivered through a pull request validated by CI.

## License

[MIT](LICENSE)
