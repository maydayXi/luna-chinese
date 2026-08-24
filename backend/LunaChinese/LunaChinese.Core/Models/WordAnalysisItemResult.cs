using LunaChinese.Core.Enums;

namespace LunaChinese.Core.Models;

/// <summary>
/// The outcome of analyzing a single requested word. Either <see cref="Analysis"/> is
/// populated (success) or <see cref="ErrorCode"/> and <see cref="ErrorMessage"/> describe
/// why the word could not be analyzed (failure). Instances are created via the
/// <see cref="Success"/> and <see cref="Failure"/> factory methods.
/// </summary>
public sealed record WordAnalysisItemResult
{
    /// <summary>
    /// The word as it was requested, echoed back to correlate the result.
    /// </summary>
    public string RequestedWord { get; }

    /// <summary>
    /// The completed analysis, or <see langword="null"/> when the request failed.
    /// </summary>
    public WordAnalysis? Analysis { get; }

    /// <summary>
    /// The error code classifying the failure, or <see langword="null"/> when the request succeeded.
    /// </summary>
    public AiOperationErrorCode? ErrorCode { get; }

    /// <summary>
    /// A human-readable error description, or <see langword="null"/> when the request succeeded.
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// <see langword="true"/> when the word was analyzed successfully (i.e. <see cref="Analysis"/> is present).
    /// </summary>
    public bool IsSuccess => Analysis is not null;
    
    /// <summary>
    /// Initializes a new result. Private so that instances are created only through the
    /// <see cref="Success"/> and <see cref="Failure"/> factory methods, which enforce a
    /// valid success/failure state.
    /// </summary>
    /// <param name="requestedWord">The word as it was requested.</param>
    /// <param name="analysis">The completed analysis, or <see langword="null"/> on failure.</param>
    /// <param name="errorCode">The error code classifying the failure, or <see langword="null"/> on success.</param>
    /// <param name="errorMessage">A human-readable error description, or <see langword="null"/> on success.</param>
    private WordAnalysisItemResult(
        string requestedWord,
        WordAnalysis? analysis,
        AiOperationErrorCode? errorCode,
        string? errorMessage)
    {
        RequestedWord = requestedWord;
        Analysis = analysis;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result carrying the completed analysis.
    /// </summary>
    /// <param name="requestedWord">The word as it was requested.</param>
    /// <param name="wordAnalysis">The completed analysis for the word.</param>
    /// <returns>A successful <see cref="WordAnalysisItemResult"/> with no error information.</returns>
    public static WordAnalysisItemResult Success(
        string requestedWord,
        WordAnalysis wordAnalysis) => new(
        requestedWord: requestedWord,
        analysis: wordAnalysis,
        errorCode: null,
        errorMessage: null);

    /// <summary>
    /// Creates a failed result describing why the word could not be analyzed.
    /// </summary>
    /// <param name="requestedWord">The word as it was requested.</param>
    /// <param name="errorCode">A machine-readable error code.</param>
    /// <param name="errorMessage">A human-readable error description.</param>
    /// <returns>A failed <see cref="WordAnalysisItemResult"/> with no analysis.</returns>
    public static WordAnalysisItemResult Failure(
        string requestedWord,
        AiOperationErrorCode errorCode,
        string errorMessage) => new(
        requestedWord,
        analysis: null,
        errorCode,
        errorMessage);
}