# Design

## Context

The repository is a greenfield .NET 10 architecture demo: it has a short README and standard .NET ignore rules, but no solution, source projects, or capability specs. The product behavior contract is defined in `specs/product-catalog/spec.md`.

## Goals / Non-Goals

**Goals:**
- Keep each product use case and its HTTP contract, handler, and validation close together.
- Provide a runnable API and repeatable tests against PostgreSQL without adding unnecessary project or framework layers.
- Make database schema changes explicit and configuration safe for local and deployed environments.

**Non-Goals:**
- Add authentication, authorization, a frontend, or a message broker.
- Create separate Domain, Application, and Infrastructure projects or introduce a mediator/repository framework.
- Build a generalized CRUD framework or a production deployment platform.

## Decisions

### One API project organized by feature

Create one .NET 10 ASP.NET Core API project and one xUnit test project in a solution. Place product endpoints, request/response contracts, validators, and one focused handler per operation under a `Features/Products` slice. Keep only genuinely cross-cutting concerns, such as the EF Core `DbContext` and exception handling, in a small shared infrastructure area.

This keeps a use case cohesive and avoids the traditional layer-per-project split. Separate projects for domain, application, and data access were considered but would add dependency wiring without a second bounded context to justify it.

### Direct handler classes with explicit DI

Have Minimal API route handlers delegate to focused feature handlers registered with built-in dependency injection. Use explicit typed results for success and expected failures. Do not add a mediator package or generic repository: direct calls make control flow visible and let EF Core remain the persistence abstraction.

### PostgreSQL with EF Core and explicit migrations

Use Npgsql's EF Core provider and a single application `DbContext`, with schema migrations checked into the API project. Read the PostgreSQL connection string from configuration/environment (for example, `ConnectionStrings__Postgres`); keep production secrets out of source control. Include a local PostgreSQL Compose service for development.

Do not silently migrate the database during API startup. Document the EF migration commands so schema changes are reviewable and production rollout can apply migrations as an explicit deployment step. A separate migration/deployment service is deferred until the repository needs one.

### Framework-native validation and Problem Details

Use .NET 10 Minimal API request validation for feature request contracts, returning field-level validation Problem Details for invalid input. Register ASP.NET Core Problem Details and an `IExceptionHandler` for unexpected exceptions so failures use a consistent response while logs retain diagnostic context. Do not expose exception details to clients.

### Structured logging without an extra logging framework

Use the built-in `Microsoft.Extensions.Logging` abstractions and structured message templates, with JSON console output for deployed environments. This meets the structured logging requirement without imposing a third-party logging stack on a small demo.

### Unit and PostgreSQL integration tests

Keep unit tests and API integration tests in the dedicated xUnit project. Unit-test handler behavior with focused substitutes where useful; integration-test HTTP behavior, validation, and persistence against PostgreSQL using `WebApplicationFactory`. This exercises the actual database provider rather than substituting an in-memory provider. Integration tests connect to a PostgreSQL service configured by `ConnectionStrings__DefaultConnection`; each test creates a uniquely named schema and drops only that schema when finished. This avoids Docker/Testcontainers while keeping test data isolated from other schemas in the configured database.

## Risks / Trade-offs

- [Integration tests require a reachable PostgreSQL service and permission to create/drop schemas] → Document the connection-string requirement and isolate test data in a unique schema that is removed after each test.
- [Schema migrations can be forgotten when the model changes] → Include migration generation and application commands in the README and test the API against the migrated schema.
- [A single API project can accumulate shared concerns] → Keep feature code cohesive and limit shared folders to cross-cutting runtime and persistence concerns; split projects only when a real boundary emerges.
- [Development database credentials may be mistaken for production credentials] → Restrict Compose credentials to local use and require deployment connection strings through secret configuration.

## Migration Plan

No existing application data or API is present. Add the initial schema migration, start PostgreSQL through the local Compose configuration, apply the migration explicitly, then run the API and test suite. For later releases, back up production data and apply reviewed EF Core migrations before deploying code that requires the new schema; rollback uses a corrective migration or a restored backup rather than automatic destructive rollback.
