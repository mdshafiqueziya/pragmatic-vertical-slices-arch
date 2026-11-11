using Microsoft.AspNetCore.Http.HttpResults;

namespace PragmaticVerticalSlices.Api.Features.Products;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProducts(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").WithTags("Products");

        group.MapPost("/", async Task<Results<Created<ProductResponse[]>, ValidationProblem>> (
            IReadOnlyList<ProductRequest> requests,
            CreateProductsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var errors = ProductRequestValidation.Validate(requests);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var products = await handler.HandleAsync(
                requests,
                cancellationToken);
            return TypedResults.Created(
                "/api/products",
                products.Select(ProductResponse.From).ToArray());
        })
        .WithName("CreateProducts")
        .WithSummary("Create products")
        .WithDescription("Creates a non-empty list of products when every product satisfies the field validation rules.");

        group.MapGet("/{id:guid}", async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> (
            Guid id,
            GetProductHandler handler,
            CancellationToken cancellationToken) =>
        {
            var product = await handler.HandleAsync(id, cancellationToken);
            return product is null
                ? TypedResults.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Product not found",
                    detail: $"Product '{id}' does not exist.")
                : TypedResults.Ok(ProductResponse.From(product));
        })
        .WithName("GetProduct")
        .WithSummary("Get a product by ID")
        .WithDescription("Returns a product by its ID, or Problem Details when it does not exist.");

        group.MapGet("/", async Task<Results<Ok<ProductPage>, ValidationProblem>> (
            ListProductsHandler handler,
            CancellationToken cancellationToken,
            int page = ProductPagination.DefaultPage,
            int pageSize = ProductPagination.DefaultPageSize) =>
        {
            var errors = ProductPagination.Validate(page, pageSize);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var result = await handler.HandleAsync(page, pageSize, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("ListProducts")
        .WithSummary("List products")
        .WithDescription("Returns a stable, paginated product list. Defaults to page 1 and page size 20.");

        group.MapPut("/{id:guid}", async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> (
            Guid id,
            ProductRequest request,
            UpdateProductHandler handler,
            CancellationToken cancellationToken) =>
        {
            var product = await handler.HandleAsync(id, request, cancellationToken);
            return product is null
                ? TypedResults.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Product not found",
                    detail: $"Product '{id}' does not exist.")
                : TypedResults.Ok(ProductResponse.From(product));
        })
        .WithName("UpdateProduct")
        .WithSummary("Update a product")
        .WithDescription("Replaces a product's editable fields after validating the submitted values.");

        group.MapDelete("/{id:guid}", async Task<Results<NoContent, ProblemHttpResult>> (
            Guid id,
            DeleteProductHandler handler,
            CancellationToken cancellationToken) =>
        {
            var deleted = await handler.HandleAsync(id, cancellationToken);
            return deleted
                ? TypedResults.NoContent()
                : TypedResults.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Product not found",
                    detail: $"Product '{id}' does not exist.");
        })
        .WithName("DeleteProduct")
        .WithSummary("Delete a product")
        .WithDescription("Deletes a product by its ID, or returns Problem Details when it does not exist.");

        return group;
    }
}
