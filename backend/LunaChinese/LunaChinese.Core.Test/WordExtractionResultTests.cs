using System.Runtime.InteropServices;
using LunaChinese.Core.Enums;
using LunaChinese.Core.Models.Extraction;

namespace LunaChinese.Core.Test;

/// <summary>
/// Behavioral tests for <see cref="WordExtractionResult"/>, covering its success and failure
/// factory methods and the state each factory guarantees.
/// </summary>
public class WordExtractionResultTests
{
    /// <summary>
    /// A successful result retains the extracted words, reports success, and carries no
    /// error information.
    /// </summary>
    [Fact]
    public void Success_WithWords_IsSuccessAndEchoesWordsWithoutError()
    {
        // Arrange
        IReadOnlyList<string> words = ["蓬勃", "腳踏車", "圖書館"];

        // Act
        var result = WordExtractionResult.Success(words);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(words, result.Words);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// An empty extracted-word list is still a successful result. The presence of the list,
    /// rather than the number of words in it, distinguishes success from failure.
    /// </summary>
    [Fact]
    public void Success_WithEmptyList_IsSuccessWithEmptyWordsNotFailure()
    {
        // Arrange
        IReadOnlyList<string> noWords = [];

        // Act
        var result = WordExtractionResult.Success(noWords);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Words);
        Assert.Empty(result.Words);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// A failed result reports failure, retains its error details, and contains no word list.
    /// </summary>
    [Fact]
    public void Failure_CarriesErrorAndHasNoWords()
    {
        #region Arrange
        const AiOperationErrorCode code = AiOperationErrorCode.RateLimited;
        const string message = "The provider rate limit was exceeded";
        #endregion

        // Act
        var result = WordExtractionResult.Failure(code, message);

        #region Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Words);
        Assert.Equal(code, result.ErrorCode);
        Assert.Equal(message, result.ErrorMessage);
        #endregion
    }

    /// <summary>
    /// The failure factory preserves every supported error classification so callers can
    /// distinguish the reason extraction failed.
    /// </summary>
    /// <param name="errorCode">The error classification supplied to the failure factory.</param>
    [Theory]
    [InlineData(AiOperationErrorCode.RateLimited)]
    [InlineData(AiOperationErrorCode.ProviderOverloaded)]
    [InlineData(AiOperationErrorCode.InvalidResponse)]
    [InlineData(AiOperationErrorCode.Timeout)]
    [InlineData(AiOperationErrorCode.Unknown)]
    public void Failure_PreservesGivenErrorCode(AiOperationErrorCode errorCode)
    {
        // Arrange
        const string error = nameof(error);

        // Act
        var result = WordExtractionResult.Failure(errorCode, error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.ErrorCode);
    }
}
