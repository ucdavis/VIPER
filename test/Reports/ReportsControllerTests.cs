using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Reports.Controllers;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.test.Reports;

public sealed class ReportsControllerTests
{
    private readonly StubReportService _service = new();
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _controller = new ReportsController(_service);
    }

    [Fact]
    public void Controller_RequiresSignInAndUsesApiConventions()
    {
        PermissionAttribute? permission = typeof(ReportsController).GetCustomAttribute<PermissionAttribute>();
        RouteAttribute? route = typeof(ReportsController).GetCustomAttribute<RouteAttribute>();

        Assert.Equal("SVMSecure", permission?.Allow);
        Assert.Equal("/api/reports", route?.Template);
        Assert.True(typeof(ApiController).IsAssignableFrom(typeof(ReportsController)));
    }

    [Fact]
    public void GetCatalog_ReturnsServiceCatalog()
    {
        IReadOnlyList<ReportCatalogItem> catalog = [new("test.sample", "Sample", "Test", "Description", Available: true)];
        _service.Catalog = catalog;

        ActionResult<IReadOnlyList<ReportCatalogItem>> result = _controller.GetCatalog("Test");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(catalog, ok.Value);
        Assert.Equal("Test", _service.LastArea);
    }

    [Fact]
    public void GetDefinition_Ok_ReturnsMetadata()
    {
        var metadata = new ReportDefinitionMetadata("test.sample", "Sample", "Test", "Description", [], []);
        _service.Definition = ReportOutcome.Ok(metadata);

        ActionResult<ReportDefinitionMetadata> result = _controller.GetDefinition("test.sample");

        Assert.Same(metadata, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public void GetDefinition_NotFound_Returns404()
    {
        _service.Definition = ReportOutcome.NotFound<ReportDefinitionMetadata>();

        ActionResult<ReportDefinitionMetadata> result = _controller.GetDefinition("test.missing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void GetDefinition_Forbidden_Returns403()
    {
        _service.Definition = ReportOutcome.Forbidden<ReportDefinitionMetadata>();

        ActionResult<ReportDefinitionMetadata> result = _controller.GetDefinition("test.sample");

        ObjectResult forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Run_Invalid_Returns400WithErrorsByParameter()
    {
        ReportValidationError[] errors =
        [
            new("startDate", "Start date is required."),
            new(null, "Enter a search or a minimum age."),
        ];
        _service.Run = ReportOutcome.Invalid<ReportResult>(errors);

        ActionResult<ReportResult> result = await _controller.Run("test.sample", null, TestContext.Current.CancellationToken);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(["Start date is required."], problem.Errors["startDate"]);
        Assert.Equal(["Enter a search or a minimum age."], problem.Errors[string.Empty]);
    }

    [Fact]
    public async Task Run_Ok_ReturnsResult()
    {
        var report = new ReportResult("test.sample", "Sample", DateTimeOffset.UnixEpoch, 0, false, [], [], [], [], [], []);
        _service.Run = ReportOutcome.Ok(report);

        ActionResult<ReportResult> result = await _controller.Run("test.sample", null, TestContext.Current.CancellationToken);

        Assert.Same(report, Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("test.sample", _service.LastKey);
    }

    [Fact]
    public async Task Export_Ok_ReturnsUncachedDownload()
    {
        var file = new ReportFile([1, 2, 3], "text/csv", "Sample.csv");
        _service.Export = ReportOutcome.Ok(file);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        ActionResult result = await _controller.Export("test.sample", "csv", null, TestContext.Current.CancellationToken);

        FileContentResult download = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", download.ContentType);
        Assert.Equal("Sample.csv", download.FileDownloadName);
        Assert.Equal([1, 2, 3], download.FileContents);
        Assert.Equal("private, no-store, max-age=0", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("csv", _service.LastFormat);
    }

    [Fact]
    public async Task Export_Invalid_Returns400()
    {
        _service.Export = ReportOutcome.Invalid<ReportFile>([new("format", "Choose an export format: csv.")]);

        ActionResult result = await _controller.Export("test.sample", "docx", null, TestContext.Current.CancellationToken);

        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.Equal(["Choose an export format: csv."], problem.Errors["format"]);
    }

    [Fact]
    public void ToProblemDetails_CombinesMessagesForTheSameParameter()
    {
        ValidationProblemDetails problem = ReportsController.ToProblemDetails(
        [
            new("endDate", "First."),
            new("endDate", "Second."),
        ]);

        Assert.Equal(["First.", "Second."], problem.Errors["endDate"]);
        Assert.Equal("The report parameters are not valid.", problem.Title);
    }

    /// <summary>
    /// Returns canned outcomes and records what the controller asked for.
    /// </summary>
    private sealed class StubReportService : IReportService
    {
        public IReadOnlyList<ReportCatalogItem> Catalog { get; set; } = [];

        public ReportOutcome<ReportDefinitionMetadata> Definition { get; set; } = ReportOutcome.NotFound<ReportDefinitionMetadata>();

        public ReportOutcome<ReportResult> Run { get; set; } = ReportOutcome.NotFound<ReportResult>();

        public ReportOutcome<ReportFile> Export { get; set; } = ReportOutcome.NotFound<ReportFile>();

        public string? LastFormat { get; private set; }

        public string? LastArea { get; private set; }

        public string? LastKey { get; private set; }

        public IReadOnlyList<ReportCatalogItem> GetCatalog(string? area)
        {
            LastArea = area;
            return Catalog;
        }

        public ReportOutcome<ReportDefinitionMetadata> GetDefinition(string? key)
        {
            LastKey = key;
            return Definition;
        }

        public Task<ReportOutcome<ReportResult>> RunAsync(string? key, JsonElement? parameters, CancellationToken ct)
        {
            LastKey = key;
            return Task.FromResult(Run);
        }

        public Task<ReportOutcome<ReportFile>> ExportAsync(
            string? key, string? format, JsonElement? parameters, CancellationToken ct)
        {
            LastKey = key;
            LastFormat = format;
            return Task.FromResult(Export);
        }
    }
}
