using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// Runs the real ReportAdminService (with its production transaction handling) against SQLite,
/// which supports transactions, unlike the in-memory provider the other ReportBuilder tests use.
/// This proves a failed history write rolls the report mutation back too, not just that the
/// exception surfaces.
/// </summary>
public sealed class ReportAdminServiceRollbackTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ReportBuilderDbContext> _options;
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportPermissionLookup _permissions = new();
    private readonly FakeReportUserService _users = new();

    public ReportAdminServiceRollbackTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ReportBuilderDbContext>().UseSqlite(_connection).Options;
        using (var create = new ReportBuilderDbContext(_options))
        {
            create.Database.EnsureCreated();
        }

        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "FacultyRoster",
            Kind = ReportSourceKind.View,
            Columns = [FakeReportSourceCatalog.Column("Name")],
            Parameters = [],
            PagesInSql = true,
        });
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 16, 9, 30, 0, TimeSpan.Zero);
    }

    /// <summary>Not exercised by create, update or delete; throws if the test is wrong about that.</summary>
    private sealed class UnusedRunService : IReportRunService
    {
        public Task<ReportRunOutcome> RunAsync(string slug, ReportRunRequest request, ReportRunMode mode, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<ReportRunOutcome> PreviewAsync(ReportDefinition report, ReportRunRequest request, CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>Saves the mutation itself, then fails the history write that follows it.</summary>
    private sealed class FailingHistoryContext : ReportBuilderDbContext
    {
        private int _calls;

        public FailingHistoryContext(DbContextOptions<ReportBuilderDbContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _calls++;
            return _calls == 1 ? base.SaveChangesAsync(cancellationToken) : throw new DbUpdateException("history table is locked");
        }
    }

    private ReportAdminService CreateService(ReportBuilderDbContext context)
    {
        return new ReportAdminService(
            context, new ReportDefinitionValidator(_catalog, _permissions), _permissions, _users, new UnusedRunService(), new FixedClock());
    }

    private static ReportEditRequest Roster()
    {
        return new ReportEditRequest
        {
            Title = "Faculty roster",
            Source = "FacultyRoster",
            Columns = [new ReportColumnEdit { SourceColumn = "Name", Format = ReportColumnFormat.Text, Sortable = true, Visible = true }],
        };
    }

    [Fact]
    public async Task Create_WhenHistoryFailsToSave_RollsBackTheReportToo()
    {
        await using var failing = new FailingHistoryContext(_options);

        await Assert.ThrowsAsync<DbUpdateException>(() => CreateService(failing).CreateAsync(Roster(), Ct));

        await using var check = new ReportBuilderDbContext(_options);
        Assert.Empty(await check.Reports.ToListAsync(Ct));
    }

    [Fact]
    public async Task Update_WhenHistoryFailsToSave_RollsBackTheEditToo()
    {
        int id;
        await using (var seed = new ReportBuilderDbContext(_options))
        {
            var created = CreateService(seed);
            id = (await created.CreateAsync(Roster(), Ct)).Value!.Id;
        }

        await using var failing = new FailingHistoryContext(_options);
        ReportEditRequest edit = Roster() with { Title = "Renamed", LastModifiedOn = (await CreateService(failing).GetAsync(id, Ct))!.ModifiedOn };

        await Assert.ThrowsAsync<DbUpdateException>(() => CreateService(failing).UpdateAsync(id, edit, Ct));

        await using var check = new ReportBuilderDbContext(_options);
        Assert.Equal("Faculty roster", (await check.Reports.SingleAsync(r => r.Id == id, Ct)).Title);
    }
}
