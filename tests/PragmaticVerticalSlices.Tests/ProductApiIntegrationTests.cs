using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PragmaticVerticalSlices.Tests;

public sealed class ProductApiIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = $"products-tests-{Guid.NewGuid():N}";
    private ProductApiFactory? _factory;

    public Task InitializeAsync()
    {
        _factory = CreateFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() =>
        await _factory!.DisposeAsync();

    [Fact]
    public async Task ProductApi_ValidatesPerformsCrudPaginatesAndPersistsAcrossHostRestart()
    {
        var client = _factory!.CreateClient();

        var invalidProduct = await client.PostAsJsonAsync(
            "/api/products",
            new[]
            {
                new ProductRequest("Valid", null, 1),
                new ProductRequest("", null, 0)
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidProduct.StatusCode);
        using (var validation = await JsonDocument.ParseAsync(
                   await invalidProduct.Content.ReadAsStreamAsync()))
        {
            Assert.True(validation.RootElement.GetProperty("errors")
                .TryGetProperty("products[1].Name", out _));
        }

        var createdResponse = await client.PostAsJsonAsync(
            "/api/products",
            new[]
            {
                new ProductRequest("Alpha", null, 2),
                new ProductRequest("Widget", "A useful widget", 12.5m),
                new ProductRequest("Zebra", null, 3)
            });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal("/api/products", createdResponse.Headers.Location?.OriginalString);
        var createdProducts = await createdResponse.Content.ReadFromJsonAsync<ProductResponse[]>(JsonOptions);
        Assert.NotNull(createdProducts);
        var alpha = createdProducts[0];
        var widget = createdProducts[1];
        var zebra = createdProducts[2];

        var productsAfterBatch = await client.GetFromJsonAsync<ProductPage>(
            "/api/products",
            JsonOptions);
        Assert.Equal(3, productsAfterBatch?.TotalCount);

        var retrieved = await client.GetFromJsonAsync<ProductResponse>(
            $"/api/products/{widget.Id}",
            JsonOptions);
        Assert.Equal(widget.Id, retrieved?.Id);

        var defaultPage = await client.GetFromJsonAsync<ProductPage>(
            "/api/products",
            JsonOptions);
        Assert.NotNull(defaultPage);
        Assert.Equal(1, defaultPage.Page);
        Assert.Equal(20, defaultPage.PageSize);
        Assert.Equal(3, defaultPage.TotalCount);

        var secondPage = await client.GetFromJsonAsync<ProductPage>(
            "/api/products?page=2&pageSize=2",
            JsonOptions);
        Assert.Equal([zebra.Id], secondPage?.Items.Select(product => product.Id));

        var invalidPage = await client.GetAsync("/api/products?page=0&pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);

        var updatedResponse = await client.PutAsJsonAsync(
            $"/api/products/{widget.Id}",
            new ProductRequest("Updated widget", "Updated", 15));
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.Equal("Updated widget", updated?.Name);
        Assert.Equal(15, updated?.Price);

        var missingUpdate = await client.PutAsJsonAsync(
            $"/api/products/{Guid.NewGuid()}",
            new ProductRequest("Missing", null, 1));
        Assert.Equal(HttpStatusCode.NotFound, missingUpdate.StatusCode);

        var deleted = await client.DeleteAsync($"/api/products/{alpha.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var missingProduct = await client.GetAsync($"/api/products/{alpha.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingProduct.StatusCode);
        var missingDelete = await client.DeleteAsync($"/api/products/{alpha.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);

        await _factory.DisposeAsync();
        _factory = CreateFactory();
        var restartedClient = _factory.CreateClient();
        var persistedProduct = await restartedClient.GetFromJsonAsync<ProductResponse>(
            $"/api/products/{widget.Id}",
            JsonOptions);
        Assert.Equal("Updated widget", persistedProduct?.Name);
    }

    private ProductApiFactory CreateFactory() =>
        new(_databaseName, _databaseRoot);

    private sealed class ProductApiFactory(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName, databaseRoot));
            });
        }
    }
}
