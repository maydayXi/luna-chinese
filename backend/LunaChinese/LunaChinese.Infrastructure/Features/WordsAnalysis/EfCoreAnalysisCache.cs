using System.Text.Json;
using LunaChinese.Core.Features.WordsAnalysis;
using LunaChinese.Core.Models;
using LunaChinese.Infrastructure.Context;
using LunaChinese.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace LunaChinese.Infrastructure.Features.WordsAnalysis;

/// <summary>
/// An <see cref="IAnalysisCache"/> backed by Entity Framework Core, storing each analysis as a JSON
/// document keyed by its canonical cache key and reaching it through one or more alias rows.
/// </summary>
/// <remarks>
/// Every surface form of a word — the requested key plus the Traditional and Simplified forms of the
/// analysis itself — becomes an alias pointing at the same <see cref="CachedAnalysis"/>, so any of them
/// resolves to a single stored entry.
/// </remarks>
/// <param name="dbContext">The database context holding the cached analyses and their aliases.</param>
public class EfCoreAnalysisCache(LunaChineseDbContext dbContext) : IAnalysisCache
{
    /// <summary>
    /// The options used to serialize and deserialize the stored JSON documents.
    /// </summary>
    /// <remarks>
    /// Web defaults (camel-cased property names, case-insensitive reads) are used on both sides, so
    /// changing them would make previously cached rows unreadable.
    /// </remarks>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Deserializes a stored JSON document back into a <see cref="WordAnalysis"/>.
    /// </summary>
    /// <param name="json">The JSON document read from the cache.</param>
    /// <returns>
    /// The deserialized <see cref="WordAnalysis"/>, or <c>null</c> if the document deserializes to JSON null.
    /// </returns>
    private static WordAnalysis? Deserialize(string json) =>
        JsonSerializer.Deserialize<WordAnalysis>(json, SerializerOptions);


    /// <inheritdoc/>
    public async Task<WordAnalysis?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        var json = await dbContext.Aliases
            .Where(alias => alias.Alias == cacheKey)
            .Select(alias => alias.Analysis.ContentJson)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return json is null ? null : Deserialize(json);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Duplicate keys are collapsed before the query, and rows whose stored JSON fails to deserialize
    /// are omitted from the result rather than reported as errors.
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, WordAnalysis>> GetManyAsync(IReadOnlyCollection<string> cacheKeys,
        CancellationToken cancellationToken = default)
    {
        if (cacheKeys.Count == 0) return new Dictionary<string, WordAnalysis>();

        IReadOnlyList<string> keys = [.. cacheKeys.Distinct()];

        var rows = await dbContext.Aliases
            .Where(alias => keys.Contains(alias.Alias))
            .Select(alias => new { alias.Alias, alias.Analysis.ContentJson })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, WordAnalysis> result = new(rows.Count);
        foreach (var row in rows)
        {
            var analysis = Deserialize(row.ContentJson);
            if (analysis is not null) result[row.Alias] = analysis;
        }

        return result;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The first-writer-wins check covers every surface form of the word, not just <paramref name="cacheKey"/>.
    /// A concurrent writer that wins the race between that check and the save is absorbed the same way:
    /// the alias primary key rejects the duplicate and the pending inserts are discarded.
    /// </remarks>
    public async Task SetAsync(string cacheKey, WordAnalysis analysis, CancellationToken cancellationToken = default)
    {
        List<string> keys =
        [
            ..
            new[]
            {
                cacheKey, WordCacheKey.Build(analysis.Word),
                WordCacheKey.Build(analysis.TraditionalWord),
                WordCacheKey.Build(analysis.SimplifiedWord)
            }.Where(word => !string.IsNullOrEmpty(word)).Distinct()
        ];

        // First-writer-wins fast path: if any of these surfaces already resolves to an entry,
        // leave the existing analysis in place.
        var alreadyCached = await dbContext.Aliases
            .AnyAsync(alias => keys.Contains(alias.Alias), cancellationToken)
            .ConfigureAwait(false);

        if (alreadyCached) return;

        try
        {
            dbContext.Analyses.Add(new CachedAnalysis
            {
                CanonicalKey = cacheKey,
                ContentJson = JsonSerializer.Serialize(analysis, SerializerOptions),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            dbContext.Aliases.AddRange(keys.Select(key => new AnalysisAlias
            {
                Alias = key,
                CanonicalKey = cacheKey
            }));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // A concurrent writer inserted an overlapping alias between the check above and this save
            // (the Aliases primary key rejects the duplicate).
            // First writer wins: discard our pending inserts and keep the context usable for later calls.
            dbContext.ChangeTracker.Clear();
        }
    }
}