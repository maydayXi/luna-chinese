namespace LunaChinese.Core.Models;

/// <summary>
/// A progress update emitted while a batch of words is being analyzed, reporting the word
/// that just finished and how far the batch has advanced.
/// </summary>
/// <param name="Word">The word that was just analyzed.</param>
/// <param name="CompletedCount">The number of words completed so far, including this one.</param>
/// <param name="TotalCount">The total number of words in the batch.</param>
/// <param name="FromCache">
/// <c>true</c> if the result for <paramref name="Word"/> came from cache rather than a fresh analysis.
/// </param>
public sealed record WordAnalysisProgress(
    string Word,
    int CompletedCount,
    int TotalCount,
    bool FromCache);