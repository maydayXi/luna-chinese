using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LunaChinese.Core.Enums;
using LunaChinese.Core.Features.WordsExtraction;
using LunaChinese.Core.Models.Extraction;
using LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Request;
using LunaChinese.Infrastructure.Features.WordsExtraction.Gemini.Response;
using Microsoft.Extensions.Logging;

namespace LunaChinese.Infrastructure.Features.WordsExtraction.Gemini;

/// <summary>
/// An <see cref="IWordExtractionAiClient"/> backed by Google's Gemini <c>generateContent</c> API.
/// It sends the page image together with an extraction instruction, constrains the reply to a
/// <c>{ "word": [...] }</c> JSON schema, then parse, filters, and de-duplicates the words. 
/// </summary>
/// <param name="httpClient"> The typed client pointing at the Gemini API.</param>
/// <param name="options"> The per-environment settings, notably the model name. </param>
/// <param name="logger"> Records the provider's error body, which is kept out of the caller-facing result. </param>
/// <remarks>
/// The typed <paramref name="httpClient"/> is expected to carry the Gemini base address and the
/// <c>x-goog-api-key</c> header, both configured at registration time;
/// this class owns only the request shape, response parsing, and the mapping from failure to <see cref="AiOperationErrorCode"/>.
/// </remarks>
public sealed class GeminiAiClient(
    HttpClient httpClient,
    GeminiExtractionOptions options,
    ILogger<GeminiAiClient> logger) : IWordExtractionAiClient
{
    /// <summary>
    /// The most of a provider error body written to the log; Gemini's errors are short JSON documents,
    /// so this only guards against an unexpectedly large body (e.g. an HTML error page from a proxy).
    /// </summary>
    private const int MaxLoggedErrorBodyLength = 2000;

    /// <summary>
    /// Web defaults (camel-cased names) so the PascalCase DTOs serialize to the field names the Gemini API expects,
    /// and its camelCase response deserializes back without per-property attributes. 
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The instruction paired with the image in <see cref="ExtractMode.All"/>;
    /// returns only the numbered headwords printed in the textbook's vocabulary list,
    /// ignoring handwritten notes, example sentences, and annotations.
    /// </summary>
    private const string AllModelPrompt = """
        這是一張中文課本生詞表的照片。請只取出書上「印刷」且「有編號」的生詞詞條本身。

        規則：
        1. 只取每個編號（例如 7.、8.、9.）後面的詞條，每個編號對應一個詞。
        2. 不要包含任何手寫內容（筆記、補充詞、圈選或旁註），即使字跡清楚也一律忽略。
        3. 不要包含例句、拼音、詞性、英文解釋，或例句中出現的其他詞。
        4. 詞條同時印有繁體與簡體時，只取繁體（編號旁的主要寫法）。
        5. 依編號順序輸出，不要重複。

        將結果放在 words 陣列中回傳。
        """;

    public async Task<WordExtractionResult> ExtractAsync(ExtractionImage image, ExtractMode mode,
        CancellationToken cancellationToken = default)
    {
        // This client implements the "all" mode only
        if (mode != ExtractMode.All)
            throw new NotSupportedException(
                $"{nameof(GeminiAiClient)} currently supports only {nameof(ExtractMode.All)}.");

        var request = BuildRequest(image);
        var uri = $"models/{options.Model}:generateContent";

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(uri, request, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller cancelled; surface it rather than reclassifying it as a provider failure.
            throw;
        }
        catch (OperationCanceledException)
        {
            return WordExtractionResult.Failure(
                AiOperationErrorCode.Timeout, "The extraction request timed out.");
        }
        catch (HttpRequestException exception)
        {
            return WordExtractionResult.Failure(AiOperationErrorCode.Unknown, exception.Message);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // The body explains failures the status alone can't (an invalid model name, a bad API key).
                // Log it for diagnosis, but keep it out of the result, which reaches API callers.
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogWarning("Gemini returned HTTP {StatusCode} for model {Model}: {Body}",
                    (int)response.StatusCode, options.Model,
                    body.Length > MaxLoggedErrorBodyLength ? body[..MaxLoggedErrorBodyLength] : body);

                var (code, message) = MapFailureStatus(response.StatusCode);
                return WordExtractionResult.Failure(code, message);
            }

            GeminiGenerateContentResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>
                    (SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (JsonException)
            {
                return WordExtractionResult.Failure(AiOperationErrorCode.InvalidResponse,
                    "The provider response body was not valid JSON.");
            }

            return ParseWord(payload);
        }
    }

    /// <summary>
    /// Performs the two-stage parse - unwrap the response envelope, then deserialize the JSON text
    /// carried inside the first candidate part - and returns the cleaned word list.
    /// </summary>
    /// <param name="response">
    /// The deserialized response envelope, possibly incomplete.
    /// </param>
    /// <returns>
    /// A success carrying the filtered, de-duplicated words (possibly empty), or an <see cref="AiOperationErrorCode.InvalidResponse"/> 
    /// failure when the shape is unusable.
    /// </returns>
    private static WordExtractionResult ParseWord(GeminiGenerateContentResponse? response)
    {
        // A prompt blocked before generation carries its reason here rather than in candidates.
        if (response?.PromptFeedback?.BlockReason is { } blockReason)
            return WordExtractionResult.Failure(AiOperationErrorCode.InvalidResponse,
                $"The prompt was blocked by the provider: {blockReason}");

        var payloadText = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(payloadText))
            return WordExtractionResult.Failure(AiOperationErrorCode.InvalidResponse,
                "The provider response contained no content to parse.");

        GeminiExtractedWords? results;

        try
        {
            results = JsonSerializer.Deserialize<GeminiExtractedWords>(payloadText, SerializerOptions);
        }
        catch (JsonException)
        {
            return WordExtractionResult.Failure(AiOperationErrorCode.InvalidResponse,
                "The provider payload was not valid JSON.");
        }

        List<string> words =
        [
            .. 
            (results?.Words ?? [])
            .Select(word => word.Trim())
            .Where(word => word.Length > 0 && ContainsHan(word))
            .Distinct()
        ];

        // An empty list is a valid success: the image was read but held no eligible words.
        return WordExtractionResult.Success(words);
    }

    /// <summary>
    /// Reports whether the text contains at lease one CJK Unified Ideograph (U+4E00-U+9FFF)
    /// </summary>
    /// <param name="word">The candidate word</param>
    /// <returns> <see langword="true"/> if it contains a Han character. </returns>
    private static bool ContainsHan(string word) =>
        word.Any(character => character is >= '\u4e00' and <= '\u9fff');

    /// <summary>
    /// Maps a non-success HTTP status to the matching <see cref="AiOperationErrorCode"/> and a message.
    /// Transient provider faults (500/502/503/529) map to <see cref="AiOperationErrorCode.ProviderOverloaded"/>
    /// and provider-side deadlines (408/504) to <see cref="AiOperationErrorCode.Timeout"/>, so callers can tell a
    /// retryable provider fault from a request problem, which stays <see cref="AiOperationErrorCode.Unknown"/>.
    /// </summary>
    /// <param name="statusCode">The status returned by the provider.</param>
    /// <returns> The classified error and a human-readable description. </returns>
    private static (AiOperationErrorCode code, string message) MapFailureStatus(HttpStatusCode statusCode) =>
        (int)statusCode switch
        {
            (int)HttpStatusCode.TooManyRequests => (
                AiOperationErrorCode.RateLimited,
                "The provider rate limit was exceeded."),
            (int)HttpStatusCode.InternalServerError or (int)HttpStatusCode.BadGateway
                or (int)HttpStatusCode.ServiceUnavailable or 529 => (
                AiOperationErrorCode.ProviderOverloaded,
                "The provider was temporarily unavailable."),
            (int)HttpStatusCode.RequestTimeout or (int)HttpStatusCode.GatewayTimeout => (
                AiOperationErrorCode.Timeout,
                "The provider timed out."),
            _ => (AiOperationErrorCode.Unknown, $"The provider returned HTTP status {(int)statusCode}")
        };

    /// <summary>
    /// Assembly the request body: one user turn carrying the instruction text and the image,
    /// with a generation config that forces a schema-constrained JSON reply.
    /// </summary>
    /// <param name="image">The page image extract from.</param>
    /// <returns> The request ready to serialize and post. </returns>
    private static GeminiGenerateContentRequest BuildRequest(ExtractionImage image) => new(
        Contents:
        [
            new GeminiContent([
                GeminiPart.FromText(AllModelPrompt),
                GeminiPart.FromImage(image.MediaType, image.Base64)
            ])
        ],
        GenerationConfig: new GeminiGenerationConfig(
            ResponseMimeType: "application/json",
            ResponseSchema: GeminiExtractionSchema.Build())
    );
}