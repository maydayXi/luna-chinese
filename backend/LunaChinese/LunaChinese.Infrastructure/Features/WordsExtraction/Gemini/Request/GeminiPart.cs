using System.Text.Json.Serialization;

namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

/**
 *  {
 *      "text": "......"
 *  }
 *  or
 *  {
 *      "inlineData": {
 *          "mineType": "image/jpeg", "data": "base64string" 
 *      }
 *  }
 */

/// <summary>
/// One part of a turn. A part is ether text or inline binary data; exactly one of <see cref="Text"/> or
/// <see cref="InlineData"/> is set, and the unset one is omitted from the JSON so the API sees a well-formed part.
/// Use <see cref="FromText"/> and <see cref="FromImage"/> rather than constructing directly.
/// </summary>
internal sealed record GeminiPart
{
    /// <summary>
    /// The text of this part, or <see langword="null"/> when this is an image part.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// The inline image of this part, or <see langword="null"/> when this is a text part.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiInlineData? InlineData { get; init; }

    /// <summary>
    /// Creates a text part.
    /// </summary>
    /// <param name="text">
    /// The instruction or prompt text.
    /// </param>
    /// <returns>
    /// A part carrying only text.
    /// </returns>
    public static GeminiPart FromText(string text) => new()
    {
        Text = text
    };

    /// <summary>
    /// Creates an inline image part.
    /// </summary>
    /// <param name="mimeType">
    /// The image media type, for example <c>image/jpeg</c>.
    /// </param>
    /// <param name="base64">
    /// The image bytes encoded as Base64, without a data-URI prefix.
    /// </param>
    /// <returns>
    /// A part carrying only inline image data.
    /// </returns>
    public static GeminiPart FromImage(string mimeType, string base64) => new()
    {
        InlineData = new GeminiInlineData(mimeType, base64)
    };
};