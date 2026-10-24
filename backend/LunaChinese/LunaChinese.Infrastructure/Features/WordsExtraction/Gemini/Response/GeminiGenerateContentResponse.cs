namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;

/**
 *  {
 *      "candidates": [<see cref="GeminiCandidate"/>]
 *  }
 */

/// <summary>
/// The response body from Gemini <c>generateContent</c> endpoint. All members are nullable because a blocked, empty,
/// or malformed response may omit any of them; the client is responsible for treating missing pieces as an
/// <c>InvalidResponse</c> failure.
/// </summary>
/// <param name="Candidates">  </param>
/// <param name="PromptFeedback">  </param>
internal sealed record GeminiGenerateContentResponse(
    IReadOnlyList<GeminiCandidate>? Candidates,
    GeminiPromptFeedback? PromptFeedback);