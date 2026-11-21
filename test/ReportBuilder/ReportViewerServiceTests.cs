using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>The list of reports and a report's form: only reports the user may view, and only what the viewer needs.</summary>
public sealed class ReportViewerServiceTests : IDisposable
{
    private const string ViewerPermission = "SVMSecure.Personnel.Reports";
    private static readonly Guid RosterSlug = Guid.Parse("8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e");

    private readonly ReportBuilderDbContext _context;
    private readonly FakeReportUserService _users = new();
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportQueryExecutor _executor = new();
    private readonly ReportViewerService _service;

    public ReportViewerServiceTests()
    {
        DbContextOptions<ReportBuilderDbContext> options = new DbContextOptionsBuilder<ReportBuilderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ReportBuilderDbContext(options);
        _service = new ReportViewerService(
            _context, _users, new ReportChoiceService(_catalog, _executor), NullLogger<ReportViewerService>.Instance);
        _users.Permissions.Add(ViewerPermission);
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "DeptChoices",
            Kind = ReportSourceKind.View,
            Columns = [],
            Parameters = [],
            PagesInSql = true,
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class FakeDbException : DbException
    {
        public FakeDbException() : base("timeout")
        {
        }
    }

    private async Task AddAsync(params ReportDefinition[] reports)
    {
        foreach (ReportDefinition report in reports)
        {
            _context.Reports.Add(report);
        }

        await _context.SaveChangesAsync(Ct);
    }

    private static ReportDefinition Report(int id, string title, string? area, string? permission = ViewerPermission)
    {
        return new ReportDefinition
        {
            Id = id,
            Slug = id == 1 ? RosterSlug : Guid.NewGuid(),
            Title = title,
            Area = area,
            Description = $"About {title}",
            Source = "FacultyRoster",
            Permissions = permission is null ? [] : [new ReportPermission { ReportId = id, Permission = permission }],
        };
    }

    private Task<ReportDefinitionOutcome> DefinitionAsync(Guid? slug = null)
    {
        return _service.GetDefinitionAsync((slug ?? RosterSlug).ToString(), Ct);
    }

    [Fact]
    public async Task Catalog_ListsViewableReportsByAreaThenTitle()
    {
        ReportDefinition deleted = Report(5, "Deleted", "Personnel");
        deleted.DeletedOn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        await AddAsync(
            Report(1, "Roster", "Personnel"),
            Report(2, "Leave", "Personnel"),
            Report(3, "Courses", "Effort"),
            Report(4, "Other people's", "Personnel", "SVMSecure.Other"),
            Report(6, "Admins only", "Admin", null),
            deleted);

        IReadOnlyList<ReportCatalogItem> catalog = await _service.GetCatalogAsync(Ct);

        Assert.Equal(["Courses", "Leave", "Roster"], catalog.Select(r => r.Title));
        ReportCatalogItem roster = catalog[2];
        Assert.Equal(RosterSlug, roster.Slug);
        Assert.Equal("Personnel", roster.Area);
        Assert.Equal("About Roster", roster.Description);
    }

    [Fact]
    public async Task Catalog_ForAdmins_ListsEveryReport()
    {
        await AddAsync(Report(1, "Roster", "Personnel", "SVMSecure.Other"), Report(2, "Admins only", null, null));
        _users.Permissions.Add(ReportBuilderPermissions.Admin);

        Assert.Equal(2, (await _service.GetCatalogAsync(Ct)).Count);
    }

    [Fact]
    public async Task Catalog_NoOneSignedIn_IsEmpty()
    {
        await AddAsync(Report(1, "Roster", "Personnel"));
        _users.User = null;

        Assert.Empty(await _service.GetCatalogAsync(Ct));
    }

    [Fact]
    public async Task Definition_DescribesTheFormAndTable()
    {
        ReportDefinition report = Report(1, "Roster", "Personnel");
        report.SensitiveNotice = " Confidential ";
        report.PageSize = 50;
        report.AllowExcel = true;
        report.AllowPdf = true;
        report.OrderBy = " Name ";
        report.OrderByDescending = true;
        report.Columns =
        [
            new ReportColumn { SourceColumn = "Name", DisplayOrder = 1 },
            new ReportColumn { SourceColumn = "Salary", DisplayOrder = 2, Visible = false },
        ];
        report.Parameters =
        [
            new ReportParameter { Name = "Dept", Label = "Department", Type = ReportParameterType.Choice, Required = true, ChoicesSource = "DeptChoices", DisplayOrder = 2 },
            new ReportParameter { Name = "Name", DefaultValue = " Smith ", DisplayOrder = 1 },
            new ReportParameter { Name = "LoginId", Binding = ReportParameterBinding.CurrentUserLoginId },
        ];
        await AddAsync(report);
        _executor.Answer = _ => new ReportQueryResult
        {
            Rows = [FakeReportQueryExecutor.Row(("Value", "VME"), ("Label", "Medicine"))],
            Truncated = false,
        };

        ReportDefinitionOutcome outcome = await DefinitionAsync();

        Assert.Equal(ReportRunStatus.Ok, outcome.Status);
        ReportViewerDefinition definition = Assert.IsType<ReportViewerDefinition>(outcome.Definition);
        Assert.Equal(RosterSlug, definition.Slug);
        Assert.Equal("Roster", definition.Title);
        Assert.Equal("About Roster", definition.Description);
        Assert.Equal("Personnel", definition.Area);
        Assert.Equal("Confidential", definition.SensitiveNotice);
        Assert.True(definition.Paginate);
        Assert.Equal(50, definition.PageSize);
        Assert.True(definition.AllowExcel);
        Assert.True(definition.AllowPdf);
        Assert.Equal("Name", definition.SortBy);
        Assert.True(definition.SortDescending);
        Assert.Equal(["Name"], definition.Columns.Select(c => c.Name));
        Assert.Equal(
            [
                new ReportViewerParameter { Name = "Name", Label = "Name", Type = ReportParameterType.Text, Required = false, DefaultValue = "Smith" },
                new ReportViewerParameter
                {
                    Name = "Dept",
                    Label = "Department",
                    Type = ReportParameterType.Choice,
                    Required = true,
                    Choices = [new ReportChoice { Value = "VME", Label = "Medicine" }],
                },
            ],
            definition.Parameters,
            new ViewerParameterComparer());
    }

    [Fact]
    public async Task Definition_WithDefaultsLeftEmpty()
    {
        ReportDefinition report = Report(1, "Roster", null);
        report.AllowExcel = false;
        report.Parameters = [new ReportParameter { Name = "Dept", Type = ReportParameterType.MultiChoice, ChoicesSource = "Missing" }];
        await AddAsync(report);

        ReportViewerDefinition definition = Assert.IsType<ReportViewerDefinition>((await DefinitionAsync()).Definition);

        Assert.Null(definition.SensitiveNotice);
        Assert.Null(definition.SortBy);
        Assert.False(definition.SortDescending);
        Assert.False(definition.AllowExcel);
        Assert.False(definition.AllowPdf);
        Assert.Equal(ReportLimits.DefaultPageSize, definition.PageSize);
        Assert.Empty(definition.Columns);
        Assert.Empty(Assert.Single(definition.Parameters).Choices!);
    }

    [Fact]
    public async Task Definition_NoOneSignedIn_IsForbidden()
    {
        await AddAsync(Report(1, "Roster", null));
        _users.User = null;

        Assert.Equal(ReportRunStatus.Forbidden, (await DefinitionAsync()).Status);
    }

    [Fact]
    public async Task Definition_Unknown_IsNotFound()
    {
        Assert.Equal(ReportRunStatus.NotFound, (await DefinitionAsync(Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Definition_WithoutPermission_IsForbiddenAndLoadsNoChoices()
    {
        ReportDefinition report = Report(1, "Roster", null, "SVMSecure.Other");
        report.Parameters = [new ReportParameter { Name = "Dept", Type = ReportParameterType.Choice, ChoicesSource = "DeptChoices" }];
        await AddAsync(report);

        ReportDefinitionOutcome outcome = await DefinitionAsync();

        Assert.Equal(ReportRunStatus.Forbidden, outcome.Status);
        Assert.Null(outcome.Definition);
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task Definition_ChoicesFailing_IsUnavailable()
    {
        ReportDefinition report = Report(1, "Roster", null);
        report.Parameters = [new ReportParameter { Name = "Dept", Type = ReportParameterType.Choice, ChoicesSource = "DeptChoices" }];
        await AddAsync(report);
        _executor.Answer = _ => throw new FakeDbException();

        ReportDefinitionOutcome outcome = await DefinitionAsync();

        Assert.Equal(ReportRunStatus.Unavailable, outcome.Status);
        Assert.Equal(["This report's choices couldn't be loaded. Try again later."], outcome.Errors);
    }

    /// <summary>Compares parameters by value, including their choice lists.</summary>
    private sealed class ViewerParameterComparer : IEqualityComparer<ReportViewerParameter>
    {
        public bool Equals(ReportViewerParameter? x, ReportViewerParameter? y)
        {
            return x is not null && y is not null
                && (x with { Choices = null }) == (y with { Choices = null })
                && (x.Choices ?? []).SequenceEqual(y.Choices ?? [])
                && (x.Choices is null) == (y.Choices is null);
        }

        public int GetHashCode(ReportViewerParameter obj)
        {
            return obj.Name.GetHashCode(StringComparison.Ordinal);
        }
    }
}
