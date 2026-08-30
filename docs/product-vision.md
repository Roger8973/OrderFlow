# OrderFlow — Product Vision

## Overview

OrderFlow is an order management platform for small and medium businesses that sell
products. It lets a business register customers and products, and manage the full
lifecycle of an order, from creation to completion.

Beyond its business purpose, OrderFlow is also a portfolio project. It exists to
demonstrate professional, senior-level backend engineering with C#/.NET — clean and
pragmatic architecture, automated testing, containerization, CI/CD, and Spec-Driven
Development (SDD) with AI — applied to a real, coherent business problem, built
incrementally, feature by feature.

## Problem

Many small businesses that sell products still manage their sales operation with
spreadsheets, chat messages, and disconnected tools. This leads to recurring problems:

- duplicated orders;
- difficulty tracking an order's current status;
- lack of control over product data and pricing;
- pricing errors caused by outdated or inconsistent information;
- no clear way to spot delayed orders;
- no history of what changed on an order, and when.

## Vision Statement

OrderFlow centralizes customers, products, and orders in a single, simple, reliable
application — so a salesperson can register a customer, check product availability,
create an order, and track it through to completion without leaving one system.

## Target User

### Persona: João, Salesperson

João works for a company that sells products directly to customers. When he receives
an order request, he needs to:

1. find the customer (or register a new one);
2. check which products are available;
3. inform quantities;
4. create the order;
5. follow its processing;
6. know when the order has been completed.

OrderFlow should make this workflow simple, fast, and fully traceable — at any point,
João (or a manager) should be able to see exactly what was ordered, by whom, and what
state it is in.

## Goals

**Business goals**

- Replace spreadsheets and disconnected tools with a single source of truth for
  customers, products, and orders.
- Make order status always visible and trustworthy.
- Prevent pricing and duplication errors through consistent, validated data.
- Preserve a history of how orders evolve over time.

**Technical / portfolio goals**

Use OrderFlow to demonstrate:

- C# / .NET and ASP.NET Core Web API;
- Dapper with PostgreSQL;
- pragmatic Clean Architecture combined with Vertical Slice Architecture;
- CQRS, validation, and well-modeled business rules;
- authentication and role-based authorization;
- automated testing (unit and integration);
- Docker and Docker Compose;
- CI/CD and basic observability;
- Spec-Driven Development (SDD) as a structured way to build software with AI.

## Non-Goals (Out of Scope for the MVP)

To keep the project focused and avoid artificial complexity, the following are
explicitly out of scope for the MVP and will only be considered later, if a real need
emerges:

- payment processing and financial integrations;
- inventory / stock management;
- notifications (email, SMS, push);
- multi-tenancy;
- microservices, message brokers (Kafka, RabbitMQ), or event sourcing;
- a mobile application.

The project starts as a modular monolith built with vertical slices. Additional
complexity is only introduced when a genuine requirement justifies it — not to make
the project look more sophisticated than the problem requires.

## MVP Scope

```
OrderFlow
│
├── Customers        — register and query customers
├── Products          — register and manage products
├── Orders            — create and track orders through their lifecycle
├── Authentication     — secure access to the system
└── Dashboard          — visualize order and revenue indicators
```

- **Customers** — create, retrieve, list (with filters and pagination), and
  deactivate customers. A customer with existing orders is never physically deleted.
- **Products** — create, retrieve, list, update, and deactivate products. An inactive
  product cannot be added to new orders.
- **Orders** — the core feature. Create an order for a customer with one or more
  product lines, track its status, confirm it, cancel it, and view its history.
- **Authentication** — register and authenticate users, initially via JWT; later
  evolving into role-based authorization (Admin, Salesperson, Manager).
- **Dashboard** — summarized indicators for orders, revenue, and cancellations, plus
  a breakdown of orders by status, to support operational visibility.

## Order Lifecycle

An order moves through a simple, well-defined set of states:

```
Created → Confirmed → Processing → Completed
```

An order can also be cancelled, but only while it hasn't been completed:

```
Created    → Cancelled
Confirmed  → Cancelled
Processing → Cancelled
```

A completed order can never be cancelled. This lifecycle is intentionally simple, but
rich enough to produce meaningful, testable business rules.

## Guiding Principles

- **Pragmatic architecture over ceremony.** Clean Architecture and Vertical Slices,
  not a full DDD toolkit with abstractions the project doesn't need.
- **Vertical, incremental delivery.** Each feature is built end-to-end (domain, use
  cases, API, tests) before moving to the next one.
- **Spec-Driven Development.** Each feature starts as a written spec (user story,
  acceptance criteria, business rules, API contract) before implementation — making
  the AI-assisted development process explicit and reviewable.
- **One language, consistently.** Code, documentation, specs, commits, branches, and
  pull requests are written in English, to match professional/international
  standards and avoid mixing terms.
- **Small, coherent history.** Work is delivered through small feature branches
  merged into `develop` via pull requests, each representing one real, understandable
  increment — not a single large, undifferentiated commit.
- **Complexity only when earned.** New patterns or infrastructure (event sourcing,
  message brokers, microservices, etc.) are added only when a concrete need appears,
  not preemptively.

## High-Level Roadmap

1. **Foundation** — solution structure, architecture, Docker, PostgreSQL, CI,
   health checks, Swagger.
2. **Customer Management** — create, retrieve, list, deactivate customers.
3. **Product Management** — create, retrieve, list, update, deactivate products.
4. **Order Management** — create, retrieve, list, confirm, cancel orders; lifecycle
   business rules; integration tests.
5. **Authentication** — registration, login, JWT, role-based authorization.
6. **Dashboard** — order summary, revenue, and status statistics.
7. **Frontend** — a thin client (login, customers, products, orders, dashboard)
   consuming the API.

Future evolutions beyond the MVP may include payments, inventory, and notifications,
once there's a real reason to add them.

## Success Criteria

The MVP is successful when OrderFlow provides:

- a working, documented REST API covering Customers, Products, Orders, Authentication,
  and Dashboard;
- meaningful business rules enforced at the domain level (e.g., the order lifecycle);
- automated unit and integration test coverage for each feature;
- a containerized setup that runs locally with Docker Compose;
- a CI pipeline that builds and tests the solution automatically;
- a clear, traceable Git and spec history that shows how each feature moved from
  specification to implementation — demonstrating Spec-Driven Development in practice.
