namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;

/**
 *  {
 *      "parts": [<see cref="GeminiResponsePart"/>]
 *  }
 */

/// <summary>
/// The content of a candidate
/// </summary>
/// <param name="Parts">
/// The ordered parts; for a JSON response, the first part's text is the JSON. 
/// </param>
internal sealed record GeminiResponseContent(IReadOnlyList<GeminiResponsePart>? Parts);