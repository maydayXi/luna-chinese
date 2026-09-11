using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LunaChinese.Infrastructure.Context;

/// <summary>
/// Creates <see cref="LunaChineseDbContext"/> instances for the EF Core design-time tools
/// (<c>dotnet ef migrations</c>, <c>dotnet ef database update</c>), which cannot resolve the
/// context from the API host's dependency injection container.
/// </summary>
/// <remarks>
/// The connection string here is a local development SQLite file and is used only at design time;
/// the running application configures its own connection through dependency injection.
/// </remarks>
public sealed class LunaChineseDbContextFactory : IDesignTimeDbContextFactory<LunaChineseDbContext>
{
    /// <summary>
    /// Creates a new <see cref="LunaChineseDbContext"/> pointing at the local development SQLite database.
    /// </summary>
    /// <param name="args">Arguments passed by the design-time tooling; not used.</param>
    /// <returns>A context configured for design-time use.</returns>
    public LunaChineseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LunaChineseDbContext>()
            .UseSqlite("Data Source=lunachinese.dev.db")
            .Options;

        return new LunaChineseDbContext(options);
    }
}