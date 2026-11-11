using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed class GetProductHandler(AppDbContext dbContext)
{
    public Task<Product?> HandleAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().SingleOrDefaultAsync(
            product => product.Id == id,
            cancellationToken);
}
