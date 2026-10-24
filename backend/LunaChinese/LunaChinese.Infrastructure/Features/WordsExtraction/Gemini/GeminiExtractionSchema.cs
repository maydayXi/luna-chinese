using LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini;

/// <summary>
/// The fixed response schema for word extraction: an object with a required <c>words</c> array of strings,
/// mirroring the analysis pipeline's expectation of a plain word list.
/// </summary>
internal static class GeminiExtractionSchema
{
    /// <summary>
    /// The property name carrying the extracted words.
    /// </summary>
    public const string WordsProperty = "words";

    /// <summary>
    /// Builds the <c>{ "words": string[] }</c> schema for the generation config.
    /// </summary>
    /// <returns>
    /// A shcema constraining the response to a required array of word strings.
    /// </returns>
    public static GeminiSchema Build() => new()
    {
        Type = GeminiSchemaType.Object,
        Properties = new Dictionary<string, GeminiSchema>
        {
            [WordsProperty] = new()
            {
                Type = GeminiSchemaType.Array,
                Items = new GeminiSchema
                {
                    Type = GeminiSchemaType.String
                }
            }
        },
        Required = [WordsProperty]
    };
}