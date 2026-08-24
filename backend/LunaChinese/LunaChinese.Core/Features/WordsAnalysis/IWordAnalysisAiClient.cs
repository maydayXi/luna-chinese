using LunaChinese.Core.Models;

namespace LunaChinese.Core.Features.WordsAnalysis;

/// <summary>
/// Sends Chinese words to the underlying AI provider and returns a completed analysis for each.<br/>
/// Implementations own the provider-specific concerns - prompt assembly, HTTP, JSON parsing,
/// and resilience - while <see cref="IWordAnalysisService"/> treats this purely as a source of
/// fresh analyses for words that are not already cached.
/// </summary>
public interface IWordAnalysisAiClient
{
    /// <summary>
    /// Analyzes a single batch of words in one provider call, returning an individual outcome for
    /// each requested word
    /// </summary>
    /// <param name="words">
    /// The words to analyze in this batch.
    /// Expected to be free of duplicates and no larger than the caller's configured batch size. 
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// One <see cref="WordAnalysisItemResult"/> for each word in <paramref name="words"/>.
    /// </returns>
    Task<IReadOnlyList<WordAnalysisItemResult>> AnalyzeAsync(
        IReadOnlyCollection<string> words,
        CancellationToken cancellationToken = default);
}