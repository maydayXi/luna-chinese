namespace LunaChinese.Core.Enums;

/// <summary>
/// Identifies why an AI-backed operation (such as word analysis) failed.
/// </summary>
public enum AiOperationErrorCode
{
    /// <summary>
    /// The request was rejected because the AI provider's rate limit was exceeded.
    /// </summary>
    RateLimited,

    /// <summary>
    /// The AI provider was temporarily overloaded and could not service the request.
    /// </summary>
    ProviderOverloaded,

    /// <summary>
    /// The provider returned a response that could not be parsed or did not match the expected shape.
    /// </summary>
    InvalidResponse,

    /// <summary>
    /// The operation did not complete within the allotted time.
    /// </summary>
    Timeout,

    /// <summary>
    /// The operation failed for an unclassified or unexpected reason.
    /// </summary>
    Unknown
}