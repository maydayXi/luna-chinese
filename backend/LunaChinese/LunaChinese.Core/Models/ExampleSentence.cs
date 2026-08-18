namespace LunaChinese.Core.Models;

/// <summary>
/// An example sentence illustrating the use of a word, with translations.
/// </summary>
/// <param name="Chinese">The sentence in Chinese.</param>
/// <param name="Pinyin">The pinyin romanization of the sentence.</param>
/// <param name="Korean">The Korean translation of the sentence.</param>
/// <param name="English">The English translation of the sentence.</param>
/// <example>
/// var example = new ExampleSentence(
/// Chinese: "我騎腳踏車上班。",
/// Pinyin: "Wǒ qí jiǎotàchē shàngbān.",
/// KoreanTranslation: "나는 자전거를 타고 출근한다.",
/// EnglishTranslation: "I ride a bicycle to work.");
/// </example>
public sealed record ExampleSentence(
    string Chinese,
    string Pinyin,
    string Korean,
    string English);