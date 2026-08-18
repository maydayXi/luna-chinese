namespace LunaChinese.Core.Models;

/// <summary>
/// Request to analyze one or more Chinese words.
/// </summary>
/// <param name="Words">The Chinese words to analyze.</param>
public sealed record AnalyzeWordsCommand(
    IReadOnlyCollection<string> Words);