using System.Net;
using System.Text;
using LunaChinese.Core.Enums;
using LunaChinese.Core.Models.Extraction;
using LunaChinese.Infrastructure.Features.WordsExtraction.Gemini;
using Microsoft.Extensions.Logging;

namespace LunaChinese.Core.Test;

/// <summary>
/// Behavioral tests for <see cref="GeminiAiClient"/>, driven through a fake <see cref="HttpMessageHandler"/>
/// so the request never leaves the process. They pin down how provider HTTP failures are classified and
/// that the provider's error body is logged rather than lost.
/// </summary>
public sealed class GeminiAiClientTests
{
    private static readonly ExtractionImage Image = new("aGVsbG8=", "image/jpeg");

    private readonly RecordingLogger _logger = new();

    /// <summary>
    /// Creates the client under test over a handler that answers every request with
    /// <paramref name="status"/> and <paramref name="body"/>.
    /// </summary>
    /// <param name="status">The HTTP status the fake provider returns.</param>
    /// <param name="body">The response body the fake provider returns.</param>
    /// <returns>A <see cref="GeminiAiClient"/> wired to the fake provider and <see cref="_logger"/>.</returns>
    private GeminiAiClient CreateSut(HttpStatusCode status, string body = "") =>
        new(new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("https://gemini.test/v1beta/") },
            new GeminiExtractionOptions { Model = "gemini-test" },
            _logger);

    /// <summary>
    /// Each provider failure status maps to the error code that tells the caller whether retrying makes sense:
    /// transient provider faults are <see cref="AiOperationErrorCode.ProviderOverloaded"/>, provider-side deadlines
    /// are <see cref="AiOperationErrorCode.Timeout"/>, and request problems such as a bad model name (400/404)
    /// stay <see cref="AiOperationErrorCode.Unknown"/>.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, AiOperationErrorCode.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, AiOperationErrorCode.ProviderOverloaded)]
    [InlineData(HttpStatusCode.BadGateway, AiOperationErrorCode.ProviderOverloaded)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AiOperationErrorCode.ProviderOverloaded)]
    [InlineData((HttpStatusCode)529, AiOperationErrorCode.ProviderOverloaded)]
    [InlineData(HttpStatusCode.RequestTimeout, AiOperationErrorCode.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, AiOperationErrorCode.Timeout)]
    [InlineData(HttpStatusCode.BadRequest, AiOperationErrorCode.Unknown)]
    [InlineData(HttpStatusCode.NotFound, AiOperationErrorCode.Unknown)]
    public async Task ExtractAsync_WhenProviderFails_MapsStatusToErrorCode(
        HttpStatusCode status, AiOperationErrorCode expected)
    {
        #region Arrange
        var sut = CreateSut(status);
        #endregion

        #region Act
        var result = await sut.ExtractAsync(Image, ExtractMode.All);
        #endregion

        #region Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.ErrorCode);
        #endregion
    }

    /// <summary>
    /// A provider failure's body is logged with the status and model, because it holds the explanation the status
    /// alone can't give (e.g. "unexpected model name format"). It must stay out of the result, which reaches API
    /// callers.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_WhenProviderFails_LogsTheErrorBodyButKeepsItOutOfTheResult()
    {
        #region Arrange
        const string providerError = """{"error":{"code":400,"message":"unexpected model name format"}}""";
        var sut = CreateSut(HttpStatusCode.BadRequest, providerError);
        #endregion

        #region Act
        var result = await sut.ExtractAsync(Image, ExtractMode.All);
        #endregion

        #region Assert
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("400", entry.Message);
        Assert.Contains("gemini-test", entry.Message);
        Assert.Contains("unexpected model name format", entry.Message);
        Assert.DoesNotContain("unexpected model name format", result.ErrorMessage);
        #endregion
    }

    /// <summary>
    /// A successful reply is unwrapped from the envelope, trimmed, filtered to entries containing a Han character,
    /// and de-duplicated in order. Nothing is logged on success.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_WhenProviderSucceeds_ReturnsCleanedWords()
    {
        #region Arrange
        const string reply = """
            {"candidates":[{"content":{"parts":[{"text":"{\"words\":[\" 銷售 \",\"abc\",\"彼此\",\"銷售\"]}"}]}}]}
            """;
        var sut = CreateSut(HttpStatusCode.OK, reply);
        #endregion

        #region Act
        var result = await sut.ExtractAsync(Image, ExtractMode.All);
        #endregion

        #region Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["銷售", "彼此"], result.Words);
        Assert.Empty(_logger.Entries);
        #endregion
    }

    /// <summary>
    /// Answers every request with a fixed status and body, standing in for the Gemini API.
    /// </summary>
    /// <param name="status">The status to return.</param>
    /// <param name="body">The JSON body to return.</param>
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    /// <summary>
    /// Captures formatted log entries so tests can assert on what was logged.
    /// </summary>
    private sealed class RecordingLogger : ILogger<GeminiAiClient>
    {
        /// <summary>
        /// The entries logged so far, in order.
        /// </summary>
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
