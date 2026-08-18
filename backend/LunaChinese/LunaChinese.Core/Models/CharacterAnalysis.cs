namespace LunaChinese.Core.Models;

/// <summary>
/// The analysis of a single Chinese character, including its readings,
/// radical information, and mnemonic story.
/// </summary>
/// <param name="Character">The character as supplied.</param>
/// <param name="Traditional">The character written in Traditional Chinese.</param>
/// <param name="Simplified">The character written in Simplified Chinese.</param>
/// <param name="Hun">The Korean gloss of the character's meaning (훈).</param>
/// <param name="Eum">The Korean reading of the Hanja character, i.e. its romanization (음).</param>
/// <param name="Radical">The radical of the character.</param>
/// <param name="RadicalNameKorea">The name of the radical in Korean.</param>
/// <param name="RadicalNameEnglish">The name of the radical in English.</param>
/// <param name="RadicalMeaningKorea">The meaning of the radical in Korean.</param>
/// <param name="RadicalMeaningEnglish">The meaning of the radical in English.</param>
/// <param name="StoryKorea">A mnemonic story for the character in Korean.</param>
/// <param name="StoryEnglish">A mnemonic story for the character in English.</param>
/// <example>
/// var character = new CharacterAnalysis(
/// Character: "腳",
/// TraditionalCharacter: "腳",
/// SimplifiedCharacter: "脚",
/// Hun: "다리",
/// Eum: "각",
/// Radical: "月",
/// KoreanRadicalName: "육달월",
/// EnglishRadicalName: "flesh radical",
/// KoreanRadicalMeaning: "몸이나 살",
/// EnglishRadicalMeaning: "body or flesh",
/// KoreanMnemonicStory: "몸과 연결된 발을 떠올린다.",
/// EnglishMnemonicStory: "Think of the foot as part of the body.");
/// </example>
public sealed record CharacterAnalysis(
    string Character,
    string Traditional,
    string Simplified,
    string Hun,
    string Eum,
    string Radical,
    string RadicalNameKorea,
    string RadicalNameEnglish,
    string RadicalMeaningKorea,
    string RadicalMeaningEnglish,
    string StoryKorea,
    string StoryEnglish);