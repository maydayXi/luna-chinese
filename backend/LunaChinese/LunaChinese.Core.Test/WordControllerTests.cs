using LunaChinese.API.Controllers;
using LunaChinese.Core.Enums;
using LunaChinese.Core.Features.WordsExtraction;
using LunaChinese.Core.Models.Extraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LunaChinese.Core.Test;

/// <summary>
/// Tests for <see cref="WordController"/>'s upload validation. A bad upload must be rejected with a client error
/// before it is buffered or sent to the provider, rather than failing at the provider and surfacing as a 500.
/// </summary>
public sealed class WordControllerTests
{
    private const long TenMegabytes = 10 * 1024 * 1024;

    private readonly IWordExtractionAiClient _client = Substitute.For<IWordExtractionAiClient>();

    /// <summary>
    /// Creates the controller under test with a real <see cref="ProblemDetailsFactory"/>, which
    /// <see cref="ControllerBase.Problem(string?, string?, int?, string?, string?)"/> needs outside the MVC pipeline.
    /// </summary>
    /// <returns>A <see cref="WordController"/> wired to <see cref="_client"/>.</returns>
    private WordController CreateSut() => new(_client)
    {
        ProblemDetailsFactory = new ServiceCollection()
            .AddLogging()
            .AddMvcCore().Services
            .BuildServiceProvider()
            .GetRequiredService<ProblemDetailsFactory>(),
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    /// <summary>
    /// Builds an upload that declares <paramref name="length"/> bytes and <paramref name="contentType"/>.
    /// The validation reads only the declared metadata, so the content itself can be tiny.
    /// </summary>
    private static FormFile Upload(long length, string contentType) =>
        new(new MemoryStream([1, 2, 3]), 0, length, "image", "page")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    /// <summary>
    /// An upload over the size limit is rejected with 413 without ever reaching the provider.
    /// </summary>
    [Fact]
    public async Task ExtractAsync_WhenImageIsTooLarge_Returns413WithoutCallingTheProvider()
    {
        #region Arrange
        var sut = CreateSut();
        #endregion

        #region Act
        var response = await sut.ExtraAsync(Upload(TenMegabytes + 1, "image/jpeg"), ExtractMode.All,
            CancellationToken.None);
        #endregion

        #region Assert
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<ObjectResult>(response).StatusCode);
        await _client.DidNotReceiveWithAnyArgs().ExtractAsync(default!, default);
        #endregion
    }

    /// <summary>
    /// A non-image (or unsupported image) content type is rejected with 415 without reaching the provider,
    /// including a missing content type, which is no longer assumed to be JPEG.
    /// </summary>
    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/gif")]
    [InlineData("")]
    public async Task ExtractAsync_WhenContentTypeIsUnsupported_Returns415WithoutCallingTheProvider(
        string contentType)
    {
        #region Arrange
        var sut = CreateSut();
        #endregion

        #region Act
        var response = await sut.ExtraAsync(Upload(3, contentType), ExtractMode.All, CancellationToken.None);
        #endregion

        #region Assert
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, Assert.IsType<ObjectResult>(response).StatusCode);
        await _client.DidNotReceiveWithAnyArgs().ExtractAsync(default!, default);
        #endregion
    }

    /// <summary>
    /// A supported image at exactly the size limit passes validation and reaches the provider with its media type,
    /// normalized to lowercase without parameters (e.g. <c>; charset=...</c>).
    /// </summary>
    [Fact]
    public async Task ExtractAsync_WhenImageIsValid_PassesItToTheProvider()
    {
        #region Arrange
        _client.ExtractAsync(Arg.Any<ExtractionImage>(), Arg.Any<ExtractMode>(), Arg.Any<CancellationToken>())
            .Returns(WordExtractionResult.Success(["銷售"]));
        var sut = CreateSut();
        #endregion

        #region Act
        var response = await sut.ExtraAsync(Upload(TenMegabytes, "IMAGE/PNG; foo=bar"), ExtractMode.All,
            CancellationToken.None);
        #endregion

        #region Assert
        Assert.IsType<OkObjectResult>(response);
        await _client.Received(1).ExtractAsync(
            Arg.Is<ExtractionImage>(image => image.MediaType == "image/png"),
            ExtractMode.All, Arg.Any<CancellationToken>());
        #endregion
    }
}
