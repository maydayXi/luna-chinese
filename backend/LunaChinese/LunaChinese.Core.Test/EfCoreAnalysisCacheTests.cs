using LunaChinese.Core.Features.WordsAnalysis;
using LunaChinese.Core.Models;
using LunaChinese.Infrastructure.Context;
using LunaChinese.Infrastructure.Features.WordsAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LunaChinese.Core.Test;

/// <summary>
/// Behavioral tests for <see cref="EfCoreAnalysisCache"/>, run against a real SQLite database held
/// in memory rather than a substitute, so the parts that only exist in the storage layer are actually
/// exercised: the alias rows that let every writing of a word reach one stored entry, the JSON
/// round-trip, and the first-writer-wins behavior of <c>SetAsync</c>.
/// </summary>
public class EfCoreAnalysisCacheTests : IDisposable
{
    /// <summary>
    /// The connection to the in-memory database. SQLite keeps such a database alive only while a
    /// connection to it is open, so this one is held open for the whole test and closed in <see cref="Dispose"/>.
    /// </summary>
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Context options bound to <see cref="_connection"/>, so every context built during a test
    /// sees the same database.
    /// </summary>
    private readonly DbContextOptions<LunaChineseDbContext> _options;

    /// <summary>
    /// Creates a fresh context over the shared database.
    /// </summary>
    /// <returns>A new <see cref="LunaChineseDbContext"/> for the test's database.</returns>
    private LunaChineseDbContext NewContext() => new(_options);

    /// <summary>
    /// Creates the system under test over a context of its own.
    /// Each call gets a separate context so a read cannot be served out of a previous write's change
    /// tracker: results have to come back from the database.
    /// </summary>
    /// <returns>An <see cref="EfCoreAnalysisCache"/> over a fresh context.</returns>
    private EfCoreAnalysisCache NewCache() => new(NewContext());

