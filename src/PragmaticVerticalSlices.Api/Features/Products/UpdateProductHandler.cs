using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed class UpdateProductHandler(AppDbContext dbContext)
{
    public async Task<Product?> HandleAsync(
        Guid id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Update(request.Name, request.Description, request.Price);
        await dbContext.SaveChangesAsync(cancellationToken);

        return product;
    }
}
