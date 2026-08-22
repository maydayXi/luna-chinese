using LunaChinese.Core.Models;

namespace LunaChinese.Core.Features.WordsAnalysis;

/// <summary>
/// Caches completed <see cref="WordAnalysis"/> results so previously analyzed words can be
/// served without invoking the AI provider again.
/// </summary>
public interface IAnalysisCache
{
    /// <summary>
    /// Retrieves the cached analysis for a single cache key.
    /// </summary>
    /// <param name="cacheKey">The cache key identifying the analysis.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The cached <see cref="WordAnalysis"/>, or <c>null</c> if the key is not cached.</returns>
    Task<WordAnalysis?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the cached analyses for multiple cache keys in a single call.
    /// </summary>
    /// <param name="cacheKeys">The cache keys to look up.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A dictionary mapping each cached key to its <see cref="WordAnalysis"/>; keys with no
    /// cached entry are omitted.
    /// </returns>
    Task<IReadOnlyDictionary<string, WordAnalysis>> GetManyAsync(
        IReadOnlyCollection<string> cacheKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an analysis in the cache under the given key, overwriting any existing entry.
    /// </summary>
    /// <param name="cacheKey">The cache key to store the analysis under.</param>
    /// <param name="analysis">The analysis to cache.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SetAsync(string cacheKey, WordAnalysis analysis, CancellationToken cancellationToken = default);
}