# pragmatic-vertical-slices-arch
A practical, feature-first architecture demo—building complete vertical slices without unnecessary layers or abstractions.

## Solution overview

The .NET solution contains an ASP.NET Core Minimal API in `src/PragmaticVerticalSlices.Api` and an xUnit test project in `tests/PragmaticVerticalSlices.Tests`. Product use cases are organized together under `Features/Products`; shared persistence and runtime concerns live outside the feature slice.

## Requirements

- .NET 10 SDK (10.0.100 or later)

## Build and test

Run from the repository root after setting up PostgreSQL as described below:

```bash
dotnet build PragmaticVerticalSlices.slnx
dotnet test PragmaticVerticalSlices.slnx
```

The integration test creates a unique schema in the configured PostgreSQL database and drops that schema after the test. The configured database user must be allowed to create and drop schemas.

## Local PostgreSQL workflow

The API requires PostgreSQL. To start the repository's local development database with Docker Compose:

```bash
docker compose up -d postgres
```

The API stores its tables in the PostgreSQL `products_schema` schema and uses snake_case for database identifiers.

Set the connection string in the environment. These credentials are for local development only; use a secret store or environment variables for real credentials, not a committed settings file.

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=products;Username=products;Password=products-local-only'
```

For an already-running local PostgreSQL service, set the same environment variable to that database's connection string instead. The integration tests use this connection and create their own temporary schema.

Create the `products_schema` schema and required tables manually before running the API. The application does not apply database migrations automatically.

Run the API:

```bash
dotnet run --project src/PragmaticVerticalSlices.Api/PragmaticVerticalSlices.Api.csproj
```

The API listens on the configured ASP.NET Core URL and reads `ConnectionStrings__DefaultConnection` from the environment.

When running in the Development environment, Swagger UI is available at `/swagger/` and the generated OpenAPI document is at `/openapi/v1.json`.

## Product API

The API exposes the following product routes:

### Create products

`POST /api/products` accepts a non-empty JSON array. All products must pass validation; otherwise, the request returns `400 Bad Request` with indexed Problem Details validation errors and creates none. A successful request returns `201 Created`, an array of the created products, and a `Location` header for the products collection.

```http
POST /api/products
Content-Type: application/json

[
  {
    "name": "Widget",
    "description": "A useful widget",
    "price": 12.50
  },
  {
    "name": "Gadget",
    "description": "A useful gadget",
    "price": 24.00
  }
]
```

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "name": "Widget",
    "description": "A useful widget",
    "price": 12.50
  },
  {
    "id": "00000000-0000-0000-0000-000000000002",
    "name": "Gadget",
    "description": "A useful gadget",
    "price": 24.00
  }
]
```

### Retrieve a product

`GET /api/products/{id}` returns `200 OK` with the product or `404 Not Found` with Problem Details when the identifier is unknown.

```http
GET /api/products/00000000-0000-0000-0000-000000000001
```

### List products

`GET /api/products?page=1&pageSize=20` returns a page in name-then-ID order, including its metadata and total count. `page` defaults to 1; `pageSize` defaults to 20 and must be between 1 and 100.

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "name": "Widget",
      "description": "A useful widget",
      "price": 12.50
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

Product names are required and limited to 200 characters, descriptions may contain up to 1,000 characters, and prices must be greater than zero. Invalid request fields and pagination values return `400 Bad Request` with Problem Details validation errors.

### Update a product

`PUT /api/products/{id}` replaces the product's editable fields and returns `200 OK` with the updated representation. Invalid fields return `400 Bad Request` with validation errors; an unknown identifier returns `404 Not Found` with Problem Details.

```http
PUT /api/products/00000000-0000-0000-0000-000000000001
Content-Type: application/json

{
  "name": "Updated widget",
  "description": "Updated description",
  "price": 15.00
}
```

### Delete a product

`DELETE /api/products/{id}` returns `204 No Content` when the product is deleted. An unknown identifier returns `404 Not Found` with Problem Details.

```http
DELETE /api/products/00000000-0000-0000-0000-000000000001
```

## OpenSpec

OpenSpec is initialized in this repository with the `spec-driven` workflow and GitHub Copilot support. In VS Code Copilot Chat, use `/opsx-propose` to create a change proposal, `/opsx-apply` to implement a reviewed change, and `/opsx-explore`, `/opsx-update`, `/opsx-sync`, or `/opsx-archive` for the corresponding workflows.

If the OpenSpec commands are not available after setup, restart VS Code to refresh them.
