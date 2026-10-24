namespace LunaChinese.API.Contracts;

/// <summary>
/// The response for successful word-extraction request: the Chinese word found in the image.
/// </summary>
/// <param name="Words">
/// The extracted words, in the ordered returned by the extractor; may be empty.
/// </param>
public sealed record ExtractWordsResponse(IReadOnlyList<string> Words);