using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Viper.Areas.CMS.Controllers;
using Viper.Areas.CMS.Services;

namespace Viper.test.CMS;

/// <summary>
/// Controller wiring tests for CMSUserPhotoController: each by-id-type endpoint forwards only
/// its identifier (and the altPhoto flag) to ICmsUserPhotoService and returns the bytes as an
/// image/jpeg file response with a private cache header.
/// </summary>
public sealed class CMSUserPhotoControllerTests
{
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

    [Fact]
    public async Task GetByMailId_ForwardsMailIdOnly()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _photoService.GetUserPhotoAsync("mail@example.com", null, null, false, Arg.Any<CancellationToken>())
            .Returns(bytes);

        var result = await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal(bytes, file.FileContents);
        await _photoService.Received(1).GetUserPhotoAsync("mail@example.com", null, null, false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByLoginId_ForwardsLoginIdAndAltPhotoFlag()
    {
        _photoService.GetUserPhotoAsync(null, "loginX", null, true, Arg.Any<CancellationToken>())
            .Returns(new byte[] { 9 });

        var result = await _controller.GetByLoginId("loginX", altPhoto: true, TestContext.Current.CancellationToken);

        Assert.IsType<FileContentResult>(result);
        await _photoService.Received(1).GetUserPhotoAsync(null, "loginX", null, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIamId_ForwardsIamIdOnly()
    {
        _photoService.GetUserPhotoAsync(null, null, "1000123", false, Arg.Any<CancellationToken>())
            .Returns(new byte[] { 7 });

        var result = await _controller.GetByIamId("1000123", ct: TestContext.Current.CancellationToken);

        Assert.IsType<FileContentResult>(result);
        await _photoService.Received(1).GetUserPhotoAsync(null, null, "1000123", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ServePhoto_SetsPrivateCacheHeader()
    {
        _photoService.GetUserPhotoAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<CancellationToken>()).Returns(new byte[] { 1 });

        await _controller.GetByMailId("mail@example.com", ct: TestContext.Current.CancellationToken);

        Assert.Contains("private", _controller.Response.Headers.CacheControl.ToString());
    }
}
