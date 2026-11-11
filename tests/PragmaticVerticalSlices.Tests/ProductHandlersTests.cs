using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Features.Products;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Tests;

public sealed class ProductHandlersTests
{
    [Fact]
    public async Task CreateAndGetHandlers_PersistAndRetrieveProduct()
    {
        await using var dbContext = CreateDbContext();
        var createHandler = new CreateProductsHandler(dbContext);
        var getHandler = new GetProductHandler(dbContext);
        var request = new ProductRequest("Widget", "A test product", 12.50m);

        AssertValid(request);
        var createdProducts = await createHandler.HandleAsync([request], CancellationToken.None);
        var created = Assert.Single(createdProducts);
        var retrieved = await getHandler.HandleAsync(created.Id, CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(request.Name, retrieved.Name);
        Assert.Equal(request.Description, retrieved.Description);
        Assert.Equal(request.Price, retrieved.Price);
    }

    [Fact]
    public async Task CreateProductsHandler_PersistsAllProductsInRequest()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateProductsHandler(dbContext);
        var requests = new[]
        {
            new ProductRequest("First", null, 1),
            new ProductRequest("Second", "Second product", 2)
        };

        var products = await handler.HandleAsync(requests, CancellationToken.None);

        Assert.Equal(2, products.Count);
        Assert.Equal(requests.Select(request => request.Name), products.Select(product => product.Name));
        Assert.Equal(2, await dbContext.Products.CountAsync());
    }

    [Fact]
    public async Task GetHandler_ReturnsNullWhenProductDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetProductHandler(dbContext);

        var product = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(product);
    }

    [Fact]
    public async Task UpdateHandler_ChangesEditableFields()
    {
        await using var dbContext = CreateDbContext();
        var original = new Product("Old name", "Old description", 1);
        dbContext.Products.Add(original);
        await dbContext.SaveChangesAsync();
        var handler = new UpdateProductHandler(dbContext);
        var request = new ProductRequest("New name", "New description", 5);
        AssertValid(request);

        var updated = await handler.HandleAsync(original.Id, request, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("New name", updated.Name);
        Assert.Equal("New description", updated.Description);
        Assert.Equal(5, updated.Price);
    }

    [Fact]
    public async Task UpdateHandler_ReturnsNullWhenProductDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var handler = new UpdateProductHandler(dbContext);

        var updated = await handler.HandleAsync(
            Guid.NewGuid(),
            new ProductRequest("Valid name", null, 1),
            CancellationToken.None);

        Assert.Null(updated);
    }

    [Fact]
    public void UpdateRequest_RejectsInvalidFields()
    {
        var request = new ProductRequest(" ", new string('x', 1_001), 0);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task DeleteHandler_DeletesExistingProductAndReportsMissingProducts()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product("Delete me", null, 1);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var handler = new DeleteProductHandler(dbContext);

        var deleted = await handler.HandleAsync(product.Id, CancellationToken.None);
        var deletedAgain = await handler.HandleAsync(product.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(deletedAgain);
        Assert.False(await dbContext.Products.AnyAsync(item => item.Id == product.Id));
    }

    [Fact]
    public async Task ListHandler_ReturnsStablePagesWithDefaults()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Products.AddRange(
            new Product("Zulu", null, 2),
            new Product("Alpha", null, 1),
            new Product("Mike", null, 3));
        await dbContext.SaveChangesAsync();
        var handler = new ListProductsHandler(dbContext);

        var firstPage = await handler.HandleAsync(
            ProductPagination.DefaultPage,
            2,
            CancellationToken.None);
        var secondPage = await handler.HandleAsync(2, 2, CancellationToken.None);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(["Alpha", "Mike"], firstPage.Items.Select(item => item.Name));
        Assert.Equal(["Zulu"], secondPage.Items.Select(item => item.Name));
        Assert.Equal(1, ProductPagination.DefaultPage);
        Assert.Equal(20, ProductPagination.DefaultPageSize);
    }

    [Fact]
    public async Task ListHandler_UsesIdAsStableTieBreakerForEqualNames()
    {
        await using var dbContext = CreateDbContext();
        var products = Enumerable.Range(0, 3)
            .Select(_ => new Product("Same name", null, 1))
            .ToArray();
        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync();
        var handler = new ListProductsHandler(dbContext);

        var firstRead = await handler.HandleAsync(1, 3, CancellationToken.None);
        var secondRead = await handler.HandleAsync(1, 3, CancellationToken.None);

        Assert.Equal(
            products.Select(product => product.Id).Order().ToArray(),
            firstRead.Items.Select(product => product.Id).ToArray());
        Assert.Equal(
            firstRead.Items.Select(product => product.Id),
            secondRead.Items.Select(product => product.Id));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Pagination_RejectsValuesOutsideAllowedBounds(int page, int pageSize)
    {
        var errors = ProductPagination.Validate(page, pageSize);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Pagination_AcceptsMaximumPageSize()
    {
        var errors = ProductPagination.Validate(1, ProductPagination.MaximumPageSize);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("", null, 1)]
    [InlineData("Valid name", "Valid description", 0)]
    [InlineData("Valid name", null, -1)]
    public void ProductRequest_RejectsInvalidFields(string name, string? description, decimal price)
    {
        var request = new ProductRequest(name, description, price);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void ProductRequestValidation_RejectsEmptyListsAndReportsInvalidItemIndexes()
    {
        var emptyErrors = ProductRequestValidation.Validate([]);
        var itemErrors = ProductRequestValidation.Validate(
        [
            new ProductRequest("Valid", null, 1),
            new ProductRequest("", null, 0)
        ]);

        Assert.Contains("products", emptyErrors.Keys);
        Assert.Contains(itemErrors.Keys, key => key.StartsWith("products[1].", StringComparison.Ordinal));
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void AssertValid(ProductRequest request)
    {
        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true));
    }
}
