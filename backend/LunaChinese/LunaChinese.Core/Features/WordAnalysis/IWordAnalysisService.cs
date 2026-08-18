using LunaChinese.Core.Models;

namespace LunaChinese.Core.Features.WordAnalysis;

/// <summary>
/// Analyzes Chinese words, producing readings, meanings, and per-character breakdowns.
/// </summary>
public interface IWordAnalysisService
{
    /// <summary>
    /// Analyzes a batch of Chinese words, returning an individual result for each requested
    /// word alongside aggregate success/failure counts.
    /// </summary>
    /// <param name="command">The words to analyze.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="BatchWordAnalysisResult"/> containing a per-word outcome for every word
    /// in <paramref name="command"/>; individual words may fail without failing the batch.
    /// </returns>
    Task<BatchWordAnalysisResult> AnalyseBatchAsync(
        AnalyzeWordsCommand command,
        CancellationToken cancellationToken = default);
}