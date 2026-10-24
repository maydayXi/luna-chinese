namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini;

/// <summary>
/// 
/// </summary>
public record GeminiExtractionOptions
{
    /// <summary>
    /// The Gemini model to call, for example a current Flash or Flash-Lite model.
    /// Supplied by configuration so the model can change without a code change;
    /// model name are versioned and retired over time, so this is deliberately not hard-coded. 
    /// </summary>
    public required string Model { get; init; }
}