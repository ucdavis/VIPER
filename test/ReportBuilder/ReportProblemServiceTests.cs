using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// The problems page: failed runs from the log, newest first and within a window, and every live
/// report checked against the catalog and RAPS as a save would check it.
/// </summary>
public sealed class ReportProblemServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);

    private readonly ReportBuilderDbContext _context;
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportPermissionLookup _permissions = new();
    private readonly ReportProblemService _service;

    public ReportProblemServiceTests()
    {
        DbContextOptions<ReportBuilderDbContext> options = ReportBuilderTestDb.CreateOptions();
        _context = new ReportBuilderDbContext(options);
        _service = new ReportProblemService(_context, new ReportDefinitionValidator(_catalog, _permissions), new FixedClock());
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "FacultyRoster",
            Kind = ReportSourceKind.View,
            Columns = [FakeReportSourceCatalog.Column("Name"), FakeReportSourceCatalog.Column("Dept")],
            Parameters = [],
            PagesInSql = true,
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static ReportDefinition Report(int id, string title, Action<ReportDefinition>? configure = null)
    {
        var report = new ReportDefinition
        {
            Id = id,
            Slug = Guid.NewGuid(),
            Title = title,
            Area = "Personnel",
            Source = "FacultyRoster",
            PageSize = 25,
            Permissions = [new ReportPermission { ReportId = id, Permission = "SVMSecure.Personnel" }],
            Columns = [new ReportColumn { ReportId = id, SourceColumn = "Name", Visible = true, DisplayOrder = 1 }],
        };
        configure?.Invoke(report);
        return report;
    }

    private static ReportRunLog Failure(int id, int reportId, int daysAgo, string message = "Invalid column name 'Dept'.")
    {
        return new ReportRunLog
        {
            Id = id,
            ReportId = reportId,
            Action = ReportRunAction.Run,
            LoginId = "jdoe",
            RanOn = Now.UtcDateTime.AddDays(-daysAgo),
            ErrorNumber = 207,
            ErrorMessage = message,
        };
    }

    private async Task SeedAsync(IEnumerable<ReportDefinition> reports, IEnumerable<ReportRunLog> logs)
    {
        foreach (ReportDefinition report in reports)
        {
            _context.Reports.Add(report);
        }

        foreach (ReportRunLog log in logs)
        {
            _context.ReportRunLog.Add(log);
        }

        await _context.SaveChangesAsync(Ct);
    }

    // Failed runs

    [Fact]
    public async Task Failures_AreNewestFirstWithTheirReport_AndSkipSuccessfulRuns()
    {
        await SeedAsync(
            [Report(1, "Roster"), Report(2, "Leave", r => r.DeletedOn = Now.UtcDateTime)],
            [
                Failure(10, 1, daysAgo: 3),
                Failure(11, 2, daysAgo: 1, "Timeout expired."),
                new ReportRunLog { Id = 12, ReportId = 1, RanOn = Now.UtcDateTime, RowsReturned = 5 },
            ]);

        IReadOnlyList<ReportFailure> failures = await _service.GetFailuresAsync(null, Ct);

        Assert.Equal([11, 10], failures.Select(f => f.Id));
        ReportFailure latest = failures[0];
        Assert.Equal(("Leave", 2, true), (latest.ReportTitle, latest.ReportId, latest.ReportDeleted));
        Assert.Equal(("Timeout expired.", 207, "jdoe"), (latest.ErrorMessage, latest.ErrorNumber, latest.LoginId));
        Assert.False(failures[1].ReportDeleted);
    }

    [Theory]
    [InlineData(null, new[] { 10, 11 })]
    [InlineData(1, new[] { 10 })]
    [InlineData(0, new[] { 10 })]
    [InlineData(7, new[] { 10, 11 })]
    public async Task Failures_WithinTheWindow_CountingToday(int? days, int[] expected)
    {
        await SeedAsync([Report(1, "Roster")], [Failure(10, 1, 0), Failure(11, 1, 6), Failure(12, 1, 400)]);

        IReadOnlyList<ReportFailure> failures = await _service.GetFailuresAsync(days, Ct);

        Assert.Equal(expected, failures.Select(f => f.Id));
    }

    [Fact]
    public async Task Failures_LookBackAtMostAYear()
    {
        await SeedAsync([Report(1, "Roster")], [Failure(10, 1, ReportLimits.MaxFailureDays - 1), Failure(11, 1, ReportLimits.MaxFailureDays)]);

        IReadOnlyList<ReportFailure> failures = await _service.GetFailuresAsync(10_000, Ct);

        Assert.Equal(10, Assert.Single(failures).Id);
    }

    [Fact]
    public async Task Failures_AreCapped()
    {
        await SeedAsync([Report(1, "Roster")], Enumerable.Range(1, ReportLimits.MaxFailures + 5).Select(id => Failure(id, 1, 0)));

        IReadOnlyList<ReportFailure> failures = await _service.GetFailuresAsync(null, Ct);

        Assert.Equal(ReportLimits.MaxFailures, failures.Count);
    }

    // Checking every report

    [Fact]
    public async Task Check_ListsOnlyLiveReportsWithProblems_ByAreaAndTitle()
    {
        await SeedAsync(
            [
                Report(1, "Roster"),
                Report(2, "Zeta", r => r.Columns = [new ReportColumn { ReportId = 2, SourceColumn = "Salary", Visible = true, DisplayOrder = 1 }]),
                Report(3, "Alpha", r => r.Source = "Dropped"),
                Report(4, "Gone", r => { r.Source = "Dropped"; r.DeletedOn = Now.UtcDateTime; }),
                Report(5, "Orphan", r => r.Permissions = [new ReportPermission { ReportId = 5, Permission = "SVMSecure.Retired" }]),
            ],
            []);

        ReportCheckResult result = await _service.CheckAsync(Ct);

        Assert.Equal(4, result.Checked);
        Assert.Equal(Now.UtcDateTime, result.CheckedOn);
        Assert.Equal(["Alpha", "Orphan", "Zeta"], result.Failing.Select(p => p.Title));
        Assert.Contains("'Dropped' isn't a view or procedure in the [report] schema.", result.Failing[0].Problems);
        Assert.Equal(["RAPS has no permission named 'SVMSecure.Retired'."], result.Failing[1].Problems);
        Assert.Equal(("FacultyRoster", "Personnel"), (result.Failing[2].Source, result.Failing[2].Area));
        Assert.Single(result.Failing[2].Problems);
    }

    [Fact]
    public async Task Check_WithNothingWrong_IsEmpty()
    {
        await SeedAsync([Report(1, "Roster")], []);

        ReportCheckResult result = await _service.CheckAsync(Ct);

        Assert.Equal(1, result.Checked);
        Assert.Empty(result.Failing);
    }
}