    /// <summary>
    /// Opens the shared connection and creates the schema, giving each test an empty cache.
    /// </summary>
    public EfCoreAnalysisCacheTests()
    {
        // A shared in-memory connection: the database lives only as long as the connection is open
        // so it's keep open for the lifetime of the test and disposed at end.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<LunaChineseDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new LunaChineseDbContext(_options);
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// A key that was never written is a miss, reported as <c>null</c> rather than as an error.
    /// This is the signal the service uses to decide a word still needs the AI provider.
    /// </summary>
    [Fact]
    public async Task GetAsync_UncachedKey_ReturnsNull()
    {
        const string word = "蘋果";
        var result = await NewCache()
            .GetAsync(WordCacheKey.Build(word));
        Assert.Null(result);
    }

    /// <summary>
    /// The basic round-trip: what <c>SetAsync</c> stores, a later <c>GetAsync</c> under the same key returns.
    /// The read runs on a different context, so the value genuinely came back out of the database.
    /// </summary>
    [Fact]
    public async Task SetAsync_ThenGetAsyncByCanonicalKey_ReturnsStoredAnalysis()
    {
        #region Arrange

        const string expected = "bicycle";
        const string word = "腳踏車";
        var key = WordCacheKey.Build(word);

        #endregion

        #region Act

        await NewCache().SetAsync(key, TestData.Bicycle());
        var result = await NewCache().GetAsync(key);

        #endregion

        #region Assert

        Assert.NotNull(result);
        Assert.Equal(expected, result.EnglishMeaning);

        #endregion
    }

    /// <summary>
    /// The alias scheme: a word cached under its Traditional writing is also reachable by its Simplified
    /// one, even though that form was never the canonical key. Without this the same word would be
    /// analyzed twice — once per script — because <see cref="WordCacheKey"/> deliberately keys the two
    /// writings apart.
    /// </summary>
    [Fact]
    public async Task SetAsync_StoredUnderTraditional_GetAsyncBySimplified_HitsSameEntry()
    {
        #region Arrange

        const string traditionalBike = "腳踏車";
        const string simplifiedBike = "脚踏车";
        // Stored key by the traditional writing 
        await NewCache().SetAsync(WordCacheKey.Build(traditionalBike), TestData.Bicycle());

        #endregion

        #region Act

        // And looked up by simplified writing, which was never the canonical key.
        var result = await NewCache().GetAsync(WordCacheKey.Build(simplifiedBike));

        #endregion

        #region Assert

        Assert.NotNull(result);
        Assert.Equal("자전거", result.KoreanMeaning);

        #endregion
    }

    /// <summary>
    /// Bulk lookup keys the result by the key the caller asked for, not by the canonical key it resolved
    /// to, so the caller can correlate entries back to its own request. Two aliases of one stored analysis
    /// therefore come back as two entries.
    /// </summary>
    [Fact]
    public async Task GetManyAsync_ReturnsDictionaryKeyedByTheRequestedKeys()
    {
        #region Arrange
        const string expected = "bicycle";
        const string traditionalBike = "腳踏車";
        const string simplifiedBike = "脚踏车";

        await NewCache().SetAsync(WordCacheKey.Build(traditionalBike), TestData.Bicycle());

        string traditionalKey = WordCacheKey.Build(traditionalBike),
            simplifiedKey = WordCacheKey.Build(simplifiedBike);
        #endregion

        #region Act
        var result = await NewCache().GetManyAsync(
            [traditionalKey, simplifiedKey]);
        #endregion

        #region Assert
        // Both requested keys resolve, and each entry is keyed by the key that was asked for.
        Assert.True(result.ContainsKey(traditionalKey));
        Assert.True(result.ContainsKey(simplifiedKey));
        Assert.Equal(expected, result[simplifiedKey].EnglishMeaning);
        #endregion
    }

    /// <summary>
    /// Uncached keys are absent from the result rather than present with a null value.
    /// That absence is how the service separates cache hits from the misses it sends to the AI provider.
    /// </summary>
    [Fact]
    public async Task GetManyAsync_OmitUncachedKeys()
    {
        #region Arrage
        const string bike = "腳踏車", apple = "蘋果";
        await NewCache().SetAsync(bike, TestData.Bicycle());
        #endregion

        #region Act
        var result = await NewCache().GetManyAsync(
            [WordCacheKey.Build(bike), WordCacheKey.Build(apple)]);
        #endregion

        #region Assert
        Assert.True(result.ContainsKey(WordCacheKey.Build(bike)));
        Assert.False(result.ContainsKey(WordCacheKey.Build(apple)));
        #endregion
    }

    /// <summary>
    /// An empty request is answered with an empty result, without touching the database.
    /// </summary>
    [Fact]
    public async Task GetManyAsync_WithNoKeys_ReturnsEmpty()
    {
        // Act
        var result = await NewCache().GetManyAsync([]);
        
        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// First writer wins, and the check spans the whole alias set: a second write whose aliases overlap an
    /// existing entry — here the shared Traditional writing — leaves the stored analysis untouched instead
    /// of replacing it or failing on the duplicate alias key.
    /// </summary>
    [Fact]
    public async Task SetAsync_WhenAVariantIsAlreadyCached_DoseNotOverwrite()
    {
        #region Arrange
        const string expected = "bicycle", traditionalBike = "腳踏車", simplifiedBike = "脚踏车";
        WordAnalysis first = TestData.Bicycle(),
            second = first with { EnglishMeaning = "bike" };
        #endregion

        #region Act
        // First write wins; the second stored targets an overlapping alias set (same traditional)
        await NewCache().SetAsync(traditionalBike, first);
        await NewCache().SetAsync(simplifiedBike, second);
        var result = await NewCache().GetAsync(WordCacheKey.Build(simplifiedBike));
        #endregion

        #region Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result.EnglishMeaning);
        #endregion
    }
    
    /// <summary>
    /// A fully-populated analysis survives the JSON round-trip intact, nested records included.
    /// The cache stores the analysis as a serialized document, so anything the serializer drops would be
    /// lost silently on every later cache hit.
    /// </summary>
    [Fact]
    public async Task SetAsync_RoundTripsTheFullAnalysisThroughJson()
    {
        var original = TestData.Bicycle();
        await NewCache().SetAsync(WordCacheKey.Build("腳踏車"), original);

        var result = await NewCache().GetAsync(WordCacheKey.Build("腳踏車"));

        Assert.NotNull(result);
        Assert.Equal(original.Pinyin, result!.Pinyin);
        Assert.Equal(original.HanjaReading, result.HanjaReading);
        Assert.Equal(original.Example, result.Example);          // ExampleSentence is a record: value equality
        Assert.Single(result.Characters);
        Assert.Equal(original.Characters.First(), result.Characters.First());  // CharacterAnalysis: value equality
    }

    /// <summary>
    /// Closes the shared connection, which discards the in-memory database with it.
    /// </summary>
    public void Dispose() => _connection.Dispose();
}