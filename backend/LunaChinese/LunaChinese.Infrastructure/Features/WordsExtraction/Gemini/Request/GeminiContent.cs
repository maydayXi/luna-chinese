namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

/**
 *  {
 *      "parts": [<see cref="GeminiPart"/>]
 *  }
 */

/// <summary>
/// A single ture of content, carrying one or more ordered parts.
/// </summary>
/// <param name="Parts">
/// The parts of this ture - for extraction, an instruction text part and an image part.
/// </param>
internal sealed record GeminiContent(IReadOnlyList<GeminiPart> Parts);