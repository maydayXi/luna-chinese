using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LunaChinese.Infrastructure.Context;

/// <summary>
/// Creates <see cref="LunaChineseDbContext"/> instances for the EF Core design-time tools
/// (<c>dotnet ef migrations</c>, <c>dotnet ef database update</c>), which cannot resolve the
/// context from the API host's dependency injection container.
/// </summary>
/// <remarks>
/// The connection string is read from <see cref="ConnectionEnvVariable"/> environment variable.
/// There is no local fallback here - unlike SQL Server's integrated auth,
/// Postgres always needs explicit credentials, so a missing value fails fast instead of silently guessing wrong.   
/// </remarks>
public sealed class LunaChineseDbContextFactory : IDesignTimeDbContextFactory<LunaChineseDbContext>
{
    /// <summary>
    /// The environment variable holding the design-time SQL Server connection string.
    /// </summary>
    private const string ConnectionEnvVariable = "LUNA_CHINESE_DB_CONNECTION";

    /// <summary>
    /// Creates a new <see cref="LunaChineseDbContext"/> pointing at the local development SQLite database.
    /// </summary>
    /// <param name="args">Arguments passed by the design-time tooling; not used.</param>
    /// <returns>A context configured for design-time use.</returns>
    public LunaChineseDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvVariable) ??
                               throw new InvalidOperationException(
                                   $"Set the '{ConnectionEnvVariable}' environment variable to your Neon connection string before running EF Core design-time tools");

        var options = new DbContextOptionsBuilder<LunaChineseDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LunaChineseDbContext(options);
    }
}