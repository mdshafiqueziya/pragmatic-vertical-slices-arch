using System.ComponentModel.DataAnnotations;

namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed record ProductRequest(
    [property: Required]
    [property: StringLength(200, MinimumLength = 1)]
    string Name,
    [property: StringLength(1_000)]
    string? Description,
    [property: Range(typeof(decimal), "0.01", "9999999999999999.99")]
    decimal Price);

public sealed record ProductResponse(Guid Id, string Name, string? Description, decimal Price)
{
    public static ProductResponse From(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price);
}

public sealed record ProductPage(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
