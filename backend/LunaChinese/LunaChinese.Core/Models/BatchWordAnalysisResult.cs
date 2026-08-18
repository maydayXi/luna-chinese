namespace LunaChinese.Core.Models;

/// <summary>
/// The combined result of a batch word-analysis request, pairing each requested word's
/// individual outcome with aggregate success/failure counts.
/// </summary>
/// <param name="Items">The per-word results, one entry for each requested word.</param>
public record BatchWordAnalysisResult(IReadOnlyList<WordAnalysisItemResult> Items)
{
    /// <summary>
    /// The total number of words in the batch (successful and failed combined).
    /// </summary>
    public int TotalCount => Items.Count;

    /// <summary>
    /// The number of words that were analyzed successfully.
    /// </summary>
    public int SuccessCount => Items.Count(item => item.IsSuccess);

    /// <summary>
    /// The number of words that failed to be analyzed.
    /// </summary>
    public int FailureCount => Items.Count(item => !item.IsSuccess);

    /// <summary>
    /// <c>true</c> when every word in the batch was analyzed successfully (no failures).
    /// </summary>
    public bool IsFullySuccessful => FailureCount == 0;

    /// <summary>
    /// <c>true</c> when the batch contains a mix of successes and failures.
    /// </summary>
    public bool IsPartiallySuccessful => SuccessCount > 0 && FailureCount > 0;
}