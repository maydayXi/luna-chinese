using LunaChinese.Core.Enums;
using LunaChinese.Core.Models.Extraction;

namespace LunaChinese.Core.Features.WordsExtraction;

/// <summary>
/// Extracts candidate study words from a photographed page in a single AI provider call. 
/// Implementations own the provider-specific concerns and produce a plain list of Chinese words.
/// </summary>
public interface IWordExtractionAiClient
{
    /// <summary>
    /// Reads the supplied image and returns the Chinese words selected according to <paramref name="mode"/>.
    /// </summary>
    /// <param name="image">
    /// The page image to extract words from.
    /// </param>
    /// <param name="mode">
    /// Which words to pull from the image - all study words, or only those the reader marked.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="WordExtractionResult"/> carrying the extracted words on success,
    /// (possibly empty when none were found), or an <see cref="AiOperationErrorCode"/> on failure.
    /// </returns>
    Task<WordExtractionResult> ExtractAsync(ExtractionImage image, ExtractMode mode,
        CancellationToken cancellationToken = default);
}
