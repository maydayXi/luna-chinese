using System.Text.Json.Serialization;

namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

/**
 *  {
 *      "type": "OBJECT",
 *      "properties": {
 *          "words": { "type": "ARRAY", "items": { "type": "STRING" } }
 *      },
 *      "required": ["words"]
 *  }
 */

/// <summary>
/// A minimal subset of the Gemini response schema (itself a subset of OpenAPI 3.0) -
/// only the fields this project need's <see cref="Type"/>, its <see cref="Items"/> for arrays,
/// its <see cref="Properties"/> for objects, and the <see cref="Required"/> property names.
/// Unset fields are omitted so the schema stays valid.
/// </summary>
internal sealed record GeminiSchema
{
    /// <summary>
    /// The node's data type, one of the values in <see cref="GeminiSchemaType"/>
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// The element schema when <see cref="Type"/> is an array; otherwise omitted.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiSchema? Items { get; init; }
    
    /// <summary>
    /// The member schemas when <see cref="Type"/> is an object; otherwise omitted.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, GeminiSchema>? Properties { get; init; }
    
    /// <summary>
    /// The name of required properties for an object; otherwise omitted.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Required { get; init; }
}