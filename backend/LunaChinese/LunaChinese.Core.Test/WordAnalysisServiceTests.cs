using LunaChinese.Core.Enums;
using LunaChinese.Core.Features.WordsAnalysis;
using LunaChinese.Core.Models;
using NSubstitute;

namespace LunaChinese.Core.Test;

/// <summary>
/// Behavioral tests for <see cref="WordAnalysisService"/>.
/// The cache and AI client are substituted so each test can isolate one aspect of the orches tration:
/// 
/// </summary>
public class WordAnalysisServiceTests
{
    private readonly IAnalysisCache _cache = Substitute.For<IAnalysisCache>();

    private readonly IWordAnalysisAiClient _aiClient = Substitute.For<IWordAnalysisAiClient>();

    /// <summary>
    /// Create a system under test.
    /// </summary>
    /// <returns></returns>
    private WordAnalysisService CreateSut() => new(_cache, _aiClient);

    private void GivenCached(params string[] cachedWords)
    {
        var hits = cachedWords.ToDictionary(word => word, TestData.Analysis);

        // Behavior to execute when it is called during test, accept any passed parameters
        _cache.GetManyAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                // Get the arguments passed to the mock
                var keys = call.Arg<IReadOnlyCollection<string>>();

                IReadOnlyDictionary<string, WordAnalysis> found =
                    keys.Where(hits.ContainsKey).ToDictionary(key => key, key => hits[key]);

                return Task.FromResult(found);
            });
    }

    private void GivenAiSucceedsForAll()
    {
        // Accept any passed parameters
        _aiClient.AnalyzeAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var words = call.Arg<IReadOnlyCollection<string>>();

                // Assume all analysis succeed
                IReadOnlyList<WordAnalysisItemResult> results =
                    words.Select(word => WordAnalysisItemResult.Success(word, TestData.Analysis(word)))
                        .ToList();

                return Task.FromResult(results);
            });
    }

    [Fact]
    public async Task AnalyzeBatchAsync_WithNoUsableWords_ReturnEmptyResultWithoutTouchingCacheOrAi()
    {
        #region Arrange

        var sut = CreateSut();

        #endregion

        #region Act

        var result = await sut.AnalyzeBatchAsync(TestData.Command("  ", string.Empty, "\t"));

        #endregion

        #region Assert

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        // The GetManyAsync and AnalyzeAsync method must not be called at all
        await _cache.DidNotReceive()
            .GetManyAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        await _aiClient.DidNotReceive()
            .AnalyzeAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());

        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_TrimsBlanksAndCollapsesDuplicates()
    {
        #region Arrange
        GivenCached(); // everything is a miss
        GivenAiSucceedsForAll();

        var sut = CreateSut();
        string[] expected = ["腳踏車", "蘋果"];
        #endregion

        #region Act
        var result = await sut.AnalyzeBatchAsync(TestData.Command(
            "腳踏車", " 腳踏車 ", "蘋果", string.Empty, string.Empty));
        #endregion

        #region Assert
        // Verify that blank entries are removed, duplicates are collapsed,
        // and surrounding whitespace is trimmed. 
        Assert.Equal(expected, result.Items.Select(item => item.RequestedWord));

        // Verify that AnalyzeAsync is called exactly once with the normalized,
        // unique words in their original order.
        await _aiClient.Received(1)
            .AnalyzeAsync(Arg.Is<IReadOnlyCollection<string>>(
                    words => words.SequenceEqual(expected)), 
                Arg.Any<CancellationToken>());
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_WhenAllWordCached_DoesNotCallAi()
    {
        #region Arrange
        GivenCached("腳踏車", "蘋果");
        var sut = CreateSut();
        #endregion

        #region Act
        var result = await sut.AnalyzeBatchAsync(TestData.Command("腳踏車", "蘋果"));
        #endregion

        #region Assert
        Assert.True(result.IsFullySuccessful);
        Assert.Equal(2, result.SuccessCount);

        await _aiClient.DidNotReceive()
            .AnalyzeAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_ReportsCacheHitsWithFromCacheTrue()
    {
        #region Arrange
        const string word = "腳踏車";
        // Configure the cache so the requested word is returned as a cache hit
        GivenCached(word);
        // Records every progress update reported by AnalyzeBatchAsync
        RecordingProgress progress = new();
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze a single cached word and capture the reported progress updates.
        await sut.AnalyzeBatchAsync(TestData.Command(word), progress);
        #endregion

        #region Assert
        // Verify that exactly one progress update is reported.
        // Assert.Equal(1, progress.Updates.Count);
        // var update = progress.Updates[0];
        var update = Assert.Single(progress.Updates);
        
        // Verify that the progress update belongs to the requested word.
        Assert.Equal(word, update.Word);
        
        // Verify that the service reports the result as coming from the cache.
        Assert.True(update.FromCache);
        
        // Verify that the single word has been completed.
        Assert.Equal(1, update.CompletedCount);
        
        // Verify that the batch contains exactly one word.
        Assert.Equal(1, update.TotalCount);
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_SendsOnlyUncachedWordsToAi()
    {
        #region Arrange
        const string bike = "腳踏車", apple = "蘋果";
        // Configure the cache so bike is a hit and apple is a miss.
        GivenCached(bike);
        var sut = CreateSut();
        string[] expected = [apple];
        #endregion

        #region Act
        // Analyze one cached word and one uncached word.
        await sut.AnalyzeBatchAsync(TestData.Command(bike, apple));
        #endregion

        #region Assert
        // Verify that the AI client is called exactly once and receives only the uncached word.
        // Match on the contents rather than the instance: the service passes the chunked array,
        // which never equals a separately created collection.
        await _aiClient.Received(1).AnalyzeAsync(
            Arg.Is<IReadOnlyCollection<string>>(words => words.SequenceEqual(expected)),
            Arg.Any<CancellationToken>());
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_ReportsFreshResultWithFromCatchFalse()
    {
        #region Arrange
        const string word = "蘋果";
        // Configure the cache so every requested word is a cache miss.
        GivenCached();
        // Configure the AI client to successfully analyze all uncached words.
        GivenAiSucceedsForAll();
        RecordingProgress progress = new();
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze an uncached word adn capture its progress update.
        await sut.AnalyzeBatchAsync(TestData.Command(word), progress);
        #endregion

        #region Assert
        // Verify that exactly one progress update is reported
        var update = Assert.Single(progress.Updates);
        // Verify that the progress update belongs to the requested word
        Assert.Equal(word, update.Word);
        // Verify that the result is reported as a fresh AI result, rather than a cached result.
        Assert.False(update.FromCache);
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_CachesFreshSuccesses()
    {
        #region Arrange
        const string word = "蘋果";
        GivenCached();
        GivenAiSucceedsForAll();
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze an uncached word so that a fresh successful result is produced.
        await sut.AnalyzeBatchAsync(TestData.Command(word));
        #endregion

        #region Assert
        // Verify that the fresh successful analysis is written to the cache exactly once.
        await _cache.Received(1).SetAsync(word,
            Arg.Is<WordAnalysis>(analysis => analysis.Word == word),
            Arg.Any<CancellationToken>());
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_DoesNotCacheFailures()
    {
        #region Arrange
        const string word = "蘋果";
        GivenCached();
        // Configure the AI client to return a failed result for every requested word.
        _aiClient.AnalyzeAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var words = call.Arg<IReadOnlyCollection<string>>();
                IReadOnlyList<WordAnalysisItemResult> results = words
                    .Select(requestedWord => WordAnalysisItemResult.Failure(
                        requestedWord,
                        AiOperationErrorCode.InvalidResponse, "Bad response"))
                    .ToList();

                return Task.FromResult(results);
            });

        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze an uncached word whose AI analysis is configured to fail.
        var result = await sut.AnalyzeBatchAsync(TestData.Command(word));
        #endregion

        #region Assert
        
        var item = Assert.Single(result.Items);
        Assert.Equal(word, item.RequestedWord);
        
        // Verify that the failed AI result is returned as a failure.
        Assert.False(result.Items.Single().IsSuccess);
        // Verify that failed analysis results are never written to the cache.
        await _cache.DidNotReceive()
            .SetAsync(
                Arg.Any<string>(),
                Arg.Any<WordAnalysis>(),
                Arg.Any<CancellationToken>());
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_ChunksCacheMissesIntoBatchesOfTen()
    {
        #region Arrange
        GivenCached();
        // Record the number of words sent in each AI request
        List<int> batchSizes = [];
        _aiClient.AnalyzeAsync(
                // Configure the AI client to record each batch size.
                Arg.Do<IReadOnlyCollection<string>>(words =>
                    batchSizes.Add(words.Count)),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var words = call.Arg<IReadOnlyCollection<string>>();
                // Assume that all analyses succeed
                IReadOnlyList<WordAnalysisItemResult> results = [.. words.Select(word => 
                        WordAnalysisItemResult.Success(word, TestData.Analysis(word)))];

                return Task.FromResult(results);
            });
        // Create 25 unique words so that expected AI batches are 10, 10 and 5
        string[] words = [.. Enumerable.Range(0, 25).Select(i => $"詞{i + 1}")];
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze all 25 uncached words.
        await sut.AnalyzeBatchAsync(TestData.Command(words));
        #endregion

        #region Assert
        // Verify that cache misses are split into AI batches of at most 10 words.
        Assert.Equal([10, 10, 5], batchSizes);
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_WhenClientOmitsARequestedWord_ReturnsSynthesizedUnknownFailure()
    {
        #region Arrange

        const string bike = "腳踏車", apple = "蘋果";
        GivenCached();
        // The client answers for bike but silently drops apple  
        _aiClient.AnalyzeAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                IReadOnlyList<WordAnalysisItemResult> results =
                [
                    WordAnalysisItemResult.Success(bike, TestData.Analysis(bike))
                ];

                return Task.FromResult(results);
            });
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze two uncached words even though the AI client is configured to return a result for only one of then.
        var result = await sut.AnalyzeBatchAsync(TestData.Command(bike, apple));
        #endregion

        #region Assert
        // Verify that the omitted requested word is still represented in the final batch result
        var missingWordResult = result.Items.Single(item => item.RequestedWord == apple);
        // Verify that the synthesized result is reported as a failure.
        Assert.False(missingWordResult.IsSuccess);
        // Verify that an omitted AI result classified as an unknown failure.
        Assert.Equal(AiOperationErrorCode.Unknown, missingWordResult.ErrorCode);
        #endregion
    }

    /// <summary>
    /// "B" is a cache hit; "A" and "C" go through the AI. 
    /// The output must still follow the original request order 
    /// regardless of which produced each result.
    /// </summary>
    [Fact]
    public async Task AnalyzeBatchAsync_PreservesOriginalRequestOrder()
    {
        #region Arrange
        const string a = "A", b = "B", c = "C";
        GivenCached(b);
        GivenAiSucceedsForAll();
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze the words in the original request order: A, B, C
        var result = await sut.AnalyzeBatchAsync(TestData.Command(a, b, c));
        #endregion

        #region Assert
        // Verify that the final batch result preserves the original request order 
        // even though the results come from different source 
        Assert.Equal([a, b, c], result.Items.Select(item => item.RequestedWord));
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_WithMixedOutcomes_ReportsPartialSuccess()
    {
        #region Arrange

        const string success = "成功", failure = "失敗";
        GivenCached();
        // Configure the AI client to return one successful result and one timeout failure.
        _aiClient.AnalyzeAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                IReadOnlyList<WordAnalysisItemResult> results =
                [
                    WordAnalysisItemResult.Success(success, TestData.Analysis(success)),
                    WordAnalysisItemResult.Failure(failure, AiOperationErrorCode.Timeout, 
                        nameof(AiOperationErrorCode.Timeout).ToLower())
                ];

                return Task.FromResult(results);
            });
        var sut = CreateSut();
        #endregion

        #region Act
        // Analyze a batch containing one successful word and one failed word.
        var result = await sut.AnalyzeBatchAsync(TestData.Command(success, failure));
        #endregion

        #region Assert
        // Verify that the batch reports exactly one successful result
        Assert.Equal(1, result.SuccessCount);
        
        // Verify that the batch reports exactly one failed result
        Assert.Equal(1, result.FailureCount);
        
        // Verify that mixed success and failure outcomes are reported as a partially successful batch
        Assert.True(result.IsPartiallySuccessful);
        
        // Verify that a batch containing any failure is not fully successful.
        Assert.False(result.IsFullySuccessful);
        #endregion
    }

    [Fact]
    public async Task AnalyzeBatchAsync_WhenAlreadyCanceledWithMisses_ThrowsAndSkipsAi()
    {
        #region Arrange
        // All miss, so the batch loop runs and hits the cancellation check
        GivenCached();

        // Create a cancellation token already canceled
        // before AnalyzeBatchAsync starts processing the cache miss.
        using var context = new CancellationTokenSource();
        await context.CancelAsync();
        var sut = CreateSut();
        #endregion

        #region Act & Assert
        // Verify that processing stops with a cancellation exception.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.AnalyzeBatchAsync(
            TestData.Command("蘋果"), cancellationToken: context.Token));
        
        // Verify that the canceled operation stops before the AI client is called.
        await _aiClient.DidNotReceive()
            .AnalyzeAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>());
        #endregion
    }
    
    /// <summary>
    /// Records progress synchronously. A plain <see cref="Progress{T}"/> marshals callbacks,
    /// through the captured synchronization context, which would 
    /// </summary>
    private sealed class RecordingProgress : IProgress<WordAnalysisProgress>
    {
        public List<WordAnalysisProgress> Updates { get; } = [];

        public void Report(WordAnalysisProgress value) => Updates.Add(value);
    }
}