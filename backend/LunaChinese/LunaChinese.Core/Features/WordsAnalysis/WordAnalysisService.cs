using System.Diagnostics;
using LunaChinese.Core.Enums;
using LunaChinese.Core.Models;

namespace LunaChinese.Core.Features.WordsAnalysis;

/// <summary>
/// Coordinates word analysis: it consults <see cref="IAnalysisCache"/> first, <br/>
/// sends only the uncached words to <see cref="IWordAnalysisAiClient"/> in batches, caches fresh results,
/// and reports progress per word. Individual words may fail without failing the batch. 
/// </summary>
/// <param name="cache">The cache consulted before analysis and populated with fresh results.</param>
/// <param name="aiClient">The AI client that analyzes words not found in the cache.</param>
public sealed class WordAnalysisService(
    IAnalysisCache cache,
    IWordAnalysisAiClient aiClient) : IWordAnalysisService
{
    private const int BatchSize = 10;

    /// <summary>
    /// Analyzes the command's words: normalizes and de-duplicates them, serves cache hits
    /// immediately, analyzes the remaining words in batches (one provider call each) while
    /// caching successes, and returns per-word results in the original request order.
    /// </summary>
    /// <param name="command">The words to analyze; blank entries are ignored and duplicates collapsed.</param>
    /// <param name="progress">An optional receiver notified as each word completes.</param>
    /// <param name="cancellationToken">A token to cancel the operation between batches.</param>
    /// <returns>
    /// A <see cref="BatchWordAnalysisResult"/> with one <see cref="WordAnalysisItemResult"/> per
    /// distinct word; individual words may fail without failing the batch.
    /// </returns>
    public async Task<BatchWordAnalysisResult> AnalyzeBatchAsync(
        AnalyzeWordsCommand command, 
        IProgress<WordAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var words = command.Words
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Select(word => word.Trim())
            .Distinct()
            .ToList();
        
        if (words.Count == 0) return new BatchWordAnalysisResult([]);

        int totalCount = words.Count,
            completedCount = 0;
        
        // Map each word to its cache key so results can be looked back up by word
        var cacheKeysByWord = words.ToDictionary(word => word, WordCacheKey.Build);
        
        // 1. Look up every word in the cache in a single call
        var cached = await cache.GetManyAsync(
            cacheKeysByWord.Values.Distinct().ToList(), cancellationToken)
            .ConfigureAwait(false);

        // Results keyed by word so the final list can preserve the original request order.
        Dictionary<string, WordAnalysisItemResult> resultByWord = new(totalCount);
        List<string> misses = [];
        
        words.ForEach(word =>
        {
            if (!cached.TryGetValue(cacheKeysByWord[word], out var analysis))
                misses.Add(word);
            else
            {
                resultByWord[word] = WordAnalysisItemResult.Success(word, analysis);
                completedCount++;
                progress?.Report(new WordAnalysisProgress(
                    word, completedCount, totalCount, true));
            }
        });
        
        // 2. Analyze the cache misses one batch (one provider call) at a time, caching successes.
        foreach (var batch in misses.Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchResults = await aiClient.AnalyzeAsync(batch, cancellationToken)
                .ConfigureAwait(false);

            var batchResultByWord = batchResults.ToDictionary(result => result.RequestedWord);

            foreach (var word in batch)
            {
                // Defend against a client that omits a word it was asked to analyze.
                if (!batchResultByWord.TryGetValue(word, out var result))
                    result = WordAnalysisItemResult.Failure(word, AiOperationErrorCode.Unknown, 
                        "The AI provider didn't return a result for this word.");
                if (result.IsSuccess)
                {
                    Debug.Assert(result.Analysis != null);
                    await cache.SetAsync(cacheKeysByWord[word], result.Analysis, cancellationToken)
                        .ConfigureAwait(false);
                }

                resultByWord[word] = result;
                completedCount++;
                progress?.Report(new WordAnalysisProgress(
                    word, completedCount, totalCount, false));
            }
        }
        
        // 3. Emit results in the original (distinct) request order.
        return new BatchWordAnalysisResult([.. words.Select(word => resultByWord[word])]);
    }
}