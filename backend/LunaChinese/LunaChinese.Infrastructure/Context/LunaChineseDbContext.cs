using LunaChinese.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace LunaChinese.Infrastructure.Context;

/// <summary>
/// Entity Framework Core context backing the word-analysis cache.
/// </summary>
/// <param name="options">The options used to configure this context, supplied by dependency injection.</param>
public sealed class LunaChineseDbContext(DbContextOptions<LunaChineseDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The alias entries mapping normalized surface forms to their canonical analyses.
    /// </summary>
    internal DbSet<AnalysisAlias> Aliases => Set<AnalysisAlias>();

    /// <summary>
    /// The cached word analyses, keyed by their canonical cache key.
    /// </summary>
    internal DbSet<CachedAnalysis> Analyses => Set<CachedAnalysis>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedAnalysis>(entity =>
        {
            entity.ToTable(nameof(Analyses));
            entity.HasKey(analysis => analysis.CanonicalKey);
            entity.Property(analysis => analysis.CanonicalKey).HasMaxLength(256);
            entity.Property(analysis => analysis.ContentJson).IsRequired();
            entity.Property(analysis => analysis.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<AnalysisAlias>(entity =>
        {
            entity.ToTable(nameof(Aliases));
            entity.HasKey(alias => alias.Alias);
            entity.Property(alias => alias.Alias).HasMaxLength(256);
            entity.Property(alias => alias.CanonicalKey).HasMaxLength(256).IsRequired();

            // Each alias resolves to exactly one canonical analysis;
            // remove its aliases with it.
            entity.HasOne(alias => alias.Analysis)
                .WithMany()
                .HasForeignKey(alias => alias.CanonicalKey)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}