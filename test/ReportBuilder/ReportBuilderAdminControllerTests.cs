using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Controllers;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.test.ReportBuilder;

/// <summary>The admin controller passes requests to the services and turns their outcomes into responses.</summary>
public sealed class ReportBuilderAdminControllerTests
{
    private readonly StubAdminService _admin = new();
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportPermissionLookup _permissions = new();
    private readonly StubProblemService _problems = new();
    private readonly ReportBuilderAdminController _controller;

    public ReportBuilderAdminControllerTests()
    {
        _controller = new ReportBuilderAdminController(_admin, _catalog, _permissions, _problems)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "FacultyRoster",
            Kind = ReportSourceKind.View,
            Columns = [],
            Parameters = [],
            PagesInSql = true,
        });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly ReportEditRequest Fields = new() { Title = "Roster", Source = "FacultyRoster" };

    private static readonly ReportAdminDefinition Saved = new() { Id = 4, Slug = Guid.Empty, Report = Fields };

    private static T OkValue<T>(ActionResult<T> result)
    {
        return Assert.IsAssignableFrom<T>(Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public void Controller_NeedsTheAdminPermission()
    {
        Assert.Equal(ReportBuilderPermissions.Admin, typeof(ReportBuilderAdminController).GetCustomAttribute<PermissionAttribute>()?.Allow);
        Assert.Equal("/api/report-builder/admin", typeof(ReportBuilderAdminController).GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.True(typeof(ApiController).IsAssignableFrom(typeof(ReportBuilderAdminController)));
    }

    [Fact]
    public async Task Sources_ListAndDescribe()
    {
        IReadOnlyList<ReportSourceSummary> sources = OkValue(await _controller.GetSources(Ct));
        ReportSourceDetail source = OkValue(await _controller.GetSource("FacultyRoster", Ct));

        Assert.Equal("FacultyRoster", Assert.Single(sources).Name);
        Assert.Equal("FacultyRoster", source.Name);
        Assert.IsType<NotFoundObjectResult>((await _controller.GetSource("Dropped", Ct)).Result);
    }

    [Fact]
    public async Task Permissions_AreSearched()
    {
        IReadOnlyList<ReportPermissionOption> found = OkValue(await _controller.SearchPermissions("effort", Ct));

        Assert.Equal("SVMSecure.Effort", Assert.Single(found).Permission);
    }

    [Fact]
    public async Task List_PassesTheDeletedFlag()
    {
        OkValue(await _controller.List(includeDeleted: true, Ct));

        Assert.True(_admin.LastIncludeDeleted);
    }

    [Fact]
    public async Task Get_ReturnsTheReportOr404()
    {
        _admin.Definition = Saved;

        Assert.Same(Saved, OkValue(await _controller.Get(4, Ct)));

        _admin.Definition = null;
        Assert.IsType<NotFoundObjectResult>((await _controller.Get(4, Ct)).Result);
    }

    [Fact]
    public async Task Create_Returns201WithTheReport()
    {
        _admin.Outcome = ReportAdminOutcome.Ok(Saved);

        ActionResult<ReportAdminDefinition> result = await _controller.Create(Fields, Ct);

        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal((nameof(ReportBuilderAdminController.Get), (object?)4), (created.ActionName, created.RouteValues?["id"]));
        Assert.Same(Saved, created.Value);
    }

    [Fact]
    public async Task Create_Invalid_Returns400WithTheErrors()
    {
        _admin.Outcome = ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.Invalid, "Enter a title.");

        ActionResult<ReportAdminDefinition> result = await _controller.Create(Fields, Ct);

        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(Assert.IsType<BadRequestObjectResult>(result.Result).Value);
        Assert.Equal(["Enter a title."], problem.Errors[string.Empty]);
        Assert.Equal("The report can't be saved.", problem.Title);
    }

    [Fact]
    public async Task Update_PassesTheIdAndMapsTheOutcome()
    {
        _admin.Outcome = ReportAdminOutcome.Ok(Saved);

        Assert.Same(Saved, OkValue(await _controller.Update(4, Fields, Ct)));
        Assert.Equal(4, _admin.LastId);
    }

    [Fact]
    public async Task Update_FromAnOlderCopy_Returns409()
    {
        _admin.Outcome = ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.Conflict, "Someone else saved it.");

        ActionResult<ReportAdminDefinition> result = await _controller.Update(4, Fields, Ct);

        Assert.Equal("Someone else saved it.", Assert.IsType<ConflictObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task DeleteAndRestore_MapTheOutcome()
    {
        _admin.Outcome = ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.NotFound);

        Assert.IsType<NotFoundObjectResult>((await _controller.Delete(4, Ct)).Result);
        Assert.IsType<NotFoundObjectResult>((await _controller.Restore(4, Ct)).Result);
        Assert.Equal(["Delete", "Restore"], _admin.Calls);
    }

    [Fact]
    public async Task History_ReturnsTheVersionsOr404()
    {
        _admin.History = [new ReportHistoryItem { Id = 1, Action = ReportHistoryAction.Created }];

        Assert.Single(OkValue(await _controller.GetHistory(4, Ct)));

        _admin.History = null;
        Assert.IsType<NotFoundObjectResult>((await _controller.GetHistory(4, Ct)).Result);
    }

    [Fact]
    public async Task HistoryVersion_ReturnsTheVersionOr404()
    {
        _admin.Version = new ReportHistoryVersion { Version = new ReportHistoryItem { Id = 9 }, Report = Fields };

        Assert.Equal(9, OkValue(await _controller.GetHistoryVersion(4, 9, Ct)).Version.Id);

        _admin.Version = null;
        Assert.IsType<NotFoundObjectResult>((await _controller.GetHistoryVersion(4, 9, Ct)).Result);
    }

    [Fact]
    public async Task Preview_ReturnsThePage()
    {
        var page = new ReportRunResult { Columns = [], Rows = [], Page = 1, PerPage = 50, TotalRows = 0, Truncated = false };
        _admin.Preview = new ReportRunOutcome { Status = ReportRunStatus.Ok, Result = page };

        Assert.Same(page, OkValue(await _controller.Preview(new ReportPreviewRequest { Report = Fields }, Ct)));
    }

    [Theory]
    [InlineData(ReportRunStatus.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ReportRunStatus.Invalid, StatusCodes.Status400BadRequest)]
    [InlineData(ReportRunStatus.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    public async Task Preview_MapsFailures(ReportRunStatus status, int expected)
    {
        _admin.Preview = new ReportRunOutcome { Status = status };

        ActionResult<ReportRunResult> result = await _controller.Preview(new ReportPreviewRequest { Report = Fields }, Ct);

        Assert.Equal(expected, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task Preview_Unavailable_SaysWhy()
    {
        _admin.Preview = new ReportRunOutcome { Status = ReportRunStatus.Unavailable, Errors = ["The source is gone."] };

        ActionResult<ReportRunResult> result = await _controller.Preview(new ReportPreviewRequest { Report = Fields }, Ct);

        Assert.Equal("The source is gone.", Assert.IsAssignableFrom<ObjectResult>(result.Result).Value);
    }

    /// <summary>Returns canned outcomes and records what the controller asked for.</summary>
    [Fact]
    public async Task Failures_PassTheWindowThrough()
    {
        IReadOnlyList<ReportFailure> failures = OkValue(await _controller.GetFailures(7, Ct));

        Assert.Same(_problems.Failures, failures);
        Assert.Equal(7, _problems.LastDays);
    }

    [Fact]
    public async Task Check_ReturnsTheResult()
    {
        Assert.Same(_problems.Check, OkValue(await _controller.Check(Ct)));
    }

    private sealed class StubProblemService : IReportProblemService
    {
        public IReadOnlyList<ReportFailure> Failures { get; } = [];
        public ReportCheckResult Check { get; } = new() { Checked = 0, CheckedOn = DateTime.MinValue, Failing = [] };
        public int? LastDays { get; private set; }

        public Task<IReadOnlyList<ReportFailure>> GetFailuresAsync(int? days, CancellationToken ct)
        {
            LastDays = days;
            return Task.FromResult(Failures);
        }

        public Task<ReportCheckResult> CheckAsync(CancellationToken ct)
        {
            return Task.FromResult(Check);
        }
    }

    private sealed class StubAdminService : IReportAdminService
    {
        public ReportAdminDefinition? Definition { get; set; }
        public ReportAdminOutcome<ReportAdminDefinition> Outcome { get; set; } = ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.NotFound);
        public IReadOnlyList<ReportHistoryItem>? History { get; set; }
        public ReportHistoryVersion? Version { get; set; }
        public ReportRunOutcome Preview { get; set; } = new() { Status = ReportRunStatus.Ok };
        public bool LastIncludeDeleted { get; private set; }
        public int? LastId { get; private set; }
        public List<string> Calls { get; } = [];

        public Task<IReadOnlyList<ReportAdminListItem>> ListAsync(bool includeDeleted, CancellationToken ct)
        {
            LastIncludeDeleted = includeDeleted;
            return Task.FromResult<IReadOnlyList<ReportAdminListItem>>([]);
        }

        public Task<ReportAdminDefinition?> GetAsync(int id, CancellationToken ct)
        {
            return Task.FromResult(Definition);
        }

        public Task<ReportAdminOutcome<ReportAdminDefinition>> CreateAsync(ReportEditRequest request, CancellationToken ct)
        {
            return Task.FromResult(Outcome);
        }

        public Task<ReportAdminOutcome<ReportAdminDefinition>> UpdateAsync(int id, ReportEditRequest request, CancellationToken ct)
        {
            LastId = id;
            return Task.FromResult(Outcome);
        }

        public Task<ReportAdminOutcome<ReportAdminDefinition>> DeleteAsync(int id, CancellationToken ct)
        {
            Calls.Add("Delete");
            return Task.FromResult(Outcome);
        }

        public Task<ReportAdminOutcome<ReportAdminDefinition>> RestoreAsync(int id, CancellationToken ct)
        {
            Calls.Add("Restore");
            return Task.FromResult(Outcome);
        }

        public Task<IReadOnlyList<ReportHistoryItem>?> GetHistoryAsync(int id, CancellationToken ct)
        {
            return Task.FromResult(History);
        }

        public Task<ReportHistoryVersion?> GetHistoryVersionAsync(int id, int historyId, CancellationToken ct)
        {
            return Task.FromResult(Version);
        }

        public Task<ReportRunOutcome> PreviewAsync(ReportPreviewRequest request, CancellationToken ct)
        {
            return Task.FromResult(Preview);
        }
    }
}
