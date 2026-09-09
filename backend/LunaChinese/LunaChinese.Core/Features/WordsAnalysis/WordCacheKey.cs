using System.Text;

namespace LunaChinese.Core.Features.WordsAnalysis;

/// <summary>
/// Builds the cache key for a word.
/// The key deterministically folds away purely cosmetic differences in the
/// input — Unicode composition, full-width ASCII variants, and stray whitespace —
/// so that inputs that differ only in presentation resolve to the same entry.
/// </summary>
public static class WordCacheKey
{
    /// <summary>
    /// The distance between a full-width ASCII character (U+FF01-U+FF5E) and its
    /// half-width equivalent (U+0021-U+007E); subtracting it converts one to the other.
    /// </summary>
    private const int FullWidthToHalfWidthOffset = 0xFEE0;

    /// <summary>
    /// Builds the cache key for <paramref name="word"/>.
    /// </summary>
    /// <param name="word"> The word to build a key for; may be <see langword="null"/> or blank. </param>
    /// <returns>
    /// The normalized cache key, or <see cref="string.Empty"/> when the input is <see langword="null"/>,
    /// empty or whitespace. 
    /// </returns>
    public static string Build(string? word)
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;

        // Compose to a single canonical Unicode form so equivalent code point sequences match.
        var composed = word.Normalize(NormalizationForm.FormC);

        // Fold full-width ASCII variants and the ideographic space to their half-width forms,
        // which are almost always input-method artifacts rather than meaningful distinctions.
        StringBuilder folded = new(composed.Length);

        foreach (var ch in composed)
            folded.Append(FoldWidth(ch));

        return CollapseWhitespace(folded.ToString());
    }

    /// <summary>
    /// Maps a single full-width ASCII character to its half width equivalent,
    /// leaving every other character unchanged.
    /// </summary>
    /// <param name="ch">The character to fold.</param>
    /// <returns>
    /// The half-width equivalent of <paramref name="ch"/> when it is a full-width ASCII
    /// character or the ideographic space; otherwise <paramref name="ch"/> itself.
    /// </returns>
    private static char FoldWidth(char ch) => ch switch
    {
        '\u3000' => ' ',    // Full-width space
        >= '\uFF01' and <= '\uFF5E' => (char)(ch - FullWidthToHalfWidthOffset),
        _ => ch
    };

    /// <summary>
    /// Replaces each run of whitespace with a single space and trims the leading and trailing ends.
    /// </summary>
    /// <param name="value">The width-folded value to collapse.</param>
    /// <returns>
    /// <paramref name="value"/> with every interior whitespace run reduced to one space
    /// and no leading or trailing whitespace.
    /// </returns>
    private static string CollapseWhitespace(string value)
    {
        StringBuilder result = new(value.Length);
        bool pendingSpace = false;

        foreach (var ch in value)
        {
            // Remember that a gap occurred, but only emit it once a following non-whitespace character
            // confirms it is an interior gap, not a trailing one.
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }

            result.Append(ch);
        }

        return result.ToString();
    }
}