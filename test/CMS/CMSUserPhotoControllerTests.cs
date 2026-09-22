using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Viper.Areas.CMS.Controllers;
using Viper.Areas.CMS.Services;

namespace Viper.test.CMS;

/// <summary>
/// Controller wiring tests for CMSUserPhotoController: each by-id-type endpoint forwards only
/// its identifier to ICmsUserPhotoService, routing to GetUserPhotoAsync when altPhoto is false
/// and GetAlternatePhotoAsync when altPhoto is true, and returns the bytes as an image/jpeg file
/// response with a private cache header; conditional requests (If-Modified-Since) short-circuit
/// to 304 per FIX 4. A null result from GetAlternatePhotoAsync (no alternate photo for this
/// person) returns 404 rather than falling back to another image.
/// </summary>
public sealed class CMSUserPhotoControllerTests
{
    private static readonly DateTimeOffset PhotoLastModified =
        new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ICmsUserPhotoService _photoService;
    private readonly CMSUserPhotoController _controller;

    public CMSUserPhotoControllerTests()
    {
        _photoService = Substitute.For<ICmsUserPhotoService>();
        _controller = new CMSUserPhotoController(_photoService);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };
    }

    private static CmsUserPhotoResult Result(byte[] bytes) => new(bytes, PhotoLastModified);

    [Fact]
    public async Task GetByMailId_ForwardsMailIdOnly()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _photoService.GetUserPhotoAsync("mail@example.com", null, null, null, Arg.Any<CancellationToken>())
            .Returns(Result(bytes));

        var result = await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal(bytes, file.FileContents);
        await _photoService.Received(1).GetUserPhotoAsync("mail@example.com", null, null, null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByLoginId_AltPhoto_CallsGetAlternatePhotoWithLoginId()
    {
        _photoService.GetAlternatePhotoAsync(null, "loginX", null, null, Arg.Any<CancellationToken>())
            .Returns(Result(new byte[] { 9 }));

        var result = await _controller.GetByLoginId("loginX", altPhoto: true, TestContext.Current.CancellationToken);

        Assert.IsType<FileContentResult>(result);
        await _photoService.Received(1).GetAlternatePhotoAsync(null, "loginX", null, null, Arg.Any<CancellationToken>());
        await _photoService.DidNotReceive().GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIamId_ForwardsIamIdOnly()
    {
        _photoService.GetUserPhotoAsync(null, null, "1000123", null, Arg.Any<CancellationToken>())
            .Returns(Result(new byte[] { 7 }));

        var result = await _controller.GetByIamId("1000123", ct: TestContext.Current.CancellationToken);

        Assert.IsType<FileContentResult>(result);
        await _photoService.Received(1).GetUserPhotoAsync(null, null, "1000123", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByMothraId_ForwardsMothraIdOnly()
    {
        _photoService.GetUserPhotoAsync(null, null, null, "m-9001", Arg.Any<CancellationToken>())
            .Returns(Result(new byte[] { 5 }));

        var result = await _controller.GetByMothraId("m-9001", ct: TestContext.Current.CancellationToken);

        Assert.IsType<FileContentResult>(result);
        await _photoService.Received(1).GetUserPhotoAsync(null, null, null, "m-9001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByMailId_AltPhotoMissing_ReturnsNotFound()
    {
        _photoService.GetAlternatePhotoAsync("mail@example.com", null, null, null, Arg.Any<CancellationToken>())
            .Returns((CmsUserPhotoResult?)null);

        var result = await _controller.GetByMailId("mail@example.com", altPhoto: true,
            ct: TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ServePhoto_SetsPrivateCacheHeader()
    {
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Result(new byte[] { 1 }));

        await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var cacheControl = _controller.Response.Headers.CacheControl.ToString();
        Assert.Contains("private", cacheControl);
        Assert.Contains("stale-while-revalidate", cacheControl);
    }

    [Fact]
    public async Task ServePhoto_SetsLastModifiedHeader()
    {
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Result(new byte[] { 1 }));

        await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        Assert.Equal(PhotoLastModified, _controller.Response.GetTypedHeaders().LastModified);
    }

    [Fact]
    public async Task ServePhoto_ReturnsNotModified_WhenIfModifiedSinceCoversLastModified()
    {
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Result(new byte[] { 1, 2, 3 }));
        _controller.Request.GetTypedHeaders().IfModifiedSince = PhotoLastModified;

        var result = await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Fact]
    public async Task ServePhoto_ReturnsNotModified_WhenIfModifiedSinceIsAfterLastModified()
    {
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Result(new byte[] { 1, 2, 3 }));
        _controller.Request.GetTypedHeaders().IfModifiedSince = PhotoLastModified.AddDays(1);

        var result = await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Fact]
    public async Task ServePhoto_ReturnsFile_WhenIfModifiedSinceIsBeforeLastModified()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Result(bytes));
        _controller.Request.GetTypedHeaders().IfModifiedSince = PhotoLastModified.AddDays(-1);

        var result = await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(bytes, file.FileContents);
    }

    [Fact]
    public async Task ServePhoto_ReturnsFile_WhenNoIfModifiedSinceHeaderSent()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Result(bytes));

        var result = await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(bytes, file.FileContents);
    }
}
