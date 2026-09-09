using LunaChinese.Core.Features.WordsAnalysis;

namespace LunaChinese.Core.Test;

/// <summary>
/// Behavioral tests for <see cref="WordCacheKey"/>: the cosmetic differences the key folds away
/// — Unicode composition, full-width ASCII variants and stray whitespace — and the blank input
/// that maps to <see cref="string.Empty"/>.
/// </summary>
public class WordCacheKeyTests
{
    /// <summary>
    /// Input that carries no word at all — null, empty, or nothing but whitespace of any
    /// width — has no meaningful key, so it maps to <see cref="string.Empty"/> rather than
    /// to a key made of blanks.
    /// </summary>
    /// <param name="input">The null or blank input under test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    [InlineData("\u3000")]
    public void Build_WithNullOrBlankInput_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, WordCacheKey.Build(input));
    }

    /// <summary>
    /// A word that is already normalized passes through untouched: normalization only removes
    /// cosmetic differences, it never rewrites the word itself.
    /// </summary>
    [Fact]
    public void Build_WithCleanWord_ReturnsUnchanged()
    {
        const string word = "腳踏車";
        Assert.Equal(word, WordCacheKey.Build(word));
    }

    /// <summary>
    /// Leading and trailing whitespace is stripped — including the ideographic space and tabs
    /// or newlines — so padding introduced while typing does not create a separate entry.
    /// </summary>
    /// <param name="input">The padded input.</param>
    /// <param name="expected">The trimmed key the input must produce.</param>
    [Theory]
    [InlineData(" 腳踏車 ", "腳踏車")]
    [InlineData("\u3000蘋果\u3000", "蘋果")]
    [InlineData("\t你好\n", "你好")]
    public void Build_TrimsSurroundingWhitespace(string input, string expected)
    {
        Assert.Equal(expected, WordCacheKey.Build(input));
    }
    
    /// <summary>
    /// A run of interior whitespace collapses to exactly one ordinary space, whatever the run
    /// is made of, so spacing variants of one phrase share a key.
    /// </summary>
    /// <param name="input">The input with an interior whitespace run.</param>
    /// <param name="expected">The collapsed key the input must produce.</param>
    [Theory]
    [InlineData("你好  世界", "你好 世界")]
    [InlineData("你好\u3000世界", "你好 世界")]
    [InlineData("你好 \t 世界", "你好 世界")]
    public void Build_CollapsesInteriorWhitespaceRunsToASingleSpace(string input, string expected)
    {
        Assert.Equal(expected, WordCacheKey.Build(input));
    }

    /// <summary>
    /// Full-width ASCII — letters, digits and punctuation alike — folds to its half-width form.
    /// These are input-method artifacts rather than meaningful distinctions.
    /// </summary>
    /// <param name="input">The full-width input.</param>
    /// <param name="expected">The half-width key the input must produce.</param>
    [Theory]
    [InlineData("ＡＢＣ", "ABC")]
    [InlineData("１２３", "123")]
    [InlineData("Ｃ＋＋", "C++")]
    public void Build_FoldsFullWidthAsciiToHalfWidth(string input, string expected)
    {
        Assert.Equal(expected, WordCacheKey.Build(input));
    }
    
    /// <summary>
    /// Canonically equivalent code point sequences compose to one form, so text that merely
    /// differs in how it is encoded resolves to the same key.
    /// </summary>
    [Fact]
    public void Build_ComposesToCanonicalUnicodeForm()
    {
        // "e" + combining acute accent (U+0301) must compose to the single code point "é" (U+00E9)
        // so the two encodings of the same text share one cache key.
        Assert.Equal("\u00e9", WordCacheKey.Build("e\u0301"));
    }

    /// <summary>
    /// Building a key from an already-built key changes nothing. Normalization can therefore be
    /// applied more than once — on write and again on read — without splitting the entry.
    /// </summary>
    [Fact]
    public void Build_IsIdempotent()
    {
        #region Arrange
        const string messy = "\u3000ＡＢ  Ｃ\t１２３\u3000";
        #endregion

        #region Act
        var once = WordCacheKey.Build(messy);
        var twice = WordCacheKey.Build(once);
        #endregion

        #region Assert
        Assert.Equal(once, twice);
        #endregion
    }

    /// <summary>
    /// Traditional and Simplified forms of a word keep distinct keys: the fold is cosmetic only,
    /// and collapsing scripts would be lossy.
    /// </summary>
    [Fact]
    public void Build_DoesNotFoldTraditionalAndSimplified()
    {
        // The same word written in each script. Folding these into one key would be lossy
        // (many Traditional characters share one Simplified form), so the key must keep them
        // distinct and leave cross-script de-duplication to the cache's aliasing layer.
        
        #region Act
        string traditional = WordCacheKey.Build("腳踏車"),
            simplified = WordCacheKey.Build("脚踏车");
        #endregion

        #region Assert
        Assert.NotEqual(traditional, simplified);
        #endregion
    }
}