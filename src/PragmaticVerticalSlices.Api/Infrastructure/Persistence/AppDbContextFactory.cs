using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace PragmaticVerticalSlices.Api.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var applicationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Could not locate the API application directory.");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(applicationDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Configure the PostgreSQL connection string with ConnectionStrings__DefaultConnection.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString,
    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
            "__EFMigrationsHistory",
            AppDbContext.DefaultSchema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
