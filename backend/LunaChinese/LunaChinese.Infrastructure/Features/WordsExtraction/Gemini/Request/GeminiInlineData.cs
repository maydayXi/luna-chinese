namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;

/**
 *  { "mimeType": "image/jpeg", "data": "base64string" }
 */

/// <summary>
/// Inline binary data sent with the request, such asa the page image.
/// </summary>
/// <param name="MimeType">
/// The IANA media type of the data, for example <c>image/jpeg</c>.
/// </param>
/// <param name="Data">
/// The bytes encoded as a Base64 string. 
/// </param>
internal sealed record GeminiInlineData(string MimeType, string Data);