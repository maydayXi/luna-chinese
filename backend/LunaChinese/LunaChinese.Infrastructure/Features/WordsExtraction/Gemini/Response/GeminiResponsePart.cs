namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;

/**
 *  {
 *      "text": "json"
 *  }
 */

/// <summary>
/// One part of a candidate.
/// </summary>
/// <param name="Text">
/// The text of this part - the JSON payload when a response schema was set.
/// </param>
internal sealed record GeminiResponsePart(string? Text);