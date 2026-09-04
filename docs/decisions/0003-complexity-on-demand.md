# ADR-0003: Complexity Introduced on Demand

## Status

Accepted

## Context

Many backend portfolio projects include infrastructure — message brokers, caching
layers, load balancers, microservices — added preemptively to showcase familiarity with
the technology, rather than because a requirement calls for it. This tends to produce
architectures that are harder to justify and to reason about, and that don't reflect
how architecture actually evolves in real systems.

OrderFlow's MVP has no requirement today for asynchronous processing, horizontal
scaling, distributed caching, or advanced observability (see
[architecture.md](../architecture.md), Section 9).

## Decision

New infrastructure or architectural patterns are added to OrderFlow **only when a
concrete business or non-functional requirement justifies them** — never in
anticipation of hypothetical future needs. Each such addition must be preceded by its
own spec (or a new/updated ADR) explaining the requirement that justifies it.

Concrete triggers already identified for future phases:

| Requirement that appears | Architectural response |
|---|---|
| Order confirmation must trigger a notification and/or inventory update | Introduce a message broker and worker processes |
| The API must sustain a specific high request volume | Introduce a load balancer and multiple API instances |
| Multiple API instances need shared, low-latency state | Introduce a distributed cache (e.g., Redis) |
| Production issues require deeper insight into system behavior | Add metrics and distributed tracing on top of existing logging |
| A module needs to scale or deploy independently of the rest | Reconsider extracting that module into its own service |

## Consequences

- The MVP architecture stays as simple as the current requirements allow, per
  [ADR-0001](0001-modular-monolith-vertical-slices.md).
- Every piece of infrastructure in the system is traceable to a real requirement and a
  spec/ADR, not to a default technology stack — this is the intended portfolio
  narrative: architecture evolving in response to need.
- This requires discipline to resist adding "impressive" technology without a
  justification, even when it is common practice elsewhere.
- This ADR itself may be superseded if project goals change (e.g., if demonstrating a
  specific technology becomes an explicit goal independent of a functional need) —
  that would be a deliberate, documented exception, not a silent departure from this
  principle.
