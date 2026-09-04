# ADR-0002: Dapper over EF Core

## Status

Accepted

## Context

OrderFlow needs a data access strategy for PostgreSQL. Both EF Core and Dapper are
mature, well-supported choices in the .NET ecosystem, and either would be defensible.

EF Core is the more conventional choice for a modern .NET project, offering change
tracking, LINQ, relationship configuration, and built-in migrations. Dapper is a
lightweight micro-ORM that requires explicit SQL and offers full control over exactly
what is executed against the database, at the cost of losing EF Core's automatic
capabilities (migrations, change tracking, LINQ).

For a portfolio project, the goal is to demonstrate a deliberate, justified choice
rather than defaulting to the most popular option.

## Decision

OrderFlow uses **Dapper** for all data access, paired with explicit, hand-written SQL
against PostgreSQL, organized as **CQRS-lite**: Commands perform writes and Queries
perform reads, each with its own explicit SQL — applied consistently across every
vertical slice (see [ADR-0001](0001-modular-monolith-vertical-slices.md)).

Schema migrations are handled through versioned SQL scripts (or a lightweight runner
such as DbUp), since Dapper does not provide this out of the box.

## Consequences

- Full control and visibility over the exact SQL executed, including query performance
  and shape — valuable for demonstrating SQL/PostgreSQL proficiency.
- No automatic change tracking or LINQ: repositories and queries are more explicit and
  slightly more verbose than the EF Core equivalent.
- Migrations require an explicit tooling choice and process, instead of EF Core's
  built-in migration generation.
- Read models (queries) are free to be shaped independently of the write model
  (commands), without EF Core's tracked-entity constraints getting in the way.
- If a future module benefits from EF Core's relationship/change-tracking capabilities
  more than from Dapper's explicitness, that would be a new, separately justified
  decision — not a reason to introduce EF Core project-wide today.
