using LunaChinese.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LunaChinese.Infrastructure.Extensions;

/// <summary>
/// Dependency-injection registration for the infrastructure's Entity Framework Core context.
/// </summary>
public static class ContextExtensions
{
    /// <summary>
    /// Registers <see cref="LunaChineseDbContext"/> with the service collection, choosing the
    /// database provider from the host environment: SQLite in Development, Postgres SQL (Neon) elsewhere.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register the context with.</param>
    /// <param name="configuration">
    /// The configuration read for connection strings: <c>Sqlite</c> in Development
    /// (falling back to a local <c>lunachinese.dev.db</c> file) and <c>Postgres</c> otherwise.
    /// </param>
    /// <param name="hostEnvironment">The host environment deciding which provider is used.</param>
    /// <returns>The same <paramref name="serviceCollection"/>, so calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown outside Development when the <c>Postgres</c> connection string is missing;
    /// production must never silently fall back to a local file database.
    /// </exception>
    public static IServiceCollection AddLunaChineseContext(this IServiceCollection serviceCollection,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        serviceCollection.AddDbContext<LunaChineseDbContext>(options =>
        {
            if (hostEnvironment.IsDevelopment())
            {
                var sqlLiteConnectionString = configuration.GetConnectionString("Sqlite")
                              ?? "Data Source=lunachinese.dev.db";
                options.UseSqlite(sqlLiteConnectionString);
            }
            else
            {
                const string postgres = "Postgres";
                var sqlProvider = configuration.GetConnectionString(postgres)
                                  ?? throw new InvalidOperationException(
                                      $"Missing '{postgres}' connection string for the production database.");
                
                options.UseNpgsql(sqlProvider, npgsql => npgsql.EnableRetryOnFailure());
            }
        });

        return serviceCollection;
    }
}
