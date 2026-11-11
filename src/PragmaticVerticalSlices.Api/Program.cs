using Microsoft.EntityFrameworkCore;
using PragmaticVerticalSlices.Api.Infrastructure.Errors;
using PragmaticVerticalSlices.Api.Infrastructure.Persistence;
using PragmaticVerticalSlices.Api.Features.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configure the PostgreSQL connection string with ConnectionStrings__DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString,
    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
            "__EFMigrationsHistory",
            AppDbContext.DefaultSchema))
        .UseSnakeCaseNamingConvention());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddScoped<CreateProductsHandler>();
builder.Services.AddScoped<GetProductHandler>();
builder.Services.AddScoped<ListProductsHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapProducts();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.Run();

public partial class Program
{
}
