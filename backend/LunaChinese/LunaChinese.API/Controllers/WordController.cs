using System.Net.Mime;
using LunaChinese.API.Contracts;
using LunaChinese.Core.Enums;
using LunaChinese.Core.Features.WordsExtraction;
using LunaChinese.Core.Models.Extraction;
using Microsoft.AspNetCore.Mvc;

namespace LunaChinese.API.Controllers;

/// <summary>
/// Endpoints for working with Chinese words - currently extract then from uploaded page image.
/// </summary>
/// <param name="extractionAiClient"></param>
[ApiController, Route("api/[controller]")]
public sealed class WordController(IWordExtractionAiClient extractionAiClient) : ControllerBase
{
    /// <summary>
    /// Extracts Chinese words from an upload page image.
    /// </summary>
    /// <param name="image"> The upload page image. </param>
    /// <param name="mode"> Which words to extract, default to all vocabulary on the page. </param>
    /// <param name="cancellationToken"> A token tied to the result lifetime. </param>
    /// <returns>200 with the extracted words on success, or mapped error status on failure. </returns>
    [HttpPost("extract")]
    [ProducesResponseType<ExtractWordsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> ExtraAsync(IFormFile? image, [FromForm] ExtractMode mode,
        CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new
            {
                error = "An image file is required!"
            });

        // Read the upload as raw bytes at the boundary, and Base64-encode for the Core contract. 
        using var stream = new MemoryStream();
        await image.CopyToAsync(stream, cancellationToken);
        string base64 = Convert.ToBase64String(stream.ToArray()),
            mediaType = string.IsNullOrWhiteSpace(image.ContentType)
                ? MediaTypeNames.Image.Jpeg
                : image.ContentType;

        WordExtractionResult result;
        try
        {
            result = await extractionAiClient.ExtractAsync(new ExtractionImage(base64, mediaType),
                mode, cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            // A valid mode the configured extractor doesn't implement yet (e.g. MarkedOnly) - not a server fault.
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status501NotImplemented);
        }

        return result.IsSuccess
            ? Ok(new ExtractWordsResponse(result.Words!))
            : MapFailure(result);
    }

    /// <summary>
    /// Maps an extraction failure's <see cref="AiOperationErrorCode"/> to the HTTP status that best convey it to the caller
    /// Wrapped as a problem response carrying the error message. 
    /// </summary>
    /// <param name="result"> The failure result. </param>
    /// <returns> A problem result with the mapped status code. </returns>
    private ObjectResult MapFailure(WordExtractionResult result)
    {
        var statusCodes = result.ErrorCode switch
        {
            AiOperationErrorCode.RateLimited => StatusCodes.Status429TooManyRequests,
            AiOperationErrorCode.ProviderOverloaded => StatusCodes.Status503ServiceUnavailable,
            AiOperationErrorCode.Timeout => StatusCodes.Status504GatewayTimeout,
            AiOperationErrorCode.InvalidResponse => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: result.ErrorMessage, statusCode: statusCodes);
    }
}