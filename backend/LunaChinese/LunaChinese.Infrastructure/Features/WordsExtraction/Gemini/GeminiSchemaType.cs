namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini;

/// <summary>
/// The Gemini schema type values. These are the OpenAPI-style type name used by the <c>responseSchema</c> field.
/// </summary>
/// <remarks>
/// The exactly casing the endpoint accepts (upper - vs lower-case) should be confirmed against a live call;
/// centralizing the values here keeps that a single-line change.
/// </remarks>
internal static class GeminiSchemaType
{
    public const string Object =  "OBJECT";
    public const string Array =  "ARRAY";
    public const string String =  "STRING";
}