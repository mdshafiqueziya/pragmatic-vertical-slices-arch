using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed class CreateProductsHandler(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<Product>> HandleAsync(
        IReadOnlyList<ProductRequest> requests,
        CancellationToken cancellationToken)
    {
        var products = requests
            .Select(request => new Product(request.Name, request.Description, request.Price))
            .ToArray();

        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync(cancellationToken);

        return products;
    }
}
