# Spec Delta

## Purpose

Provides a database-backed product catalog that demonstrates the repository's feature-first API architecture through a complete and testable HTTP lifecycle.

## ADDED Requirements

### Requirement: Create products
The API SHALL create a product when its name is non-empty and no longer than 200 characters, its optional description is no longer than 1,000 characters, and its price is greater than zero.

#### Scenario: Product is created with valid input
- **WHEN** a client submits a valid product to `POST /api/products`
- **THEN** the API returns `201 Created` with the created product and a location identifying the new product

#### Scenario: Product input fails validation
- **WHEN** a client submits a product with a missing or invalid name, an overlong description, or a non-positive price
- **THEN** the API returns `400 Bad Request` with a Problem Details response identifying validation errors

### Requirement: Retrieve products
The API SHALL return a product by its identifier or report that the product does not exist.

#### Scenario: Requested product exists
- **WHEN** a client requests `GET /api/products/{id}` for an existing product
- **THEN** the API returns `200 OK` with that product

#### Scenario: Requested product does not exist
- **WHEN** a client requests a product identifier that is not present
- **THEN** the API returns `404 Not Found` with a Problem Details response

### Requirement: List products
The API SHALL return products in pages, defaulting to page 1 and page size 20, and SHALL reject page sizes greater than 100 or page numbers less than 1.

#### Scenario: Client requests a valid page
- **WHEN** a client requests `GET /api/products` with valid `page` and `pageSize` values or their defaults
- **THEN** the API returns `200 OK` with the page's products, page metadata, and total matching product count in deterministic order

#### Scenario: Client requests invalid pagination
- **WHEN** a client requests a page number less than 1 or a page size greater than 100
- **THEN** the API returns `400 Bad Request` with a Problem Details response

### Requirement: Update products
The API SHALL replace a product's editable fields only when the product exists and the submitted fields satisfy the same validation rules as product creation.

#### Scenario: Existing product is updated
- **WHEN** a client submits valid fields to `PUT /api/products/{id}` for an existing product
- **THEN** the API returns `200 OK` with the updated product

#### Scenario: Update is invalid or product is missing
- **WHEN** a client submits invalid fields or identifies a product that does not exist
- **THEN** the API returns `400 Bad Request` with validation details or `404 Not Found` with a Problem Details response, respectively

### Requirement: Delete products
The API SHALL delete an existing product and report missing products without creating a success-shaped response.

#### Scenario: Existing product is deleted
- **WHEN** a client requests `DELETE /api/products/{id}` for an existing product
- **THEN** the API returns `204 No Content` and subsequent retrieval returns `404 Not Found`

#### Scenario: Missing product is deleted
- **WHEN** a client requests deletion of a product that does not exist
- **THEN** the API returns `404 Not Found` with a Problem Details response

### Requirement: Persist catalog data
The API SHALL persist product changes so they remain available after the application process restarts.

#### Scenario: Product remains available after restart
- **WHEN** the API is restarted after a product has been created successfully
- **THEN** a subsequent request can retrieve that product with its saved values
