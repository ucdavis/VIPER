using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// Saving reports: checked first, stored as the catalog and RAPS spell things, guarded against
/// overwriting someone else's save, soft-deleted, and copied into the history on every change.
/// </summary>
public sealed class ReportAdminServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 30, 0, TimeSpan.Zero);

    private readonly ReportBuilderDbContext _context;
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportPermissionLookup _permissions = new();
    private readonly FakeReportUserService _users = new();
    private readonly PreviewRecorder _runs = new();
    private readonly ReportAdminService _service;

    public ReportAdminServiceTests()
    {
        DbContextOptions<ReportBuilderDbContext> options = new DbContextOptionsBuilder<ReportBuilderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ReportBuilderDbContext(options);
        _service = new ReportAdminService(
            _context, new ReportDefinitionValidator(_catalog, _permissions), _permissions, _users, _runs, new FixedClock());
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "FacultyRoster",
            Kind = ReportSourceKind.View,
            Columns = [FakeReportSourceCatalog.Column("Name"), FakeReportSourceCatalog.Column("Dept")],
            Parameters = [],
            PagesInSql = true,
        });
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "usp_Leave",
            Kind = ReportSourceKind.Procedure,
            Columns = [FakeReportSourceCatalog.Column("Name")],
            Parameters = [FakeReportSourceCatalog.Parameter("LoginId"), FakeReportSourceCatalog.Parameter("Kind")],
            PagesInSql = false,
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

    /// <summary>Records the report a preview was asked to run.</summary>
    private sealed class PreviewRecorder : IReportRunService
    {
        public ReportDefinition? Previewed { get; private set; }

        public Task<ReportRunOutcome> RunAsync(string slug, ReportRunRequest request, ReportRunMode mode, CancellationToken ct)
        {
            throw new NotSupportedException("The admin service only previews.");
        }

        public Task<ReportRunOutcome> PreviewAsync(ReportDefinition report, ReportRunRequest request, CancellationToken ct)
        {
            Previewed = report;
            return Task.FromResult(new ReportRunOutcome { Status = ReportRunStatus.Ok });
        }
    }

    private static ReportEditRequest Roster()
    {
        return new ReportEditRequest
        {
            Title = " Faculty roster ",
            Description = " ",
            Area = "Personnel",
            Source = "facultyroster",
            Paginate = true,
            PageSize = 50,
            AllowExcel = true,
            SensitiveNotice = " Confidential ",
            OrderBy = "name",
            Permissions = ["svmsecure.personnel"],
            Columns =
            [
                new ReportColumnEdit { SourceColumn = "name", Label = " ", Format = ReportColumnFormat.Text, Sortable = true, Visible = true },
                new ReportColumnEdit { SourceColumn = "Dept", Label = "Department", Format = ReportColumnFormat.Text, Sortable = false, Visible = true },
            ],
            Parameters =
            [
                new ReportParameterEdit
                {
                    Name = "dept",
                    Label = "Department",
                    Type = ReportParameterType.MultiChoice,
                    Operator = ReportFilterOperator.In,
                    ChoicesList = [new ReportChoice { Value = " VME ", Label = " " }],
                    ChoicesSource = "Ignored",
                    Binding = ReportParameterBinding.UserInput,
                },
                new ReportParameterEdit { Name = "Name", Type = ReportParameterType.Text, Binding = ReportParameterBinding.UserInput },
            ],
        };
    }

    private async Task<ReportAdminDefinition> CreateAsync(ReportEditRequest? request = null)
    {
        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.CreateAsync(request ?? Roster(), Ct);
        Assert.Equal(ReportAdminStatus.Ok, outcome.Status);
        return Assert.IsType<ReportAdminDefinition>(outcome.Value);
    }

    private ReportDefinition Stored(int id)
    {
        return _context.Reports.AsNoTracking()
            .Include(r => r.Columns)
            .Include(r => r.Parameters)
            .Include(r => r.Permissions)
            .Single(r => r.Id == id);
    }

    // Creating

    [Fact]
    public async Task Create_StoresTheReportTidiedAndSpelledAsTheCatalogDoes()
    {
        ReportAdminDefinition created = await CreateAsync();

        ReportDefinition stored = Stored(created.Id);
        Assert.Equal(("Faculty roster", null, "FacultyRoster", "Confidential", "Name"), (stored.Title, stored.Description, stored.Source, stored.SensitiveNotice, stored.OrderBy));
        Assert.NotEqual(Guid.Empty, stored.Slug);
        Assert.Equal(["SVMSecure.Personnel"], stored.Permissions.Select(p => p.Permission));
        (string?, string?, int)[] columns = [("Name", null, 1), ("Dept", "Department", 2)];
        Assert.Equal(columns, stored.Columns.OrderBy(c => c.DisplayOrder).Select(c => (c.SourceColumn, c.Label, c.DisplayOrder)));
    }

    [Fact]
    public async Task Create_StoresParametersWithOnlyTheSettingsThatApply()
    {
        ReportAdminDefinition created = await CreateAsync();

        ReportParameter dept = Stored(created.Id).Parameters.Single(p => p.DisplayOrder == 1);
        Assert.Equal("Dept", dept.Name);
        Assert.Null(dept.ChoicesSource);
        Assert.Equal("""[{"value":"VME","label":"VME"}]""", dept.ChoicesList);
        Assert.Equal(ReportFilterOperator.Equals, Stored(created.Id).Parameters.Single(p => p.DisplayOrder == 2).Operator);
    }

    [Fact]
    public async Task Create_RecordsWhoAndWhen()
    {
        ReportAdminDefinition created = await CreateAsync();

        Assert.Equal((Now.DateTime, "jdoe"), (created.CreatedOn, created.CreatedBy));
        Assert.Equal((Now.DateTime, "jdoe"), (created.ModifiedOn, created.ModifiedBy));
        Assert.Equal(Now.DateTime, created.Report.LastModifiedOn);
        ReportDefinitionHistory history = Assert.Single(_context.ReportHistory);
        Assert.Equal((created.Id, ReportHistoryAction.Created, "jdoe"), (history.ReportId, history.Action, history.ChangedBy));
    }

    [Fact]
    public async Task Create_Invalid_SavesNothing()
    {
        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.CreateAsync(Roster() with { Title = "" }, Ct);

        Assert.Equal(ReportAdminStatus.Invalid, outcome.Status);
        Assert.Equal(["Enter a title."], outcome.Errors);
        Assert.Empty(_context.Reports);
    }

    [Fact]
    public async Task Create_ProcedureParameters_HaveNoOperatorAndCurrentUserOnesAreText()
    {
        ReportAdminDefinition created = await CreateAsync(new ReportEditRequest
        {
            Title = "Leave",
            Source = "usp_Leave",
            Columns = [new ReportColumnEdit { SourceColumn = "Name", Format = ReportColumnFormat.Text, Sortable = true, Visible = true }],
            Parameters =
            [
                new ReportParameterEdit
                {
                    Name = "loginid",
                    Type = ReportParameterType.Choice,
                    Required = true,
                    DefaultValue = "x",
                    Binding = ReportParameterBinding.CurrentUserLoginId,
                },
                new ReportParameterEdit
                {
                    Name = "Kind",
                    Type = ReportParameterType.Choice,
                    Operator = ReportFilterOperator.Equals,
                    ChoicesSource = " FacultyRoster ",
                    Binding = ReportParameterBinding.UserInput,
                },
            ],
        });

        List<ReportParameter> parameters = [.. Stored(created.Id).Parameters.OrderBy(p => p.DisplayOrder)];
        Assert.Equal<(string?, ReportParameterType?, bool?, string?, ReportFilterOperator?)>(("LoginId", ReportParameterType.Text, false, null, null), (parameters[0].Name, parameters[0].Type, parameters[0].Required, parameters[0].DefaultValue, parameters[0].Operator));
        Assert.Equal(("FacultyRoster", (ReportFilterOperator?)null), (parameters[1].ChoicesSource, parameters[1].Operator));
    }

    // Reading

    [Fact]
    public async Task Get_ReturnsTheFormsFieldsInOrder()
    {
        ReportAdminDefinition created = await CreateAsync();

        ReportAdminDefinition? read = await _service.GetAsync(created.Id, Ct);

        Assert.NotNull(read);
        Assert.Equal(["Name", "Dept"], read.Report.Columns.Select(c => c.SourceColumn));
        Assert.Equal(["Dept", "Name"], read.Report.Parameters.Select(p => p.Name));
        Assert.Equal([new ReportChoice { Value = "VME", Label = "VME" }], read.Report.Parameters[0].ChoicesList);
        Assert.Empty(read.MissingPermissions);
    }

    [Fact]
    public async Task Get_Unknown_IsNull()
    {
        Assert.Null(await _service.GetAsync(999, Ct));
    }

    [Fact]
    public async Task Get_ReportWithEmptySettings_UsesTheRunnersDefaults()
    {
        _context.Reports.Add(new ReportDefinition
        {
            Id = 7,
            Title = "Old",
            Source = "FacultyRoster",
            Columns = [new ReportColumn { SourceColumn = null, DisplayOrder = 1 }],
            Parameters = [new ReportParameter { Name = null }],
        });
        await _context.SaveChangesAsync(Ct);

        ReportAdminDefinition read = (await _service.GetAsync(7, Ct))!;

        Assert.Equal((true, false, false), (read.Report.Paginate, read.Report.PdfLandscape, read.Report.OrderByDescending));
        Assert.Equal(("", ReportColumnFormat.Text), (read.Report.Columns[0].SourceColumn, read.Report.Columns[0].Format));
        Assert.Equal(("", ReportParameterType.Text, ReportParameterBinding.UserInput, false), (read.Report.Parameters[0].Name, read.Report.Parameters[0].Type, read.Report.Parameters[0].Binding, read.Report.Parameters[0].Required));
    }

    [Fact]
    public async Task List_FlagsPermissionsRapsNoLongerHasAndHidesDeletedReports()
    {
        ReportAdminDefinition kept = await CreateAsync();
        ReportAdminDefinition removed = await CreateAsync(Roster() with { Title = "Removed" });
        await _service.DeleteAsync(removed.Id, Ct);
        _permissions.Names.Remove("SVMSecure.Personnel");

        IReadOnlyList<ReportAdminListItem> live = await _service.ListAsync(includeDeleted: false, Ct);
        IReadOnlyList<ReportAdminListItem> all = await _service.ListAsync(includeDeleted: true, Ct);

        ReportAdminListItem item = Assert.Single(live);
        Assert.Equal((kept.Id, "Faculty roster", "FacultyRoster"), (item.Id, item.Title, item.Source));
        Assert.Equal(["SVMSecure.Personnel"], item.Permissions);
        Assert.Equal(["SVMSecure.Personnel"], item.MissingPermissions);
        Assert.Equal(2, all.Count);
    }

    // Updating

    [Fact]
    public async Task Update_ReplacesColumnsParametersAndPermissions()
    {
        ReportAdminDefinition created = await CreateAsync();
        _permissions.Names.Add("SVMSecure.Personnel.Reports");

        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.UpdateAsync(created.Id, created.Report with
        {
            Title = "Roster",
            Columns = [new ReportColumnEdit { SourceColumn = "Dept", Format = ReportColumnFormat.Text, Sortable = true, Visible = true }],
            Parameters = [],
            Permissions = ["SVMSecure.Personnel.Reports", "SVMSecure.Personnel"],
        }, Ct);

        Assert.Equal(ReportAdminStatus.Ok, outcome.Status);
        ReportDefinition stored = Stored(created.Id);
        Assert.Equal(("Roster", "Dept"), (stored.Title, Assert.Single(stored.Columns).SourceColumn));
        Assert.Empty(stored.Parameters);
        Assert.Equal(["SVMSecure.Personnel", "SVMSecure.Personnel.Reports"], stored.Permissions.Select(p => p.Permission).Order().ToArray());
    }

    [Fact]
    public async Task Update_DropsPermissionsNoLongerWanted()
    {
        ReportAdminDefinition created = await CreateAsync();

        await _service.UpdateAsync(created.Id, created.Report with { Permissions = [] }, Ct);

        Assert.Empty(Stored(created.Id).Permissions);
    }

    [Fact]
    public async Task Update_RecordsTheChange()
    {
        ReportAdminDefinition created = await CreateAsync();

        await _service.UpdateAsync(created.Id, created.Report with { Title = "Roster" }, Ct);

        IReadOnlyList<ReportHistoryItem>? history = await _service.GetHistoryAsync(created.Id, Ct);
        Assert.Equal([ReportHistoryAction.Updated, ReportHistoryAction.Created], history!.Select(h => h.Action));
    }

    [Fact]
    public async Task Update_FromAnOlderCopy_IsAConflict()
    {
        ReportAdminDefinition created = await CreateAsync();

        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.UpdateAsync(
            created.Id, created.Report with { Title = "Mine", LastModifiedOn = Now.DateTime.AddMinutes(-5) }, Ct);

        Assert.Equal(ReportAdminStatus.Conflict, outcome.Status);
        Assert.Contains("Someone else saved this report", Assert.Single(outcome.Errors));
        Assert.Equal("Faculty roster", Stored(created.Id).Title);
    }

    [Fact]
    public async Task Update_Invalid_ChangesNothing()
    {
        ReportAdminDefinition created = await CreateAsync();

        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.UpdateAsync(created.Id, created.Report with { Source = "Dropped" }, Ct);

        Assert.Equal(ReportAdminStatus.Invalid, outcome.Status);
        Assert.Equal("FacultyRoster", Stored(created.Id).Source);
    }

    [Fact]
    public async Task Update_Unknown_IsNotFound()
    {
        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.UpdateAsync(999, Roster(), Ct);

        Assert.Equal(ReportAdminStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task Update_Deleted_MustBeRestoredFirst()
    {
        ReportAdminDefinition created = await CreateAsync();
        ReportAdminDefinition deleted = (await _service.DeleteAsync(created.Id, Ct)).Value!;

        ReportAdminOutcome<ReportAdminDefinition> outcome = await _service.UpdateAsync(created.Id, deleted.Report, Ct);

        Assert.Equal(["Restore the report before editing it."], outcome.Errors);
    }

    // Deleting and restoring

    [Fact]
    public async Task Delete_HidesTheReportAndRestoreBringsItBack()
    {
        ReportAdminDefinition created = await CreateAsync();

        ReportAdminDefinition deleted = (await _service.DeleteAsync(created.Id, Ct)).Value!;
        ReportAdminDefinition restored = (await _service.RestoreAsync(created.Id, Ct)).Value!;

        Assert.Equal((Now.DateTime, "jdoe"), (deleted.DeletedOn, deleted.DeletedBy));
        Assert.Equal(((DateTime?)null, (string?)null), (restored.DeletedOn, restored.DeletedBy));
        IReadOnlyList<ReportHistoryItem>? history = await _service.GetHistoryAsync(created.Id, Ct);
        Assert.Equal(
            [ReportHistoryAction.Restored, ReportHistoryAction.Deleted, ReportHistoryAction.Created],
            history!.OrderByDescending(h => h.Id).Select(h => h.Action));
    }

    [Fact]
    public async Task Delete_Twice_RecordsItOnce()
    {
        ReportAdminDefinition created = await CreateAsync();

        await _service.DeleteAsync(created.Id, Ct);
        await _service.DeleteAsync(created.Id, Ct);
        await _service.RestoreAsync(created.Id, Ct);
        await _service.RestoreAsync(created.Id, Ct);

        Assert.Equal(3, await _context.ReportHistory.CountAsync(Ct));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteOrRestore_Unknown_IsNotFound(bool delete)
    {
        ReportAdminOutcome<ReportAdminDefinition> outcome = delete
            ? await _service.DeleteAsync(999, Ct)
            : await _service.RestoreAsync(999, Ct);

        Assert.Equal(ReportAdminStatus.NotFound, outcome.Status);
    }

    // History

    [Fact]
    public async Task HistoryVersion_ShowsTheReportAsItWas()
    {
        ReportAdminDefinition created = await CreateAsync();
        await _service.UpdateAsync(created.Id, created.Report with { Title = "Renamed" }, Ct);
        ReportHistoryItem first = (await _service.GetHistoryAsync(created.Id, Ct))!.Single(h => h.Action == ReportHistoryAction.Created);

        ReportHistoryVersion? version = await _service.GetHistoryVersionAsync(created.Id, first.Id, Ct);

        Assert.NotNull(version);
        Assert.Equal(("Faculty roster", ReportHistoryAction.Created), (version.Report?.Title, version.Version.Action));
        Assert.Equal(["Name", "Dept"], version.Report?.Columns.Select(c => c.SourceColumn));
    }

    [Fact]
    public async Task HistoryVersion_OfAnotherReportOrUnknown_IsNull()
    {
        ReportAdminDefinition created = await CreateAsync();
        int historyId = (await _context.ReportHistory.SingleAsync(Ct)).Id;

        Assert.Null(await _service.GetHistoryVersionAsync(created.Id + 1, historyId, Ct));
        Assert.Null(await _service.GetHistoryVersionAsync(created.Id, historyId + 1, Ct));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    public async Task HistoryVersion_WithAnUnreadableSnapshot_HasNoReport(string? snapshot)
    {
        ReportAdminDefinition created = await CreateAsync();
        _context.ReportHistory.Add(new ReportDefinitionHistory { ReportId = created.Id, Action = ReportHistoryAction.Updated, Snapshot = snapshot });
        await _context.SaveChangesAsync(Ct);
        int historyId = await _context.ReportHistory.MaxAsync(h => h.Id, Ct);

        ReportHistoryVersion? version = await _service.GetHistoryVersionAsync(created.Id, historyId, Ct);

        Assert.Null(version?.Report);
        Assert.Equal(ReportHistoryAction.Updated, version?.Version.Action);
    }

    [Fact]
    public async Task History_OfUnknownReport_IsNull()
    {
        Assert.Null(await _service.GetHistoryAsync(999, Ct));
    }

    // Preview

    [Fact]
    public async Task Preview_RunsTheUnsavedReportWithoutSavingIt()
    {
        ReportRunOutcome outcome = await _service.PreviewAsync(new ReportPreviewRequest { Report = Roster() }, Ct);

        Assert.Equal(ReportRunStatus.Ok, outcome.Status);
        Assert.Equal(("FacultyRoster", 2), (_runs.Previewed?.Source, _runs.Previewed?.Columns.Count));
        Assert.Empty(_context.Reports);
    }

    [Fact]
    public async Task Preview_Invalid_IsNotRun()
    {
        ReportRunOutcome outcome = await _service.PreviewAsync(new ReportPreviewRequest { Report = Roster() with { Columns = [] } }, Ct);

        Assert.Equal((ReportRunStatus.Invalid, "Show at least one column."), (outcome.Status, Assert.Single(outcome.Errors)));
        Assert.Null(_runs.Previewed);
    }
}
