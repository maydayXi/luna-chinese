namespace LunaChinese.Core.Models;

/// <summary>
/// The outcome of analyzing a single requested word. Either <see cref="Analysis"/> is
/// populated (success) or <see cref="ErrorCode"/> and <see cref="ErrorMessage"/> describe
/// why the word could not be analyzed (failure).
/// </summary>
/// <param name="RequestedWord">The word as it was requested, echoed back to correlate the result.</param>
/// <param name="Analysis">The completed analysis, or <c>null</c> when the request failed.</param>
/// <param name="ErrorCode">A machine-readable error code, or <c>null</c> when the request succeeded.</param>
/// <param name="ErrorMessage">A human-readable error description, or <c>null</c> when the request succeeded.</param>
public record WordAnalysisItemResult(
    string RequestedWord,
    WordAnalysis? Analysis,
    string? ErrorCode,
    string? ErrorMessage)
{
    /// <summary>
    /// <c>true</c> when the word was analyzed successfully (i.e. <see cref="Analysis"/> is present).
    /// </summary>
    public bool IsSuccess => Analysis is not null;

    /// <summary>
    /// Creates a successful result carrying the completed analysis.
    /// </summary>
    /// <param name="requestedWord">The word as it was requested.</param>
    /// <param name="analysis">The completed analysis for the word.</param>
    /// <returns>A successful <see cref="WordAnalysisItemResult"/> with no error information.</returns>
    public static WordAnalysisItemResult Success(
        string requestedWord,
        WordAnalysis analysis) => new WordAnalysisItemResult(
        RequestedWord: requestedWord,
        Analysis: analysis,
        ErrorCode: null,
        ErrorMessage: null
        );

    /// <summary>
    /// Creates a failed result describing why the word could not be analyzed.
    /// </summary>
    /// <param name="requestedWord">The word as it was requested.</param>
    /// <param name="errorCode">A machine-readable error code.</param>
    /// <param name="errorMessage">A human-readable error description.</param>
    /// <returns>A failed <see cref="WordAnalysisItemResult"/> with no analysis.</returns>
    public static WordAnalysisItemResult Failure(
        string requestedWord,
        string errorCode,
        string errorMessage) => new WordAnalysisItemResult(
        RequestedWord: requestedWord,
        Analysis: null,
        ErrorCode: errorCode,
        ErrorMessage: errorMessage
    );
}