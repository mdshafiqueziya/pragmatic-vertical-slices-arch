using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed class DeleteProductHandler(AppDbContext dbContext)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (product is null)
        {
            return false;
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
