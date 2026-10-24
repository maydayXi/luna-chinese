namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;

/// <summary>
/// Feedback explaining why a prompt was rejected before generation.
/// </summary>
/// <param name="BlockReason">
/// The reason the prompt was blocked, for example <c>SAFETY</c>
/// </param>
internal sealed record GeminiPromptFeedback(string? BlockReason);