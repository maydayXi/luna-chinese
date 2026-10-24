using LunaChinese.Core.Enums;

namespace LunaChinese.Core.Models.Extraction;

/// <summary>
/// The outcome of extracting words from a single <see cref="ExtractionImage"/>.
/// Either <see cref="Words"/> is populated (success) or <see cref="ErrorCode"/> and <see cref="ErrorMessage"/> describe
/// why the words could not be extracted (failure).
/// A successful extraction that found no words carries an empty <see cref="Words"/> list - that is not a failure.
/// Instances are created via the <see cref="Success"/> and <see cref="Failure"/> factory methods.
/// </summary>
public sealed record WordExtractionResult
{
    /// <summary>
    /// The Chinese words extracted from the image , or <see langword="null"/> when extraction failed.
    /// Maybe empty when extraction succeeded but the image contained no eligible words. 
    /// </summary>
    public IReadOnlyList<string>? Words { get; }

    /// <summary>
    /// The error code classifying the failure, or <see langword="null"/> when extraction succeeded.
    /// </summary>
    public AiOperationErrorCode? ErrorCode { get; }

    /// <summary>
    /// A human-readable error description, or <see langword="null"/> when extraction succeeded.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// <see langword="true"/> when words were extracted successfully (i.e. <see cref="Words"/> is present, 
    /// whether or not it is empty).
    /// </summary>
    public bool IsSuccess => Words is not null;

    /// <summary>
    /// Initializes a new result. Private so that instances are created only through the
    /// <see cref="Success"/> and <see cref="Failure"/> factory methods, which enforce a
    /// valid success/failure state.
    /// </summary>
    /// <param name="words">The extracted words, or <see langword="null"/> on failure.</param>
    /// <param name="errorCode">The error code classifying the failure, or <see langword="null"/> on success.</param>
    /// <param name="errorMessage">A human-readable error description, or <see langword="null"/> on success.</param>
    private WordExtractionResult(IReadOnlyList<string>? words,
        AiOperationErrorCode? errorCode, string? errorMessage)
    {
        Words = words;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result carrying the extracted words.
    /// An empty <paramref name="words"/> list is a valid success, 
    /// meaning the image contained no eligible words.
    /// </summary>
    /// <param name="words">The words extracted from the image; maybe empty.</param>
    /// <returns>A successful <see cref="WordExtractionResult"/> with no error information.</returns>
    public static WordExtractionResult Success(IReadOnlyList<string> words) => new(
        words: words,
        errorCode: null,
        errorMessage: null);

    /// <summary>
    /// Creates a failed result describing why extraction could not be extracted.
    /// </summary>
    /// <param name="errorCode">A machine-readable error code.</param>
    /// <param name="errorMessage">A human-readable error description.</param>
    /// <returns>A failed <see cref="WordExtractionResult"/> with no extracted words.</returns>
    public static WordExtractionResult Failure(
        AiOperationErrorCode errorCode,
        string errorMessage) => new(
        words: null,
        errorCode: errorCode,
        errorMessage: errorMessage);
};
