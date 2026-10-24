namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;

/// <summary>
/// The schema-constrained payload carried as JSON text inside the first candidate part.
/// Even with a response schema, Gemini returns the JSON as a string,
/// so the client deserializes this from <see cref="GeminiResponsePart.Text"/>.
/// </summary>
/// <param name="Words"> The extracted words; null or empty when the model returned none. </param>
public record GeminiExtractedWords(IReadOnlyList<string>? Words);