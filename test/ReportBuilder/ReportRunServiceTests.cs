using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// Running a saved report: permission first, then the source and columns checked against the
/// catalog, parameters bound, the SQL run (or a procedure paged here), and the run logged.
/// The catalog, user and executor are fakes; the report definitions are in an in-memory database.
/// </summary>
public sealed class ReportRunServiceTests : IDisposable
{
    private const string ViewerPermission = "SVMSecure.Personnel.Reports";
    private static readonly Guid RosterSlug = Guid.Parse("8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e");
    private static readonly Guid LeaveSlug = Guid.Parse("1b2c3d4e-5f60-4718-8293-a4b5c6d7e8f9");
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 9, 30, 0, TimeSpan.Zero);

    private readonly ReportBuilderDbContext _context;
    private readonly FakeReportUserService _users = new();
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportQueryExecutor _executor = new();
    private readonly ReportRunService _service;

    // Cancelled from inside a fake query, so it lives as long as the test class.
    private readonly CancellationTokenSource _cancel = new();

    public ReportRunServiceTests()
    {
        DbContextOptions<ReportBuilderDbContext> options = ReportBuilderTestDb.CreateOptions();
        _context = new ReportBuilderDbContext(options);
        _service = CreateService(_context);
        _users.Permissions.Add(ViewerPermission);

        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "FacultyRoster",
            Kind = ReportSourceKind.View,
            Columns =
            [
                FakeReportSourceCatalog.Column("Name"),
                FakeReportSourceCatalog.Column("Dept"),
                FakeReportSourceCatalog.Column("Hired", ReportValueKind.Date),
                FakeReportSourceCatalog.Column("Notes"),
            ],
            Parameters = [],
            PagesInSql = true,
        });
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "usp_Leave",
            Kind = ReportSourceKind.Procedure,
            Columns = [FakeReportSourceCatalog.Column("Name"), FakeReportSourceCatalog.Column("Hours", ReportValueKind.FractionalNumber)],
            Parameters = [FakeReportSourceCatalog.Parameter("LoginId"), FakeReportSourceCatalog.Parameter("Year"), FakeReportSourceCatalog.Parameter("Out", supported: false)],
            PagesInSql = false,
        });
    }

    public void Dispose()
    {
        _context.Dispose();
        _cancel.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ReportRunService CreateService(ReportBuilderDbContext context)
    {
        return new ReportRunService(
            context,
            _users,
            _catalog,
            new ReportChoiceService(_catalog, _executor),
            _executor,
            NullLogger<ReportRunService>.Instance,
            new StepClock());
    }

    /// <summary>Fixed local time (UTC) and a timestamp that moves 15 ms on every read.</summary>
    private sealed class StepClock : TimeProvider
    {
        private long _ticks;

        public override DateTimeOffset GetUtcNow() => Now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override long TimestampFrequency => 1000;

        public override long GetTimestamp() => _ticks += 15;
    }

    private sealed class FakeDbException : DbException
    {
        public FakeDbException(string message = "timeout") : base(message)
        {
        }
    }

    private sealed class FailingSaveContext : ReportBuilderDbContext
    {
        public FailingSaveContext(DbContextOptions<ReportBuilderDbContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("log table is locked");
        }
    }

    private static ReportColumn Column(string name, int order, string? label = null, bool visible = true,
        bool sortable = true, ReportColumnFormat? format = null)
    {
        return new ReportColumn { SourceColumn = name, Label = label, DisplayOrder = order, Visible = visible, Sortable = sortable, Format = format };
    }

    private async Task AddRosterAsync(Action<ReportDefinition>? configure = null)
    {
        var report = new ReportDefinition
        {
            Id = 1,
            Slug = RosterSlug,
            Title = "Faculty roster",
            Source = "FacultyRoster",
            PageSize = 10,
            AllowExcel = true,
            Permissions = [new ReportPermission { ReportId = 1, Permission = ViewerPermission }],
            Columns =
            [
                Column("Dept", 2, "Department"),
                Column("Name", 1, format: ReportColumnFormat.Text),
                Column("Hired", 3, "Hire date", format: ReportColumnFormat.Date),
                Column("Notes", 4, visible: false),
                Column(" ", 5),
            ],
            Parameters =
            [
                new ReportParameter { Name = "Dept", Label = "Department", Type = ReportParameterType.Text, Operator = ReportFilterOperator.Equals },
            ],
        };
        configure?.Invoke(report);
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(Ct);
    }

    private async Task AddLeaveAsync(Action<ReportDefinition>? configure = null)
    {
        var report = new ReportDefinition
        {
            Id = 2,
            Slug = LeaveSlug,
            Title = "My leave",
            Source = "usp_Leave",
            PageSize = 2,
            Permissions = [new ReportPermission { ReportId = 2, Permission = ViewerPermission }],
            Columns = [Column("Name", 1), Column("Hours", 2, format: ReportColumnFormat.Number)],
            Parameters =
            [
                new ReportParameter { Name = "LoginId", Binding = ReportParameterBinding.CurrentUserLoginId },
                new ReportParameter { Name = "Year", Type = ReportParameterType.Number },
            ],
        };
        configure?.Invoke(report);
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(Ct);
    }

    private Task<ReportRunOutcome> RunAsync(Guid slug, ReportRunRequest? request = null, ReportRunMode mode = ReportRunMode.Screen)
    {
        return _service.RunAsync(slug.ToString(), request ?? new ReportRunRequest(), mode, Ct);
    }

    private static Dictionary<string, IReadOnlyList<string>> Values(params (string Name, string Value)[] values)
    {
        return values.ToDictionary(v => v.Name, v => (IReadOnlyList<string>)[v.Value]);
    }

    private void AnswerRows(params IReadOnlyDictionary<string, object?>[] rows)
    {
        _executor.Answer = command => new ReportQueryResult
        {
            Rows = rows,
            TotalRows = command.CountText is null ? null : 42,
            Truncated = false,
        };
    }

    private static ReportRunResult Ok(ReportRunOutcome outcome)
    {
        Assert.Equal(ReportRunStatus.Ok, outcome.Status);
        Assert.Empty(outcome.Errors);
        Assert.NotNull(outcome.Result);
        return outcome.Result;
    }

    private static void AssertFails(ReportRunOutcome outcome, ReportRunStatus status, params string[] errors)
    {
        Assert.Equal(status, outcome.Status);
        Assert.Null(outcome.Result);
        Assert.Equal(errors, outcome.Errors);
    }

    // Who may run it

    [Fact]
    public async Task NoOneSignedIn_IsForbidden()
    {
        await AddRosterAsync();
        _users.User = null;

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Forbidden);
        Assert.Empty(_executor.Calls);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task UnknownSlug_IsNotFound(string slug)
    {
        await AddRosterAsync();

        AssertFails(await _service.RunAsync(slug, new ReportRunRequest(), ReportRunMode.Screen, Ct), ReportRunStatus.NotFound);
    }

    [Fact]
    public async Task DeletedReport_IsNotFound()
    {
        await AddRosterAsync(r => r.DeletedOn = Now.DateTime);

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.NotFound);
    }

    [Fact]
    public async Task WithoutTheReportsPermission_IsForbiddenBeforeParametersAreRead()
    {
        await AddRosterAsync(r => r.Permissions = [new ReportPermission { ReportId = 1, Permission = "SVMSecure.Other" }, new ReportPermission { ReportId = 1, Permission = " " }]);

        ReportRunOutcome outcome = await RunAsync(RosterSlug, new ReportRunRequest { Parameters = Values(("Nope", "x")) });

        AssertFails(outcome, ReportRunStatus.Forbidden);
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task ReportWithoutPermissions_IsForAdminsOnly()
    {
        await AddRosterAsync(r => r.Permissions = []);

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Forbidden);

        _users.Permissions.Add(ReportBuilderPermissions.Admin);
        Ok(await RunAsync(RosterSlug));
    }

    [Theory]
    [InlineData(ReportRunMode.ExportExcel)]
    [InlineData(ReportRunMode.ExportPdf)]
    public async Task Downloads_TheReportDoesNotAllow_AreForbidden(ReportRunMode mode)
    {
        await AddRosterAsync(r =>
        {
            r.AllowExcel = false;
            r.AllowPdf = false;
        });

        AssertFails(await RunAsync(RosterSlug, mode: mode), ReportRunStatus.Forbidden);
    }

    // Checks against the source and the request

    [Fact]
    public async Task SourceGone_IsUnavailable()
    {
        await AddRosterAsync(r => r.Source = "Dropped");

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Unavailable, "This report's data source is no longer available.");
    }

    [Fact]
    public async Task BadParameters_AreInvalid()
    {
        await AddRosterAsync();

        ReportRunOutcome outcome = await RunAsync(RosterSlug, new ReportRunRequest { Parameters = Values(("Other", "x")) });

        AssertFails(outcome, ReportRunStatus.Invalid, "This report has no parameter named 'Other'.");
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task NoVisibleColumns_IsUnavailable()
    {
        await AddRosterAsync(r => r.Columns = [Column("Name", 1, visible: false)]);

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Unavailable, "This report has no columns to show.");
    }

    [Fact]
    public async Task ColumnGoneFromTheSource_IsUnavailable()
    {
        await AddRosterAsync(r => r.Columns = [Column("Name", 1), Column("Salary", 2)]);

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Unavailable, "The column 'Salary' is no longer in this report's data source.");
    }

    [Fact]
    public async Task FilterOnAColumnGoneFromTheSource_IsUnavailable()
    {
        await AddRosterAsync(r => r.Parameters = [new ReportParameter { Name = "Building", DefaultValue = "VM3A" }]);

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Unavailable, "The parameter 'Building' is no longer in this report's data source.");
    }

    [Fact]
    public async Task ProcedureParameterTheSourceCannotTake_IsUnavailable()
    {
        await AddLeaveAsync(r => r.Parameters = [new ReportParameter { Name = "Out", DefaultValue = "x" }]);

        AssertFails(await RunAsync(LeaveSlug), ReportRunStatus.Unavailable, "The parameter 'Out' is no longer in this report's data source.");
    }

    [Fact]
    public async Task DefaultSortGoneFromTheSource_IsUnavailable()
    {
        await AddRosterAsync(r => r.OrderBy = "Salary");

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Unavailable, "The sort column 'Salary' is no longer in this report's data source.");
    }

    [Fact]
    public async Task BadPagingSortAndSearch_AreInvalidTogether()
    {
        await AddRosterAsync();

        ReportRunOutcome outcome = await RunAsync(RosterSlug, new ReportRunRequest
        {
            Page = 0,
            PerPage = ReportLimits.MaxPageSize + 1,
            SortBy = "Notes",
            Search = new string('x', ReportLimits.MaxTextLength + 1),
        });

        AssertFails(outcome, ReportRunStatus.Invalid,
            "This report can't be sorted by that column.",
            "Search text must be 500 characters or fewer.",
            "Rows per page must be between 1 and 500.",
            "That page doesn't exist.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PerPageBelowOne_IsInvalid(int perPage)
    {
        await AddRosterAsync();

        AssertFails(await RunAsync(RosterSlug, new ReportRunRequest { PerPage = perPage }), ReportRunStatus.Invalid,
            "Rows per page must be between 1 and 500.");
    }

    [Fact]
    public async Task PagePastTheRowCap_IsInvalid()
    {
        await AddRosterAsync();

        AssertFails(await RunAsync(RosterSlug, new ReportRunRequest { Page = int.MaxValue, PerPage = 500 }), ReportRunStatus.Invalid,
            "That page doesn't exist.");
    }

    [Fact]
    public async Task UnsortableColumn_CannotBeSortedBy()
    {
        await AddRosterAsync(r => r.Columns = [Column("Name", 1, sortable: false)]);

        AssertFails(await RunAsync(RosterSlug, new ReportRunRequest { SortBy = "Name" }), ReportRunStatus.Invalid,
            "This report can't be sorted by that column.");
    }

    // Views

    [Fact]
    public async Task View_PagedInSql()
    {
        await AddRosterAsync();
        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada"), ("Dept", "VME"), ("Hired", null), ("Extra", "dropped")));

        ReportRunResult result = Ok(await RunAsync(RosterSlug, new ReportRunRequest
        {
            Parameters = Values(("dept", "VME")),
            Page = 3,
            SortBy = "name",
            SortDescending = true,
            Search = " ada ",
        }));

        (ReportSqlCommand command, int maxRows, TimeSpan timeout) = Assert.Single(_executor.Calls);
        Assert.Equal(
            "SELECT [Name], [Dept], [Hired] FROM [report].[FacultyRoster] WHERE [Dept] = @p0 "
            + "AND ([Name] LIKE @__search ESCAPE '\\' OR [Dept] LIKE @__search ESCAPE '\\') "
            + "ORDER BY [Name] DESC OFFSET @__skip ROWS FETCH NEXT @__take ROWS ONLY",
            command.Text);
        Assert.Equal(20, command.Parameters["@__skip"]);
        Assert.Equal(10, command.Parameters["@__take"]);
        Assert.Equal("%ada%", command.Parameters["@__search"]);
        Assert.Equal(10, maxRows);
        Assert.Equal(ReportLimits.ScreenTimeout, timeout);

        Assert.Equal(
            [
                new ReportResultColumn { Name = "Name", Label = "Name", Format = ReportColumnFormat.Text, Sortable = true },
                new ReportResultColumn { Name = "Dept", Label = "Department", Format = ReportColumnFormat.Text, Sortable = true },
                new ReportResultColumn { Name = "Hired", Label = "Hire date", Format = ReportColumnFormat.Date, Sortable = true },
            ],
            result.Columns);
        IReadOnlyDictionary<string, object?> row = Assert.Single(result.Rows);
        Assert.Equal(["Name", "Dept", "Hired"], row.Keys);
        Assert.Equal("Ada", row["Name"]);
        Assert.Null(row["Hired"]);
        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.PerPage);
        Assert.Equal(42, result.TotalRows);
        Assert.False(result.Truncated);
        Assert.Equal("""{"Dept":"VME"}""", Assert.Single(_context.ReportRunLog).Parameters);
    }

    [Fact]
    public async Task View_RequestedPageSize_AndDefaultsWhenTheReportHasNone()
    {
        await AddRosterAsync(r => r.PageSize = null);

        Ok(await RunAsync(RosterSlug, new ReportRunRequest { PerPage = 100 }));
        Ok(await RunAsync(RosterSlug));

        Assert.Equal(100, _executor.Calls[0].Command.Parameters["@__take"]);
        Assert.Equal(ReportLimits.DefaultPageSize, _executor.Calls[1].Command.Parameters["@__take"]);
    }

    [Fact]
    public async Task View_DefaultSort_IsTheReportsOrder()
    {
        await AddRosterAsync(r =>
        {
            r.OrderBy = " hired ";
            r.OrderByDescending = true;
        });

        Ok(await RunAsync(RosterSlug));

        Assert.Contains("ORDER BY [Hired] DESC", _executor.LastCommand.Text);
    }

    [Fact]
    public async Task View_Unpaginated_ReadsUpToTheCapAndSaysWhenCut()
    {
        await AddRosterAsync(r => r.Paginate = false);
        _executor.Answer = _ => new ReportQueryResult
        {
            Rows = [FakeReportQueryExecutor.Row(("Name", "Ada")), FakeReportQueryExecutor.Row(("Name", "Bo"))],
            Truncated = true,
        };

        ReportRunResult result = Ok(await RunAsync(RosterSlug, new ReportRunRequest { Page = 7, PerPage = 1 }));

        Assert.StartsWith("SELECT TOP (@__top)", _executor.LastCommand.Text);
        Assert.Equal(ReportLimits.MaxRows, _executor.Calls[0].MaxRows);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PerPage);
        Assert.Equal(2, result.TotalRows);
        Assert.True(result.Truncated);
    }

    [Theory]
    [InlineData(ReportRunMode.ExportExcel, ReportRunAction.ExportExcel)]
    [InlineData(ReportRunMode.ExportPdf, ReportRunAction.ExportPdf)]
    public async Task Downloads_ReadEveryRowWithTheLongerTimeout(ReportRunMode mode, ReportRunAction action)
    {
        await AddRosterAsync(r => r.AllowPdf = true);

        Ok(await RunAsync(RosterSlug, new ReportRunRequest { Page = 2 }, mode));

        Assert.StartsWith("SELECT TOP (@__top)", _executor.LastCommand.Text);
        Assert.Equal(ReportLimits.ExportTimeout, _executor.Calls[0].Timeout);
        Assert.Equal(action, Assert.Single(_context.ReportRunLog).Action);
    }

    [Fact]
    public async Task View_DatabaseError_IsUnavailableAndLoggedWithSqlServersReason()
    {
        await AddRosterAsync();
        _executor.Answer = _ => throw new FakeDbException("The SELECT permission was denied on the object 'FacultyRoster'.");

        AssertFails(await RunAsync(RosterSlug), ReportRunStatus.Unavailable, "The report couldn't be run. Try again later.");

        ReportRunLog log = Assert.Single(_context.ReportRunLog);
        Assert.Equal("The SELECT permission was denied on the object 'FacultyRoster'.", log.ErrorMessage);
        Assert.Null(log.ErrorNumber);
        Assert.Null(log.RowsReturned);
        Assert.Equal(ReportRunAction.Run, log.Action);
    }

    [Fact]
    public async Task DatabaseError_LongerThanTheLogKeeps_IsCut()
    {
        await AddRosterAsync();
        _executor.Answer = _ => throw new FakeDbException(new string('x', ReportLimits.MaxErrorLength + 10));

        await RunAsync(RosterSlug);

        Assert.Equal(ReportLimits.MaxErrorLength, Assert.Single(_context.ReportRunLog).ErrorMessage?.Length);
    }

    [Fact]
    public async Task DatabaseError_AfterTheUserCancelled_IsNotLogged()
    {
        await AddRosterAsync();
        _executor.Answer = _ =>
        {
            _cancel.Cancel();
            throw new FakeDbException("Operation cancelled by user.");
        };

        ReportRunOutcome outcome = await _service.RunAsync(RosterSlug.ToString(), new ReportRunRequest(), ReportRunMode.Screen, _cancel.Token);

        Assert.Equal(ReportRunStatus.Unavailable, outcome.Status);
        Assert.Empty(_context.ReportRunLog);
    }

    [Fact]
    public async Task SourceGone_IsLoggedAsAFailure()
    {
        await AddRosterAsync(r => r.Source = "Dropped");

        await RunAsync(RosterSlug, mode: ReportRunMode.ExportExcel);

        ReportRunLog log = Assert.Single(_context.ReportRunLog);
        Assert.Equal("This report's data source is no longer available.", log.ErrorMessage);
        Assert.Equal(ReportRunAction.ExportExcel, log.Action);
        Assert.Null(log.Parameters);
    }

    [Fact]
    public async Task BadParameters_AreTheUsersMistake_AndNotLogged()
    {
        await AddRosterAsync();

        await RunAsync(RosterSlug, new ReportRunRequest { Parameters = Values(("Other", "x")) });

        Assert.Empty(_context.ReportRunLog);
    }

    [Fact]
    public async Task ChoiceParameter_IsCheckedAgainstItsChoices()
    {
        await AddRosterAsync(r => r.Parameters =
        [
            new ReportParameter { Name = "Dept", Type = ReportParameterType.MultiChoice, ChoicesList = """["VME","PMI"]""", Operator = ReportFilterOperator.In },
        ]);

        Ok(await RunAsync(RosterSlug, new ReportRunRequest
        {
            Parameters = new Dictionary<string, IReadOnlyList<string>> { ["Dept"] = ["PMI", "VME"] },
        }));

        Assert.Contains("WHERE [Dept] IN (@p0_0, @p0_1)", _executor.LastCommand.Text);
    }

    // Procedures

    [Fact]
    public async Task Procedure_PagedSortedAndSearchedInMemory()
    {
        await AddLeaveAsync();
        _executor.Answer = _ => new ReportQueryResult
        {
            Rows =
            [
                FakeReportQueryExecutor.Row(("Name", "Cy"), ("Hours", 8m)),
                FakeReportQueryExecutor.Row(("Name", "ada"), ("Hours", 2m)),
                FakeReportQueryExecutor.Row(("Name", "Bea"), ("Hours", 4m)),
                FakeReportQueryExecutor.Row(("Name", "Dee"), ("Hours", 1m)),
                FakeReportQueryExecutor.Row(("Name", null), ("Hours", 9m)),
                FakeReportQueryExecutor.Row(("Name", "Ed"), ("Hours", 3m)),
            ],
            Truncated = true,
        };

        ReportRunResult result = Ok(await RunAsync(LeaveSlug, new ReportRunRequest
        {
            Parameters = Values(("Year", "2026")),
            Page = 2,
            SortBy = "Name",
            SortDescending = true,
            Search = "E",
        }));

        (ReportSqlCommand command, int maxRows, _) = Assert.Single(_executor.Calls);
        Assert.Equal("EXEC [report].[usp_Leave] @LoginId = @p0, @Year = @p1", command.Text);
        Assert.Equal("jdoe", command.Parameters["@p0"]);
        Assert.Equal(2026m, command.Parameters["@p1"]);
        Assert.Equal(ReportLimits.MaxRows, maxRows);

        // "E" matches Ed, Dee and Bea; sorted descending: Ed, Dee, Bea; page 2 of 2 rows each.
        Assert.Equal(["Bea"], result.Rows.Select(r => r["Name"]));
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PerPage);
        Assert.True(result.Truncated);
    }

    [Fact]
    public async Task Procedure_SortedAscendingWithEmptyValuesFirst()
    {
        await AddLeaveAsync(r => r.Paginate = false);
        _executor.Answer = _ => new ReportQueryResult
        {
            Rows =
            [
                FakeReportQueryExecutor.Row(("Name", "d"), ("Hours", 2.5)),
                FakeReportQueryExecutor.Row(("Name", "c"), ("Hours", 2m)),
                FakeReportQueryExecutor.Row(("Name", "a"), ("Hours", null)),
                FakeReportQueryExecutor.Row(("Name", "b"), ("Hours", 1m)),
                FakeReportQueryExecutor.Row(("Name", "e"), ("Hours", null)),
            ],
            Truncated = false,
        };

        ReportRunResult result = Ok(await RunAsync(LeaveSlug, new ReportRunRequest { SortBy = "Hours" }));

        // Values of one type compare as themselves; mixed types (decimal and double) as text.
        Assert.Equal(["a", "e", "b", "c", "d"], result.Rows.Select(r => r["Name"]));
        Assert.Equal(5, result.PerPage);
    }

    [Fact]
    public async Task Procedure_ThatPagesItself_GetsSkipAndTakeAndReportsItsTotal()
    {
        await AddLeaveAsync(r => r.OrderBy = "Name");
        _catalog.Sources[1] = _catalog.Sources[1] with { PagesInSql = true };
        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada"), ("Hours", 1m), ("TotalRows", 11)));

        ReportRunResult result = Ok(await RunAsync(LeaveSlug, new ReportRunRequest { Page = 2 }));

        (ReportSqlCommand command, int maxRows, _) = Assert.Single(_executor.Calls);
        Assert.Equal("EXEC [report].[usp_Leave] @LoginId = @p0, @Skip = @__skip, @Take = @__take", command.Text);
        Assert.Equal(2, command.Parameters["@__skip"]);
        Assert.Equal(2, maxRows);
        Assert.Equal(11, result.TotalRows);
        Assert.Equal(["Name", "Hours"], Assert.Single(result.Rows).Keys);
        Assert.False(result.Truncated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("many")]
    public async Task Procedure_ThatPagesItself_WithoutAUsableTotal_HasNoTotal(string? total)
    {
        await AddLeaveAsync();
        _catalog.Sources[1] = _catalog.Sources[1] with { PagesInSql = true };
        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada"), ("TotalRows", total)));

        Assert.Null(Ok(await RunAsync(LeaveSlug)).TotalRows);
    }

    [Fact]
    public async Task Procedure_ThatPagesItself_PastTheLastPage_HasNoTotal()
    {
        await AddLeaveAsync();
        _catalog.Sources[1] = _catalog.Sources[1] with { PagesInSql = true };

        ReportRunResult result = Ok(await RunAsync(LeaveSlug, new ReportRunRequest { Page = 9 }));

        Assert.Empty(result.Rows);
        Assert.Null(result.TotalRows);
    }

    [Fact]
    public async Task Procedure_ThatPagesItself_CannotBeSearched()
    {
        await AddLeaveAsync();
        _catalog.Sources[1] = _catalog.Sources[1] with { PagesInSql = true };

        AssertFails(await RunAsync(LeaveSlug, new ReportRunRequest { Search = "x" }), ReportRunStatus.Invalid,
            "This report can't be searched.");
    }

    [Fact]
    public async Task Procedure_ThatPagesItself_CannotBeSortedByRequest()
    {
        await AddLeaveAsync();
        _catalog.Sources[1] = _catalog.Sources[1] with { PagesInSql = true };

        AssertFails(await RunAsync(LeaveSlug, new ReportRunRequest { SortBy = "Name" }), ReportRunStatus.Invalid,
            "This report's order is set by its data source and can't be changed.");
    }

    [Fact]
    public async Task Procedure_ThatPagesItself_StillGetsBoundedSkipAndTakeWhenDownloaded()
    {
        await AddLeaveAsync();
        _catalog.Sources[1] = _catalog.Sources[1] with { PagesInSql = true };
        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada"), ("Hours", 1m)));

        Ok(await RunAsync(LeaveSlug, mode: ReportRunMode.ExportExcel));

        Assert.Equal("EXEC [report].[usp_Leave] @LoginId = @p0, @Skip = @__skip, @Take = @__take", _executor.LastCommand.Text);
        Assert.Equal(0, _executor.LastCommand.Parameters["@__skip"]);
        Assert.Equal(ReportLimits.MaxRows, _executor.LastCommand.Parameters["@__take"]);
    }

    [Fact]
    public async Task Procedure_Buffered_SortsByAHiddenDefaultColumnBeforeDroppingIt()
    {
        await AddLeaveAsync(r =>
        {
            r.Columns = [Column("Name", 1)];
            r.OrderBy = "Hours";
        });
        AnswerRows(
            FakeReportQueryExecutor.Row(("Name", "Bea"), ("Hours", 2m)),
            FakeReportQueryExecutor.Row(("Name", "Ada"), ("Hours", 1m)));

        ReportRunResult result = Ok(await RunAsync(LeaveSlug));

        Assert.Equal(["Ada", "Bea"], result.Rows.Select(r => r["Name"]));
    }

    [Fact]
    public async Task Procedure_WhoseColumnsAreUnknown_TrustsTheReportsColumns()
    {
        await AddLeaveAsync(r =>
        {
            r.Columns = [Column("Name", 1), Column("Comment", 2)];
            r.OrderBy = "Comment";
        });
        _catalog.Sources[1] = _catalog.Sources[1] with { Columns = [], ColumnsError = "Uses a temp table." };
        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada")));

        ReportRunResult result = Ok(await RunAsync(LeaveSlug));

        IReadOnlyDictionary<string, object?> row = Assert.Single(result.Rows);
        Assert.Equal("Ada", row["Name"]);
        Assert.Null(row["Comment"]);
    }

    // Preview

    [Fact]
    public async Task Preview_RunsAnUnsavedReportAPageAtATimeWithoutLogging()
    {
        _users.Permissions.Add(ReportBuilderPermissions.Admin);
        var report = new ReportDefinition
        {
            Title = "Draft",
            Source = "FacultyRoster",
            Paginate = false,
            PageSize = 10,
            Columns = [Column("Name", 1)],
        };

        ReportRunResult result = Ok(await _service.PreviewAsync(report, new ReportRunRequest { Page = 2, PerPage = 500 }, Ct));

        (ReportSqlCommand command, int maxRows, _) = Assert.Single(_executor.Calls);
        Assert.Equal((50, 50), (command.Parameters["@__skip"], command.Parameters["@__take"]));
        Assert.Equal((ReportLimits.PreviewRows, 2), (result.PerPage, result.Page));
        Assert.Equal(ReportLimits.PreviewRows, maxRows);
        Assert.Empty(_context.ReportRunLog);
    }

    [Fact]
    public async Task Preview_DatabaseError_ShowsSqlServersReasonAndIsNotLogged()
    {
        _users.Permissions.Add(ReportBuilderPermissions.Admin);
        _executor.Answer = _ => throw new FakeDbException("Invalid column name 'Dept'.");
        var report = new ReportDefinition { Title = "Draft", Source = "FacultyRoster", Columns = [Column("Name", 1)] };

        ReportRunOutcome outcome = await _service.PreviewAsync(report, new ReportRunRequest(), Ct);

        AssertFails(outcome, ReportRunStatus.Unavailable, "SQL Server couldn't run the report: Invalid column name 'Dept'.");
        Assert.Empty(_context.ReportRunLog);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Preview_IsForAdminsOnly(bool signedIn)
    {
        if (!signedIn)
        {
            _users.User = null;
        }

        ReportRunOutcome outcome = await _service.PreviewAsync(new ReportDefinition { Source = "FacultyRoster" }, new ReportRunRequest(), Ct);

        AssertFails(outcome, ReportRunStatus.Forbidden);
    }

    // The run log

    [Fact]
    public async Task Run_IsLoggedWithTheUsersOwnValues()
    {
        await AddLeaveAsync(r => r.Parameters =
        [
            new ReportParameter { Name = "LoginId", Binding = ReportParameterBinding.CurrentUserLoginId },
            new ReportParameter { Name = "Year", Type = ReportParameterType.Number },
            new ReportParameter { Name = "From", Type = ReportParameterType.Date },
            new ReportParameter { Name = "Paid", Type = ReportParameterType.Boolean },
            new ReportParameter { Name = "Types", Type = ReportParameterType.MultiChoice, ChoicesList = """["Sick","Vacation"]""" },
        ]);
        _catalog.Sources[1] = _catalog.Sources[1] with
        {
            Parameters =
            [
                .. _catalog.Sources[1].Parameters,
                FakeReportSourceCatalog.Parameter("From"),
                FakeReportSourceCatalog.Parameter("Paid"),
                FakeReportSourceCatalog.Parameter("Types"),
            ],
        };
        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada")), FakeReportQueryExecutor.Row(("Name", "Bo")));

        Ok(await RunAsync(LeaveSlug, new ReportRunRequest
        {
            Parameters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Year"] = ["2026.0"],
                ["From"] = ["2026-01-31"],
                ["Paid"] = ["true"],
                ["Types"] = ["Vacation", "Sick"],
            },
        }));

        ReportRunLog log = Assert.Single(_context.ReportRunLog);
        Assert.Equal(2, log.ReportId);
        Assert.Equal(ReportRunAction.Run, log.Action);
        Assert.Equal("jdoe", log.LoginId);
        Assert.Equal(Now.UtcDateTime, log.RanOn);
        Assert.Equal("""{"From":"2026-01-31","Paid":true,"Types":["Vacation","Sick"],"Year":"2026.0"}""", log.Parameters);
        Assert.Equal(2, log.RowsReturned);
        Assert.Equal(15, log.DurationMs);
        Assert.Null(log.ErrorMessage);
    }

    [Fact]
    public async Task Run_DescribesTheReportAndTheUsersFiltersForDownloads()
    {
        await AddLeaveAsync(r =>
        {
            r.SensitiveNotice = "  Confidential  ";
            r.PdfLandscape = true;
            r.Parameters =
            [
                new ReportParameter { Name = "LoginId", Binding = ReportParameterBinding.CurrentUserLoginId },
                new ReportParameter { Name = "Year", Label = "Fiscal year", Type = ReportParameterType.Number, DisplayOrder = 1 },
                new ReportParameter { Name = "From", Label = "From", Type = ReportParameterType.Date, DisplayOrder = 2 },
                new ReportParameter { Name = "Paid", Type = ReportParameterType.Boolean, DisplayOrder = 3 },
                new ReportParameter { Name = "Types", Label = "Leave types", Type = ReportParameterType.MultiChoice, ChoicesList = """[{"value":"S","label":"Sick"},{"value":"V","label":"Vacation"}]""", DisplayOrder = 4 },
                new ReportParameter { Name = "Kind", Type = ReportParameterType.Choice, ChoicesList = """[{"value":"F","label":"Full time"}]""", DisplayOrder = 5 },
                new ReportParameter { Name = "Note", DisplayOrder = 6 },
            ];
        });
        _catalog.Sources[1] = _catalog.Sources[1] with
        {
            Parameters =
            [
                .. _catalog.Sources[1].Parameters,
                FakeReportSourceCatalog.Parameter("From"),
                FakeReportSourceCatalog.Parameter("Paid"),
                FakeReportSourceCatalog.Parameter("Types"),
                FakeReportSourceCatalog.Parameter("Kind"),
                FakeReportSourceCatalog.Parameter("Note"),
            ],
        };

        ReportRunOutcome outcome = await RunAsync(LeaveSlug, new ReportRunRequest
        {
            Parameters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Year"] = ["12345.5"],
                ["From"] = ["2026-01-31"],
                ["Paid"] = ["false"],
                ["Types"] = ["V", "s"],
                ["Kind"] = ["f"],
                ["Note"] = ["x"],
            },
        });

        Ok(outcome);
        ReportRunDetails details = Assert.IsType<ReportRunDetails>(outcome.Details);
        Assert.Equal("My leave", details.Title);
        Assert.Equal("Confidential", details.SensitiveNotice);
        Assert.True(details.PdfLandscape);
        Assert.Equal(
            [
                new ReportAppliedFilter { Label = "Fiscal year", Value = "12,345.5" },
                new ReportAppliedFilter { Label = "From", Value = "01/31/2026" },
                new ReportAppliedFilter { Label = "Paid", Value = "No" },
                new ReportAppliedFilter { Label = "Leave types", Value = "Vacation, Sick" },
                new ReportAppliedFilter { Label = "Kind", Value = "Full time" },
                new ReportAppliedFilter { Label = "Note", Value = "x" },
            ],
            details.Filters);
    }

    [Fact]
    public async Task Run_WithoutANotice_HasNoNoticeAndIsPortrait()
    {
        await AddRosterAsync(r => r.SensitiveNotice = " ");

        ReportRunOutcome outcome = await RunAsync(RosterSlug);

        ReportRunDetails details = Assert.IsType<ReportRunDetails>(outcome.Details);
        Assert.Null(details.SensitiveNotice);
        Assert.False(details.PdfLandscape);
        Assert.Empty(details.Filters);
    }

    [Fact]
    public async Task Run_WithoutUserValues_LogsNoParameters()
    {
        await AddLeaveAsync();

        Ok(await RunAsync(LeaveSlug));

        Assert.Null(Assert.Single(_context.ReportRunLog).Parameters);
    }

    [Fact]
    public async Task Run_WhoseLogCannotBeSaved_StillReturnsTheRows()
    {
        DbContextOptions<ReportBuilderDbContext> options = new DbContextOptionsBuilder<ReportBuilderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var failing = new FailingSaveContext(options);
        await using (var seed = new ReportBuilderDbContext(options))
        {
            seed.Reports.Add(new ReportDefinition
            {
                Id = 9,
                Slug = RosterSlug,
                Title = "Roster",
                Source = "FacultyRoster",
                Permissions = [new ReportPermission { ReportId = 9, Permission = ViewerPermission }],
                Columns = [Column("Name", 1)],
            });
            await seed.SaveChangesAsync(Ct);
        }

        AnswerRows(FakeReportQueryExecutor.Row(("Name", "Ada")));

        ReportRunOutcome outcome = await CreateService(failing).RunAsync(RosterSlug.ToString(), new ReportRunRequest(), ReportRunMode.Screen, Ct);

        Assert.Single(Ok(outcome).Rows);
    }
}
