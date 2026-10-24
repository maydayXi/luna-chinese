namespace LunaChinese.Core.Models.Extraction;

/// <summary>
/// A page image submitted for word extraction,
/// carried as Base64 so <c>Core</c> stays independent of any transport concern
/// (multipart upload, file stream, or provider payload).
/// The <c>Api</c> boundary is responsible for decoding an upload into this shape.  
/// </summary>
/// <param name="Base64">
/// The image bytes encoded as a Base64 string, without any data-URI prefix.
/// </param>
/// <param name="MediaType">
/// The IANA media type of the image, for example <c>image/jpeg</c> or <c>image/png</c>.
/// </param>
public sealed record ExtractionImage(string Base64, string MediaType);