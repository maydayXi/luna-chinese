namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

/**
 *  {
 *      "contents": [<see cref="GeminiContent"/>],
 *      "generationConfig": {<see cref="GeminiGenerationConfig"/>}
 *  }
 */

/// <summary>
/// The request body posted to the Gemini <c>genterateContent</c> endpoint.
/// Property names are PascalCase here and serialized as camelCase by the caller's web defaults serializer,
/// matching the field names the API excepts (<c>contents</c>, <c>gentationConfig</c>, and so on).
/// These types are internal; they are the Gemini wire format and must not leak past Infrastructure.
/// </summary>
/// <param name="Contents">
/// The ordered turns of the conversation; a single use ture for extraction.
/// </param>
/// <param name="GenerationConfig">
/// Output controls, including the JSON response schema.
/// </param>
internal sealed record GeminiGenerateContentRequest(
    IReadOnlyList<GeminiContent> Contents,
    GeminiGenerationConfig GenerationConfig);