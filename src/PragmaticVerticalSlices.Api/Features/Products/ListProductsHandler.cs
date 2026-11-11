using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed class ListProductsHandler(AppDbContext dbContext)
{
    public async Task<ProductPage> HandleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();
        var totalCount = await products.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        var items = offset >= totalCount
            ? []
            : await products
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id)
                .Skip((int)offset)
                .Take(pageSize)
                .Select(product => ProductResponse.From(product))
                .ToListAsync(cancellationToken);

        return new ProductPage(items, page, pageSize, totalCount);
    }
}
