# ADR-0001: Modular Monolith with Vertical Slices (over Microservices)

## Status

Accepted

## Context

OrderFlow needs an architecture for its MVP: Customers, Products, Orders, Auth, and
Dashboard. These modules are small and closely related — Orders depends on Customers
and Products — and are built and operated by a single developer, deployed as one unit.

Microservices, event-driven architectures, and message brokers are common defaults in
modern backend projects, but none of them are backed by a real requirement here: there
is no need to scale a module independently, no need to deploy modules on different
cadences, and no separate team boundaries to reflect in separate services.

## Decision

OrderFlow is built as a **Modular Monolith**, internally organized using **Vertical
Slice Architecture**: each feature (Customers, Products, Orders, Auth, Dashboard) is
implemented end-to-end — API, Application, Domain, Infrastructure — as a self-contained
module, within a single deployable ASP.NET Core Web API.

A pragmatic Clean Architecture layering is applied within each slice (Domain has no
dependency on Infrastructure or frameworks), without a full DDD toolkit (no generic
repositories, specifications, or unit-of-work abstractions).

Full architecture details are in [architecture.md](../architecture.md).

## Consequences

- Single deployment unit: simple to run, test, and reason about; no distributed system
  concerns (network calls between modules, eventual consistency, service discovery).
- Module boundaries are enforced by folder structure and code conventions, not by
  process/network boundaries — this requires discipline to keep modules from reaching
  into each other's internals, but costs nothing operationally.
- Because each module is already isolated as a vertical slice, extracting it into its
  own service later remains possible with low friction, *if* a concrete requirement
  ever justifies it (see [ADR-0003](0003-complexity-on-demand.md)).
- No microservice infrastructure (orchestration, distributed tracing, service mesh) is
  needed for the MVP.
