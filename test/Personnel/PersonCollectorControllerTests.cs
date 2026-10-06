using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models.PersonCollector;
using Viper.Areas.Personnel.Services;

namespace Viper.test.Personnel;

/// <summary>
/// PersonCollectorController refuses empty or unknown choices before calling the service, and
/// returns the results as JSON or an Excel workbook.
/// </summary>
public sealed class PersonCollectorControllerTests
{
    private static readonly PersonCollectorResult Result = new(
        [new PersonCollectorSection("senate", "Senate Faculty", [])], ShowLoginIds: false, ShowMoreIds: false);

    private readonly IPersonCollectorService _collector = Substitute.For<IPersonCollectorService>();
    private readonly ILogger<PersonCollectorController> _logger = Substitute.For<ILogger<PersonCollectorController>>();
    private readonly PersonCollectorController _controller;

    public PersonCollectorControllerTests()
    {
        _controller = new PersonCollectorController(
            _collector, Substitute.For<IUserHelper>(), _logger)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        _collector.CollectAsync(Arg.Any<PersonCollectorRequest>(), Arg.Any<CancellationToken>()).Returns(Result);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void GetForm_ReturnsTheServiceForm()
    {
        var form = new PersonCollectorForm([], [], [], [], ShowLoginIds: true, ShowMoreIds: false);
        _collector.GetForm().Returns(form);

        Assert.Same(form, Assert.IsType<OkObjectResult>(_controller.GetForm().Result).Value);
    }

    [Fact]
    public async Task GetResults_ReturnsTheCollectedSections()
    {
        ActionResult<PersonCollectorResult> result = await _controller.GetResults(
            new PersonCollectorRequest { SenateEmeriti = true }, Ct);

        Assert.Same(Result, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Requests_WithNothingChosenOrUnknownChoices_AreBadRequests()
    {
        Assert.IsType<BadRequestObjectResult>((await _controller.GetResults(new PersonCollectorRequest(), Ct)).Result);
        Assert.IsType<BadRequestObjectResult>((await _controller.GetResults(null!, Ct)).Result);
        Assert.IsType<BadRequestObjectResult>(
            (await _controller.GetResults(new PersonCollectorRequest { SenateGroups = ["010"] }, Ct)).Result);
        Assert.IsType<BadRequestObjectResult>(await _controller.Export(new PersonCollectorRequest(), Ct));
        await _collector.DidNotReceive().CollectAsync(Arg.Any<PersonCollectorRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_ReturnsAnExcelWorkbookAndIsAudited()
    {
        _logger.IsEnabled(LogLevel.Information).Returns(true);

        ActionResult result = await _controller.Export(new PersonCollectorRequest { StaffMsp = true }, Ct);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Equal("PersonCollector.xlsx", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
        Assert.Contains(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
    }
}
