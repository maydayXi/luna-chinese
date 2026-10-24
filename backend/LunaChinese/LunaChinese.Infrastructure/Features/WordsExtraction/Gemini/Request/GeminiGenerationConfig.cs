namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

/**
 * {
 *  "generationConfig": {
 *      "responseMimeType": "application/json",
 *      "responseSchema": {<see cref="GeminiSchema"/>}
 *  }
 * }
 */

/// <summary>
/// Output controls for the call. Setting <see cref="ResponseMimeType"/> to <c>application/json</c>
/// together with <see cref="Response"/>
/// </summary>
/// <param name="ResponseMimeType"></param>
/// <param name="ResponseSchema"></param>
internal sealed record GeminiGenerationConfig(
    string ResponseMimeType,
    GeminiSchema ResponseSchema);