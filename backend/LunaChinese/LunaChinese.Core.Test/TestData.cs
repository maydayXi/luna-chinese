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
    
    /// <summary>
    /// Builds a fully-populated analysis for "腳踏車" (bicycle), optionally overriding the
    /// as-supplied, Traditional, or Simplified writings.
    /// </summary>
    public static WordAnalysis Bicycle(
        string? word = null,
        string? traditional = null,
        string? simplified = null) =>
        new(
            Word: word ?? "腳踏車",
            TraditionalWord: traditional ?? "腳踏車",
            SimplifiedWord: simplified ?? "脚踏车",
            Pinyin: "jiǎo tà chē",
            HanjaReading: "각답거",
            KoreanMeaning: "자전거",
            EnglishMeaning: "bicycle",
            Example: new ExampleSentence(
                Chinese: "我騎腳踏車上班。",
                Pinyin: "Wǒ qí jiǎotàchē shàngbān.",
                Korean: "나는 자전거를 타고 출근한다.",
                English: "I ride a bicycle to work."),
            Characters:
            [
                new CharacterAnalysis(
                    Character: "腳", Traditional: "腳", Simplified: "脚",
                    Hun: "다리", Eum: "각", Radical: "月",
                    KoreanRadicalName: "육달월", EnglishRadicalName: "flesh radical",
                    KoreanRadicalMeaning: "몸이나 살", EnglishRadicalMeaning: "body or flesh",
                    KoreanMeaningStory: "몸과 연결된 발을 떠올린다.",
                    EnglishMeaningStory: "Think of the foot as part of the body."),
            ]);
}