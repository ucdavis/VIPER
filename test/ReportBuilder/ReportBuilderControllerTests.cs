using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ReportBuilder.Controllers;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.test.ReportBuilder;

/// <summary>The controller passes requests to the services and turns their outcomes into responses.</summary>
public sealed class ReportBuilderControllerTests
{
    private const string Slug = "8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e";
    private readonly StubServices _services = new();
    private readonly ReportBuilderController _controller;

    public ReportBuilderControllerTests()
    {
        _controller = new ReportBuilderController(_services, _services, _services)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ReportRunResult EmptyResult()
    {
        return new ReportRunResult { Columns = [], Rows = [], Page = 1, PerPage = 25, TotalRows = 0, Truncated = false };
    }

    [Fact]
    public void Controller_RequiresSignInAndUsesApiConventions()
    {
        Assert.Equal("SVMSecure", typeof(ReportBuilderController).GetCustomAttribute<PermissionAttribute>()?.Allow);
        Assert.Equal("/api/report-builder", typeof(ReportBuilderController).GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.True(typeof(ApiController).IsAssignableFrom(typeof(ReportBuilderController)));
    }

    [Fact]
    public async Task GetCatalog_ReturnsTheUsersReports()
    {
        IReadOnlyList<ReportCatalogItem> catalog = [new ReportCatalogItem { Slug = Guid.Parse(Slug), Title = "Roster" }];
        _services.Catalog = catalog;

        ActionResult<IReadOnlyList<ReportCatalogItem>> result = await _controller.GetCatalog(Ct);

        Assert.Same(catalog, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetDefinition_Ok_ReturnsTheDefinition()
    {
        var definition = new ReportViewerDefinition
        {
            Slug = Guid.Parse(Slug),
            Title = "Roster",
            Paginate = true,
            PageSize = 25,
            AllowExcel = true,
            AllowPdf = false,
            SortDescending = false,
            Columns = [],
            Parameters = [],
        };
        _services.Definition = new ReportDefinitionOutcome { Status = ReportRunStatus.Ok, Definition = definition };

        ActionResult<ReportViewerDefinition> result = await _controller.GetDefinition(Slug, Ct);

        Assert.Same(definition, Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(Slug, _services.LastSlug);
    }

    [Fact]
    public async Task GetDefinition_NotFound_Returns404()
    {
        _services.Definition = new ReportDefinitionOutcome { Status = ReportRunStatus.NotFound };

        ActionResult<ReportViewerDefinition> result = await _controller.GetDefinition(Slug, Ct);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Run_Ok_ReturnsTheRowsInScreenMode()
    {
        ReportRunResult rows = EmptyResult();
        _services.Run = new ReportRunOutcome { Status = ReportRunStatus.Ok, Result = rows };
        var request = new ReportRunRequest { Page = 2 };

        ActionResult<ReportRunResult> result = await _controller.Run(Slug, request, Ct);

        Assert.Same(rows, Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Same(request, _services.LastRequest);
        Assert.Equal(ReportRunMode.Screen, _services.LastMode);
    }

    [Fact]
    public async Task Run_Forbidden_Returns403()
    {
        _services.Run = new ReportRunOutcome { Status = ReportRunStatus.Forbidden };

        ActionResult<ReportRunResult> result = await _controller.Run(Slug, new ReportRunRequest(), Ct);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task Run_Invalid_Returns400WithTheErrors()
    {
        _services.Run = new ReportRunOutcome { Status = ReportRunStatus.Invalid, Errors = ["Department is required.", "That page doesn't exist."] };

        ActionResult<ReportRunResult> result = await _controller.Run(Slug, new ReportRunRequest(), Ct);

        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(Assert.IsType<BadRequestObjectResult>(result.Result).Value);
        Assert.Equal(["Department is required.", "That page doesn't exist."], problem.Errors[string.Empty]);
        Assert.Equal("The report request is not valid.", problem.Title);
    }

    [Theory]
    [InlineData("The column 'Salary' is no longer in this report's data source.", "The column 'Salary' is no longer in this report's data source.")]
    [InlineData(null, "This report is unavailable.")]
    public async Task Run_Unavailable_Returns503(string? error, string expected)
    {
        _services.Run = new ReportRunOutcome { Status = ReportRunStatus.Unavailable, Errors = error is null ? [] : [error] };

        ActionResult<ReportRunResult> result = await _controller.Run(Slug, new ReportRunRequest(), Ct);

        ObjectResult unavailable = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal(expected, unavailable.Value);
    }

    [Fact]
    public async Task Export_Ok_ReturnsAnUncachedDownload()
    {
        _services.Export = new ReportExportOutcome
        {
            Status = ReportRunStatus.Ok,
            File = new ReportFile { Content = [1, 2, 3], ContentType = "application/pdf", FileName = "Roster.pdf" },
        };

        ActionResult result = await _controller.Export(Slug, "pdf", new ReportRunRequest(), Ct);

        FileContentResult download = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", download.ContentType);
        Assert.Equal("Roster.pdf", download.FileDownloadName);
        Assert.Equal([1, 2, 3], download.FileContents);
        Assert.Equal("private, no-store, max-age=0", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("pdf", _services.LastFormat);
    }

    [Fact]
    public async Task Export_NoRows_Returns204()
    {
        _services.Export = new ReportExportOutcome { Status = ReportRunStatus.Ok };

        Assert.IsType<NoContentResult>(await _controller.Export(Slug, "xlsx", new ReportRunRequest(), Ct));
    }

    [Fact]
    public async Task Export_Invalid_Returns400()
    {
        _services.Export = new ReportExportOutcome { Status = ReportRunStatus.Invalid, Errors = ["Choose a download format: xlsx or pdf."] };

        ActionResult result = await _controller.Export(Slug, "csv", new ReportRunRequest(), Ct);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>Returns canned outcomes and records what the controller asked for.</summary>
    private sealed class StubServices : IReportViewerService, IReportRunService, IReportExportService
    {
        public IReadOnlyList<ReportCatalogItem> Catalog { get; set; } = [];
        public ReportDefinitionOutcome Definition { get; set; } = new() { Status = ReportRunStatus.NotFound };
        public ReportRunOutcome Run { get; set; } = new() { Status = ReportRunStatus.NotFound };
        public ReportExportOutcome Export { get; set; } = new() { Status = ReportRunStatus.NotFound };
        public string? LastSlug { get; private set; }
        public ReportRunRequest? LastRequest { get; private set; }
        public ReportRunMode? LastMode { get; private set; }
        public string? LastFormat { get; private set; }

        public Task<IReadOnlyList<ReportCatalogItem>> GetCatalogAsync(CancellationToken ct)
        {
            return Task.FromResult(Catalog);
        }

        public Task<ReportDefinitionOutcome> GetDefinitionAsync(string slug, CancellationToken ct)
        {
            LastSlug = slug;
            return Task.FromResult(Definition);
        }

        public Task<ReportRunOutcome> RunAsync(string slug, ReportRunRequest request, ReportRunMode mode, CancellationToken ct)
        {
            LastSlug = slug;
            LastRequest = request;
            LastMode = mode;
            return Task.FromResult(Run);
        }

        public Task<ReportRunOutcome> PreviewAsync(ReportDefinition report, ReportRunRequest request, CancellationToken ct)
        {
            throw new NotSupportedException("The viewer never previews.");
        }

        public Task<ReportExportOutcome> ExportAsync(string slug, string format, ReportRunRequest request, CancellationToken ct)
        {
            LastSlug = slug;
            LastFormat = format;
            LastRequest = request;
            return Task.FromResult(Export);
        }
    }
}
