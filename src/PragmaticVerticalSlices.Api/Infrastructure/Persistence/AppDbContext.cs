using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Features.Products;

namespace PragmaticVerticalSlices.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public const string DefaultSchema = "products_schema";

    protected virtual string ModelSchemaName => DefaultSchema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModelSchemaName);

        modelBuilder.Entity<Product>(product =>
        {
            product.HasKey(item => item.Id);
            product.Property(item => item.Name).HasMaxLength(200).IsRequired();
            product.Property(item => item.Description).HasMaxLength(1_000);
            product.Property(item => item.Price).HasPrecision(18, 2);
        });
    }
}
