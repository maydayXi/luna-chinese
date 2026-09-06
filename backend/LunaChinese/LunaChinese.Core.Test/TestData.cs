using LunaChinese.Core.Models;

namespace LunaChinese.Core.Test;

/// <summary>
/// Builders for the value objects the service works with, so tests can construct a valid
/// <see cref="WordAnalysis"/> without repeating its many required members.
/// </summary>
internal static class TestData
{
    /// <summary>
    /// A minimal but valid analysis whose word fields echo <paramref name="word"/>, 
    /// so test can correlate a result back to the work that produced it.
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    public static WordAnalysis Analysis(string word) => new(
        Word: word,
        TraditionalWord: word,
        SimplifiedWord: word,
        Pinyin: "pinyin",
        HanjaReading: "한자음",
        KoreanMeaning: "뜻",
        EnglishMeaning: "meaning",
        Example: null,
        Characters: []);

    /// <summary>
    /// Shorthand for building a command from a list of words.
    /// </summary>
    /// <param name="words"></param>
    /// <returns></returns>
    public static AnalyzeWordsCommand Command(params string[] words) => new(words);
}