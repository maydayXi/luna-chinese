using LunaChinese.Core.Models;

namespace LunaChinese.Infrastructure.Entity;

/// <summary>
/// A persisted <see cref="WordAnalysis"/>, stored as a JSON document and keyed by its canonical cache key.
/// </summary>
public sealed class CachedAnalysis
{
    /// <summary>
    /// The canonical cache key identifying this analysis.
    /// </summary>
    /// <remarks>
    /// Primary Key
    /// </remarks>
    public string CanonicalKey { get; set; } = null!;

    /// <summary>
    /// The analysis serialized as a JSON document.
    /// </summary>
    public string ContentJson { get; set; } = null!;

    /// <summary>
    /// When the entry was first created, in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}