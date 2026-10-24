namespace LunaChinese.Core.Enums;

/// <summary>
/// Specifies which items to include during extraction.
/// </summary>
public enum ExtractMode
{
    /// <summary>
    /// Extracts study words form all Chinese text found on the page.
    /// </summary>
    All,
    
    /// <summary>
    /// Extracts only the words the reader has marked - by highlight, box, circle, or underline.
    /// </summary>
    MarkedOnly
}
