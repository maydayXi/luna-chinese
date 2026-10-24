namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;

/**
 *  {
 *      "content": {<see cref="GeminiResponseContent"/>},
 *      "finishReason": "STOP"
 *  }
 */

/// <summary>
/// A single generated candidate.
/// </summary>
/// <param name="Content"> The candidate's content, whose parts carry the JSON text. </param>
/// <param name="FinishReason"> Why generation stopped, for example <c>STOP</c> or <c>MAX_TOKENS</c>. </param>
internal sealed record GeminiCandidate(
    GeminiResponseContent? Content,
    string? FinishReason);