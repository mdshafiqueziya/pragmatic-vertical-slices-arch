using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Npgsql;
using PragmaticVerticalSlices.Api.Features.Products;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;

namespace PragmaticVerticalSlices.Tests;

public sealed class ProductApiIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private ProductApiFactory? _factory;
    private string? _schemaName;
    private string? _schemaConnectionString;

    public async Task InitializeAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection to a PostgreSQL test database before running integration tests.");
        }

        _schemaName = $"product_tests_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(baseConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{_schemaName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = _schemaName
        };
        _schemaConnectionString = connectionBuilder.ConnectionString;
        _factory = CreateFactory(_schemaConnectionString, _schemaName!);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();

        await using var schemaConnection = new NpgsqlConnection(_schemaConnectionString);
        await schemaConnection.OpenAsync();
        await using var schemaCommand = schemaConnection.CreateCommand();
        schemaCommand.CommandText = "SELECT current_schema()";
        Assert.Equal(_schemaName, await schemaCommand.ExecuteScalarAsync());
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_schemaName is null)
        {
            return;
        }

        var baseConnectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection was removed before test cleanup.");
        await using var connection = new NpgsqlConnection(baseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP SCHEMA \"{_schemaName}\" CASCADE";
        await command.ExecuteNonQueryAsync();
    }

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
        _factory = CreateFactory(_schemaConnectionString!, _schemaName!);
        var restartedClient = _factory.CreateClient();
        var persistedProduct = await restartedClient.GetFromJsonAsync<ProductResponse>(
            $"/api/products/{widget.Id}",
            JsonOptions);
        Assert.Equal("Updated widget", persistedProduct?.Name);
    }

    private static ProductApiFactory CreateFactory(string connectionString, string schemaName) =>
        new(connectionString, schemaName);

    private sealed class ProductApiFactory(string connectionString, string schemaName)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(connectionString)
                        .UseSnakeCaseNamingConvention()
                        .ReplaceService<IModelCacheKeyFactory, TestModelCacheKeyFactory>());
                services.RemoveAll<AppDbContext>();
                services.AddScoped<AppDbContext>(serviceProvider => new TestAppDbContext(
                    serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>(),
                    schemaName));
            });
        }
    }

    private sealed class TestAppDbContext(
        DbContextOptions<AppDbContext> options,
        string schemaName) : AppDbContext(options)
    {
        public string SchemaName { get; } = schemaName;

        protected override string ModelSchemaName => SchemaName;
    }

    private sealed class TestModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) =>
            new TestModelCacheKey(
                context.GetType(),
                (context as TestAppDbContext)?.SchemaName,
                designTime);
    }

    private readonly record struct TestModelCacheKey(
        Type ContextType,
        string? SchemaName,
        bool DesignTime);
}
