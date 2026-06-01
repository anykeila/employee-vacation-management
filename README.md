# Vacation Management API 

[![CI](https://github.com/anykeila/employee-vacation-management/actions/workflows/ci.yml/badge.svg)](https://github.com/anykeila/employee-vacation-management/actions/workflows/ci.yml)

REST API built with **.NET 8 (ASP.NET Core)** for managing employees and their
vacation requests, with role-based access control (RBAC), JWT authentication,
a vacation approval workflow and a company-wide overlap rule enforced at the
database level.

## Stack

| Component          | Choice                                |
| ------------------ | ------------------------------------- |
| Language / Runtime | C# / .NET 8 (LTS)                     |
| Framework          | ASP.NET Core (controllers)            |
| ORM                | Entity Framework Core 8 + Npgsql      |
| Database           | PostgreSQL 16                         |
| Validation         | FluentValidation                      |
| Authentication     | JWT (HS256) + BCrypt password hashing |
| API versioning     | Asp.Versioning (URL segment)          |
| Documentation      | Swagger / OpenAPI (Swashbuckle)       |
| Logging            | Serilog (structured JSON)             |
| Error format       | RFC 7807 ProblemDetails               |
| Tests              | xUnit + FluentAssertions + EF InMemory |
| Containerization   | Docker + docker-compose               |

## Architecture

Pragmatic layered architecture, with dependencies always pointing **towards the
domain**:

```
VacationManagement.sln
├── src/
│   ├── VacationManagement.Domain          # Entities, enums, value objects, domain rules (no deps)
│   ├── VacationManagement.Application      # Service interfaces, DTOs, validators, Result type
│   ├── VacationManagement.Infrastructure   # EF Core, persistence, security, service implementations
│   └── VacationManagement.Api              # Controllers, auth, Swagger, error handling, composition root
└── tests/
    └── VacationManagement.UnitTests        # Domain + service unit tests (overlap and RBAC)
```

**Why layered instead of full Clean Architecture / CQRS:** for the scope of this
exercise, a clear layered separation delivers most of the benefits — testability,
separation of concerns, dependency inversion towards the domain — without the
boilerplate of per-feature mediators and handlers. The `Domain` project has zero
external dependencies, so the core business rules (e.g. the inclusive
`DateRange`) are trivially unit-testable.

## Getting started

**Prerequisite: Docker only** — make sure Docker Desktop (or the Docker daemon)
is running. `docker-compose` brings up the API together with a PostgreSQL
instance, so there is no need to install .NET or PostgreSQL locally.

```bash
# 1. Clone the repository
git clone https://github.com/anykeila/employee-vacation-management.git
cd employee-vacation-management

# 2. Build and start the stack (API + PostgreSQL)
docker compose up --build
```

| Resource     | URL                                      |
| ------------ | ---------------------------------------- |
| API base     | `http://localhost:8080/api/v1`           |
| Swagger UI   | `http://localhost:8080/swagger`          |
| OpenAPI doc  | `http://localhost:8080/swagger/v1/swagger.json` |
| Liveness     | `http://localhost:8080/health/live`      |
| Readiness    | `http://localhost:8080/health/ready` (checks the database) |

On startup the API applies EF Core migrations automatically and backfills the
seeded users' password hashes. Configuration (ports, DB credentials) can be
overridden with a `.env` file — see [.env.example](.env.example). Defaults are
baked into `docker-compose.yml`, so the stack also runs without one.

To stop the stack, press `Ctrl+C` in the terminal running it and then:

```bash
docker compose down        # stop and remove the containers
docker compose down -v     # also drop the database volume for a clean start
```

For local development outside the container, the **.NET 8 SDK** is required
(`global.json` pins the 8.0 line). Run the tests with:

```bash
dotnet test
```

## Seeded data and credentials

All seeded users share the same password: **`Password123!`**

| Id | Name             | Email                          | Role          | Manager |
| -- | ---------------- | ------------------------------ | ------------- | ------- |
| 1  | Ana Silva        | `ana.silva@workflow.com`       | Administrator | —       |
| 2  | João Pereira     | `joao.pereira@workflow.com`    | Manager       | —       |
| 5  | Joana Soares     | `joana.soares@workflow.com`    | Manager       | —       |
| 3  | Marta Fernandes  | `marta.fernandes@workflow.com` | Employee      | João (2) |
| 4  | Henrique Martins | `henrique.martins@workflow.com`| Employee      | Joana (5) |

Three historical **approved** vacations (August/September 2025) are also seeded.
They are inserted directly so they bypass the "no past start date" rule that
applies to new requests (see [Design decisions](#design-decisions-and-trade-offs)).

## Authentication

```bash
# 1. Log in to obtain a JWT
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"ana.silva@workflow.com","password":"Password123!"}'

# 2. Call protected endpoints with the token
curl http://localhost:8080/api/v1/employees \
  -H "Authorization: Bearer <token>"
```

In the Swagger UI, click **Authorize** and paste the token (without the
`Bearer ` prefix).

Login returns a short-lived **access token** and a long-lived **refresh token**.
When the access token expires, exchange the refresh token for a fresh pair —
the old refresh token is rotated out (single-use):

```bash
curl -X POST http://localhost:8080/api/v1/auth/refresh \
  -H 'Content-Type: application/json' \
  -d '{"refreshToken":"<refresh-token>"}'
```

## Endpoints and authorization

| Method & path                              | Administrator | Manager           | Employee     |
| ------------------------------------------ | ------------- | ----------------- | ------------ |
| `POST /auth/login`                         | public        | public            | public       |
| `POST /auth/refresh`                       | public        | public            | public       |
| `GET  /employees`                          | ✅            | ✅                | ❌ (403)     |
| `GET  /employees/{id}`                     | ✅            | ✅                | ❌ (403)     |
| `POST /employees`                          | ✅            | ❌ (403)          | ❌ (403)     |
| `PUT  /employees/{id}`                     | ✅            | ❌ (403)          | ❌ (403)     |
| `DELETE /employees/{id}`                   | ✅            | ❌ (403)          | ❌ (403)     |
| `POST /vacation-requests`                  | ✅            | ✅                | ✅ (own)     |
| `GET  /vacation-requests`                  | all           | own + direct reports | own only  |
| `GET  /vacation-requests/{id}`             | any           | own + direct reports | own only  |
| `POST /vacation-requests/{id}/approve`     | any           | direct reports only | ❌ (403)   |
| `POST /vacation-requests/{id}/reject`      | any           | direct reports only | ❌ (403)   |
| `POST /vacation-requests/{id}/cancel`      | any           | own only          | own only     |

Unauthenticated requests to protected endpoints return **401**; authenticated
requests without the required role/ownership return **403**.

### Pagination and filtering

Both list endpoints are paged and return a `PagedResult` envelope
(`items`, `page`, `pageSize`, `totalCount`, `totalPages`). `pageSize` defaults to
20 and is clamped to a maximum of 100, so a caller cannot request an unbounded
payload.

| Endpoint                  | Query parameters                                                        |
| ------------------------- | ----------------------------------------------------------------------- |
| `GET /employees`          | `page`, `pageSize`, `role` (e.g. `Manager`), `search` (name or email)   |
| `GET /vacation-requests`  | `page`, `pageSize`, `status`, `employeeId`, `from`, `to` (date window)  |

```bash
# Managers only, first page of 50
curl "http://localhost:8080/api/v1/employees?role=Manager&pageSize=50" \
  -H "Authorization: Bearer <token>"

# Approved requests that intersect December 2026
curl "http://localhost:8080/api/v1/vacation-requests?status=Approved&from=2026-12-01&to=2026-12-31" \
  -H "Authorization: Bearer <token>"
```

Filtering is applied **after** the role-based scoping, in SQL — a manager paging
requests still only ever sees their own team's rows.

## Business rules

- **Inclusive dates.** A request for `01/08–05/08` spans **5 days**; both
  endpoints count. Modelled by the `DateRange` value object.
- **Vacation lifecycle (state machine).** `Pending → Approved`,
  `Pending → Rejected`, or `Pending → Cancelled`. Approve/reject are manager
  decisions; cancellation is the employee withdrawing their own still-pending
  request (an administrator may cancel on anyone's behalf). A request that has
  already left the pending state cannot be changed (returns 409).
- **Company-wide overlap rule.** No two employees may hold **approved** vacations
  that share a day. The rule is checked when a request is *approved*, not when it
  is created — pending requests may freely overlap. Adjacent ranges (e.g.
  `…–05/12` and `06/12–…`) do **not** overlap.
- **Approval authorization.** Only an Administrator, or the requesting employee's
  **direct** manager, may approve or reject a request.

## Design decisions and trade-offs

This section documents the interpretation of ambiguous or unusual requirements
in the brief, and the engineering decisions behind them.

1. **The overlap rule is global, not per-employee.** The acceptance scenarios
   compare vacations across *different* employees, so the rule is "only one
   person on vacation at a time, company-wide". This is unusual, so it is
   implemented exactly as specified but isolated behind the approval step, making
   it easy to scope per-team/per-department later.

2. **Overlap enforced atomically in the database (concurrency-safe).** A naive
   "query then insert" check has a time-of-check/time-of-use race: two
   concurrent approvals can both pass the check and then both commit. A
   PostgreSQL **GiST exclusion constraint** over an inclusive `daterange`
   (`WHERE status = 'Approved'`) makes overlapping approved rows impossible at the
   storage level. The service still does a friendly pre-check for a clean 409
   message, and catches the constraint violation (`SQLSTATE 23P01`) as the
   authoritative backstop.

3. **Injectable clock (`TimeProvider`).** The sample data is dated in the past
   relative to "today", which collides with the "no past start date" rule. Rather
   than hard-coding `DateTime.Now`, the clock is injected, so the rule is
   deterministically testable (the unit tests pin "now" to a fixed date) and the
   historical seed is inserted directly, bypassing the rule.

4. **.NET 8 instead of .NET 7.** The brief mentions ASP.NET Core 7.0+; .NET 7 is
   already end-of-life, so the project targets **.NET 8 (LTS)**.

5. **Email normalization.** Emails are trimmed and lower-cased before storage and
   comparison, backed by a unique index, so `Ana.Silva@…` and `ana.silva@…` are
   treated as the same identity.

6. **Self-referencing manager relationship.** `Employee.ManagerId` is a nullable
   self-reference (`ON DELETE RESTRICT`), so an employee who still manages others
   cannot be deleted (409). An employee cannot be their own manager, and a manager
   must hold the Manager or Administrator role.

7. **HTTPS vs. "simple local setup".** The brief asks for HTTPS-only but also for
   a frictionless local run. The container serves plain HTTP on `:8080` for
   easy evaluation; in a real deployment, TLS termination belongs at the
   ingress/reverse proxy (or via `UseHttpsRedirection` + certificates), which is
   the standard production pattern.

8. **Swagger is enabled in all environments.** This is a deliberate convenience
   for evaluation (the compose stack runs with `ASPNETCORE_ENVIRONMENT=Development`
   by default). In production I would gate it behind `IsDevelopment()` or
   authentication.

9. **Refresh tokens are opaque, hashed and rotated.** The refresh token is a
   256-bit random value (not a JWT), stored only as a **SHA-256 hash**, so a
   database leak cannot be replayed against `/auth/refresh`. Each use **rotates**
   the token (the old one is revoked, single-use); presenting an already-rotated
   token is treated as theft and **revokes every active token** for that user.
   The access JWT stays short-lived; the refresh token's lifetime is configurable
   (`Jwt:RefreshTokenDays`, default 7). A hosted `BackgroundService` prunes
   **expired** tokens periodically so the table stays bounded; revoked-but-not-yet-
   expired rows are deliberately kept so replay within the window still trips the
   theft response.

## Cross-cutting concerns

- **Consistent errors (RFC 7807).** All failures return `application/problem+json`:
  business outcomes are mapped from a `Result` type to `ProblemDetails`,
  validation failures surface as `ValidationProblemDetails` (field → messages),
  and unhandled exceptions are converted to a generic 500 ProblemDetails by an
  `IExceptionHandler` — the stack trace is logged server-side but never returned
  to the client.
- **Structured logging (Serilog).** Requests are logged as compact JSON with
  method, path, status code, elapsed time and a trace id that matches the
  `traceId` returned in error responses, enabling request↔log correlation.
- **API versioning.** URL-segment versioning (`/api/v1/...`); Swagger generates
  one document per discovered version automatically.
- **Security.** Passwords are hashed with BCrypt; JWTs are validated for issuer,
  audience, lifetime and signature. The signing key length is checked at startup
  (HS256 needs ≥256 bits) so a misconfiguration fails fast at boot. Login always
  runs a hash verification — even for unknown emails — so response time never
  reveals whether an account exists. The auth endpoints are **rate-limited** per
  IP (`RateLimiting:AuthPermitLimit`/`AuthWindowSeconds`) to blunt brute-force.
  The signing key in `appsettings.json` is a development placeholder and must be
  overridden in production.

## Testing

- **Domain unit tests** cover the inclusive `DateRange`: overlap, adjacency,
  total-day count and the invalid-range guard.
- **Service unit tests** cover `VacationRequestService`: the
  Pending→Approved/Rejected/Cancelled state machine, manager-ownership
  authorization, the global no-overlap rule on approval, and read scoping. They
  use the EF Core in-memory provider and the injected fixed clock.
- **Validation & pagination unit tests** cover the FluentValidation rules
  (required fields, email format, password length, date ordering, notes length)
  and the `PaginationQuery` clamping/`Skip`/`TotalPages` arithmetic.
- **Scope note (unit vs. integration).** The in-memory provider validates the
  LINQ overlap pre-check and all RBAC/state logic, but it cannot reproduce the
  PostgreSQL GiST exclusion constraint. That database-level guarantee is verified
  end-to-end against the real PostgreSQL container.
- **Continuous integration.** [`.github/workflows/ci.yml`](.github/workflows/ci.yml)
  builds in Release and runs both suites on every push or pull request to `main`.
  GitHub's `ubuntu-latest` runner provides the Docker daemon the integration tests
  need, so the GiST constraint is exercised in CI exactly as it is locally.

## Performance and scalability

- **Overlap checks are indexed.** A composite index on
  `(Status, StartDate, EndDate)` supports the approval-time overlap query, and the
  GiST index backing the exclusion constraint makes range-overlap lookups
  efficient as the dataset grows.
- **No N+1 queries.** Read paths use explicit projections / `Include`, so a list
  of requests resolves employee names in a single joined query rather than one
  query per row.
- **Read scoping at the database.** Managers and employees filter rows in SQL
  (`WHERE`), not in memory, so the payload and work scale with what the caller is
  allowed to see.
- **Paged list endpoints.** Both collections are paged (`page`/`pageSize`, capped
  at 100) with `Skip`/`Take` translated to SQL `LIMIT`/`OFFSET`, and the filters
  run in the same query, so a large dataset never materialises in memory.
- **Connection pooling** is handled by Npgsql out of the box; the API is stateless
  (JWT-based), so it scales horizontally behind a load balancer.
- **Next steps for larger volumes:** response caching for the largely-static
  employee directory, and — if the global overlap rule were relaxed to per-team —
  partition the exclusion constraint by team to reduce contention.

## What I would add next

Per-device refresh-token management (listing and revoking active sessions), and
a notification hook so employees are emailed when a manager decides on their
request.
