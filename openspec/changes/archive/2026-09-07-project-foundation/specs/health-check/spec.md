## Purpose

The health-check capability lets operators and orchestration tooling (Docker Compose, CI) verify that the running API and its PostgreSQL dependency are operating correctly, without needing any business data or authentication.

## ADDED Requirements

### Requirement: API exposes an unauthenticated health endpoint
The system SHALL expose a `GET /health` endpoint that does not require authentication or authorization.

#### Scenario: Health endpoint is publicly reachable
- **WHEN** a client sends `GET /health` without any authentication credentials
- **THEN** the system processes the request and does not return a 401 or 403 status

### Requirement: Health endpoint reports overall system health
The `GET /health` endpoint SHALL report an overall healthy status when the API process is running and its PostgreSQL dependency is reachable.

#### Scenario: All dependencies are reachable
- **WHEN** a client sends `GET /health` and the PostgreSQL database is reachable
- **THEN** the system responds with HTTP 200 and an overall status of "Healthy"

### Requirement: Health endpoint reports PostgreSQL dependency status
The `GET /health` endpoint SHALL report the status of the PostgreSQL dependency, and the overall status SHALL be unhealthy whenever PostgreSQL cannot be reached.

#### Scenario: Database is unreachable
- **WHEN** a client sends `GET /health` and the PostgreSQL database cannot be reached
- **THEN** the system responds with a non-200 HTTP status
- **THEN** the response reports the PostgreSQL dependency as unhealthy
