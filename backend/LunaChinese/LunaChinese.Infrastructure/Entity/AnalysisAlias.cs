namespace LunaChinese.Infrastructure.Entity;

/// <summary>
/// Maps one normalized surface form of a word to the canonical <see cref="CachedAnalysis"/>
/// that should serve it.
/// </summary>
public sealed class AnalysisAlias
{
    /// <summary>
    /// The normalized surface form used for lookup (Primary Key).
    /// </summary>
    public string Alias { get; set; } = null!;

    /// <summary>
    /// The canonical key of the analysis this alias resolves to (Foreign Key)
    /// </summary>
    public string CanonicalKey { get; set; } = null!;

    /// <summary>
    /// The analysis this alias resolves to.
    /// </summary>
    public CachedAnalysis Analysis { get; set; } = null!;
}