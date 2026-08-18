namespace LunaChinese.Core.Models;

/// <summary>
/// The result of analyzing a Chinese word, including readings, meanings,
/// an example sentence, and a per-character breakdown.
/// </summary>
/// <param name="Word">The analyzed word as supplied.</param>
/// <param name="TraditionalWord">The word written in Traditional Chinese characters.</param>
/// <param name="SimplifiedWord">The word written in Simplified Chinese characters.</param>
/// <param name="Pinyin">The pinyin romanization of the word.</param>
/// <param name="HanjaReading">The Korean Hanja reading of the word.</param>
/// <param name="KoreanMeaning">The meaning of the word in Korean.</param>
/// <param name="EnglishMeaning">The meaning of the word in English.</param>
/// <param name="Example">An optional example sentence using the word.</param>
/// <param name="Characters">The analysis of each individual character in the word.</param>
/// <example>
/// var analysis = new WordAnalysis(
/// Word: "腳踏車",
/// TraditionalWord: "腳踏車",
/// SimplifiedWord: "脚踏车",
/// Pinyin: "jiǎo tà chē",
/// HanjaReading: "각답거",
/// KoreanMeaning: "자전거",
/// EnglishMeaning: "bicycle",
/// Example: example,
/// Characters: characters);
/// </example>
public record WordAnalysis(
    string Word,
    string TraditionalWord,
    string SimplifiedWord,
    string Pinyin,
    string HanjaReading,
    string KoreanMeaning,
    string EnglishMeaning,
    ExampleSentence? Example,
    IReadOnlyCollection<CharacterAnalysis> Characters);