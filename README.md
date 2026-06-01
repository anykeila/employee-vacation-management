# Vacation Management API — WorkFlow S.A.

REST API built with **.NET 8 (ASP.NET Core)** for managing employees and
vacation requests, with role-based access control (RBAC), JWT authentication
and overlap validation.

> **Status:** work in progress.  

## Stack

| Component         | Choice                                    |
| ----------------- | ----------------------------------------- |
| Language / Runtime | C# / .NET 8 (LTS)                        |
| Framework         | ASP.NET Core                              |
| ORM               | Entity Framework Core                     |
| Database          | PostgreSQL                                |
| Validation        | FluentValidation                          |
| Authentication    | JWT                                       |
| Documentation     | Swagger / OpenAPI                         |
| Logging           | Serilog (structured logging)              |
| Tests             | xUnit + FluentAssertions                  |
| Containerization  | Docker + docker-compose                   |

## Project structure

Layered architecture, with dependencies always pointing towards the domain:

```
VacationManagement.sln
├── src/
│   ├── VacationManagement.Domain          # Entities, enums and domain rules (no dependencies)
│   ├── VacationManagement.Application      # Use cases, services, validation, contracts
│   ├── VacationManagement.Infrastructure   # EF Core, repositories, persistence, integrations
│   └── VacationManagement.Api              # Controllers, authentication, Swagger, composition root
└── tests/
    └── VacationManagement.UnitTests        # Unit tests (vacation validation and RBAC)
```

**Why layered instead of full multi-project Clean Architecture or CQRS:** for
the scope of this exercise, a clear layered separation provides most of the
benefits (testability, separation of concerns, dependency inversion towards the
domain) without the boilerplate of per-feature mediators and handlers. WIP...

## How to run (summary — WIP)

Prerequisites: **Docker** and **docker-compose**. `docker-compose` brings up the
API together with a PostgreSQL instance, with no need to install .NET locally.

```bash
docker compose up --build
```

For local development outside the container, the **.NET 8 SDK** is required.
The `global.json` file pins the SDK to the 8.0 line. 
