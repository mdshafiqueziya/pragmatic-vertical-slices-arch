# Tasks

## 1. Solution and project foundation

- [x] 1.1 Create a .NET 10 solution with one ASP.NET Core Minimal API project and one xUnit test project, add their project reference and shared compiler settings, and verify `dotnet build` succeeds.
- [x] 1.2 Add the repository README's solution overview, build/test commands, and required .NET SDK; verify each documented command matches the solution layout.

## 2. Runtime and persistence foundation

- [x] 2.1 Configure PostgreSQL connection options, Npgsql EF Core `DbContext`, dependency injection, and explicit migration tooling; verify the API project builds and `dotnet ef dbcontext info` resolves the configured context.
- [x] 2.2 Add global Problem Details exception handling and structured JSON console logging without exposing exception details; verify API tests cover a handled failure and emitted diagnostics retain structured fields.
- [x] 2.3 Add the initial product schema migration and local PostgreSQL Compose configuration; verify the migration applies to a clean database and appears in `dotnet ef migrations list`.

## 3. Product creation and query slices

- [x] 3.1 Implement feature-local product request/response contracts, validation, focused create/get/list handlers, and Minimal API endpoints; verify handler unit tests cover valid and invalid data plus missing product behavior.
- [x] 3.2 Implement stable paginated product listing with the specified defaults and bounds; verify unit tests cover defaults, limits, and deterministic ordering.
- [x] 3.3 Document create, retrieve, and list routes with request/response examples and validation behavior; verify examples match the implemented endpoint contracts.

## 4. Product update and deletion slices

- [x] 4.1 Implement focused update and delete handlers with feature-local endpoints and explicit expected results; verify unit tests cover successful changes, validation failures, and missing identifiers.
- [x] 4.2 Document update and delete routes and their status codes; verify the documented behavior matches the endpoint results.

## 5. Integration and end-to-end verification

- [x] 5.1 Add PostgreSQL and `WebApplicationFactory` integration coverage for CRUD, pagination, validation Problem Details, and persisted data using a uniquely named disposable schema; verify the tests pass against the configured PostgreSQL service.
- [x] 5.2 Document PostgreSQL prerequisites, database setup/migration commands, connection-string configuration, and API launch steps; verify the documented local workflow starts the API against PostgreSQL.
- [x] 5.3 Run the complete solution test suite and a clean build; verify `dotnet test` and `dotnet build` both pass.
