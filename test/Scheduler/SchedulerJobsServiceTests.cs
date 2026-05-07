using Hangfire;
using Hangfire.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Scheduler.Models.Entities;
using Viper.Areas.Scheduler.Services;
using Viper.Classes.SQLContext;
using HangfireRecurringJobDto = Hangfire.Storage.RecurringJobDto;

namespace Viper.test.Scheduler
{
    public sealed class SchedulerJobsServiceTests : IDisposable
    {
        private readonly VIPERContext _context;
        private readonly Hangfire.JobStorage _hangfireStorage;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<SchedulerJobsService> _logger;
        private List<HangfireRecurringJobDto> _hangfireJobs = new();

        // Concrete method target so InvocationData round-trips have a real
        // MethodInfo to bind against.
        private static class FakeJob
        {
            public static void Run(string s) => _ = s;
        }

        public SchedulerJobsServiceTests()
        {
            _context = new VIPERContext(
                new DbContextOptionsBuilder<VIPERContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options);
            _hangfireStorage = Substitute.For<Hangfire.JobStorage>();
            _recurringJobManager = Substitute.For<IRecurringJobManager>();
            _logger = Substitute.For<ILogger<SchedulerJobsService>>();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private TestableService CreateSut() => new(_context, _hangfireStorage, _recurringJobManager, _logger, () => _hangfireJobs);

        private static HangfireRecurringJobDto BuildRecurringJob(
            string id,
            string cron = "0 * * * *",
            string queue = "default",
            string timeZoneId = "UTC")
        {
            var method = typeof(FakeJob).GetMethod(nameof(FakeJob.Run))!;
            return new HangfireRecurringJobDto
            {
                Id = id,
                Cron = cron,
                Queue = queue,
                TimeZoneId = timeZoneId,
                Job = new Job(typeof(FakeJob), method, new object[] { "arg1" }),
                NextExecution = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            };
        }

        // Mirrors the service's custom JSON payload shape so the resume path
        // can rehydrate a real Hangfire.Common.Job from a seeded marker.
        private static string SerializeJobPayload()
        {
            var method = typeof(FakeJob).GetMethod(nameof(FakeJob.Run))!;
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                TypeName = typeof(FakeJob).AssemblyQualifiedName,
                MethodName = method.Name,
                ParameterTypeNames = method.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray(),
                SerializedArgs = new[] { System.Text.Json.JsonSerializer.Serialize("arg1") },
            });
        }

        private async Task<SchedulerJobState> SeedMarkerAsync(string id)
        {
            var marker = new SchedulerJobState
            {
                RecurringJobId = id,
                Cron = "0 * * * *",
                Queue = "default",
                TimeZoneId = "UTC",
                JobTypeName = typeof(FakeJob).AssemblyQualifiedName ?? string.Empty,
                SerializedArgs = SerializeJobPayload(),
                PausedAt = DateTime.UtcNow,
                PausedBy = "tester",
                RowVersion = [1, 2, 3, 4],
            };
            _context.SchedulerJobStates.Add(marker);
            await _context.SaveChangesAsync();
            return marker;
        }

        // ───────────────────────── List ─────────────────────────

        [Fact]
        public async Task ListJobsAsync_ReturnsActiveAndPausedJobs()
        {
            _hangfireJobs = [BuildRecurringJob("job-active")];
            await SeedMarkerAsync("job-paused-only");

            var result = await CreateSut().ListJobsAsync();

            Assert.Equal(2, result.Count);
            var active = result.Single(r => r.Id == "job-active");
            Assert.False(active.IsPaused);
            Assert.NotNull(active.NextExecution);
            var paused = result.Single(r => r.Id == "job-paused-only");
            Assert.True(paused.IsPaused);
            Assert.Null(paused.NextExecution);
        }

        [Fact]
        public async Task ListJobsAsync_SplitBrainShowsAsPaused()
        {
            // Hangfire has the registration AND the marker exists - the marker
            // is intent and the reconciler will heal, but the operator UI
            // should reflect "paused".
            _hangfireJobs = [BuildRecurringJob("job-split")];
            await SeedMarkerAsync("job-split");

            var result = await CreateSut().ListJobsAsync();

            var dto = Assert.Single(result);
            Assert.True(dto.IsPaused);
            Assert.NotNull(dto.PausedAt);
        }

        [Fact]
        public async Task ListJobsAsync_FlagsSystemJobs()
        {
            _hangfireJobs = [BuildRecurringJob("__scheduler:reconcile")];

            var result = await CreateSut().ListJobsAsync();

            var dto = Assert.Single(result);
            Assert.True(dto.IsSystem);
        }

        // ───────────────────────── Pause ─────────────────────────

        [Fact]
        public async Task PauseJobAsync_HappyPath_AddsMarkerAndDeregisters()
        {
            _hangfireJobs = [BuildRecurringJob("job-1", cron: "*/5 * * * *", queue: "critical", timeZoneId: "UTC")];

            var result = await CreateSut().PauseJobAsync("job-1", "alice", expectedRowVersion: null);

            Assert.True(result.IsPaused);
            Assert.False(result.DeregistrationPending);
            _recurringJobManager.Received(1).RemoveIfExists("job-1");
            var marker = await _context.SchedulerJobStates.FirstOrDefaultAsync(s => s.RecurringJobId == "job-1");
            Assert.NotNull(marker);
            Assert.Equal("*/5 * * * *", marker!.Cron);
            Assert.Equal("critical", marker.Queue);
            Assert.Equal("alice", marker.PausedBy);
        }

        [Fact]
        public async Task PauseJobAsync_RejectsSystemJob()
        {
            await Assert.ThrowsAsync<SchedulerSystemJobProtectedException>(() =>
                CreateSut().PauseJobAsync("__scheduler:reconcile", "alice", expectedRowVersion: null));
            _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
        }

        [Fact]
        public async Task PauseJobAsync_Idempotent_WhenAlreadyPaused()
        {
            _hangfireJobs = []; // no Hangfire registration
            await SeedMarkerAsync("job-paused");

            var result = await CreateSut().PauseJobAsync("job-paused", "alice", expectedRowVersion: null);

            Assert.True(result.IsPaused);
            Assert.False(result.DeregistrationPending);
            _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
        }

        [Fact]
        public async Task PauseJobAsync_NotFound_WhenJobMissing()
        {
            _hangfireJobs = [];

            await Assert.ThrowsAsync<SchedulerJobNotFoundException>(() =>
                CreateSut().PauseJobAsync("nope", "alice", expectedRowVersion: null));
        }

        [Fact]
        public async Task PauseJobAsync_MarksDeregistrationPending_WhenRemoveThrows()
        {
            _hangfireJobs = [BuildRecurringJob("job-1")];
            _recurringJobManager
                .When(m => m.RemoveIfExists("job-1"))
                .Do(_ => throw new BackgroundJobClientException("storage down", new InvalidOperationException("inner")));

            var result = await CreateSut().PauseJobAsync("job-1", "alice", expectedRowVersion: null);

            Assert.True(result.IsPaused);
            Assert.True(result.DeregistrationPending);
            Assert.NotNull(await _context.SchedulerJobStates.FirstOrDefaultAsync(s => s.RecurringJobId == "job-1"));
        }

        // ───────────────────────── Resume ─────────────────────────

        [Fact]
        public async Task ResumeJobAsync_HappyPath_DeletesMarkerAndReregisters()
        {
            var marker = await SeedMarkerAsync("job-1");
            _hangfireJobs = [];

            var result = await CreateSut().ResumeJobAsync("job-1", marker.RowVersion);

            Assert.False(result.IsPaused);
            _recurringJobManager.Received(1).AddOrUpdate(
                "job-1",
                Arg.Any<Job>(),
                "0 * * * *",
                Arg.Any<RecurringJobOptions>());
            Assert.Null(await _context.SchedulerJobStates.FirstOrDefaultAsync(s => s.RecurringJobId == "job-1"));
        }

        [Fact]
        public async Task ResumeJobAsync_RejectsSystemJob()
        {
            await Assert.ThrowsAsync<SchedulerSystemJobProtectedException>(() =>
                CreateSut().ResumeJobAsync("__scheduler:reconcile", [1, 2]));
        }

        [Fact]
        public async Task ResumeJobAsync_NotFound_WhenNoMarkerAndNoRegistration()
        {
            _hangfireJobs = [];

            await Assert.ThrowsAsync<SchedulerJobNotFoundException>(() =>
                CreateSut().ResumeJobAsync("nope", [1, 2]));
        }

        [Fact]
        public async Task ResumeJobAsync_Idempotent_WhenAlreadyActive()
        {
            _hangfireJobs = [BuildRecurringJob("job-1")];

            var result = await CreateSut().ResumeJobAsync("job-1", [1, 2]);

            Assert.False(result.IsPaused);
            _recurringJobManager.DidNotReceive().AddOrUpdate(
                Arg.Any<string>(), Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
        }

        [Fact]
        public async Task ResumeJobAsync_ConcurrencyConflict_OnRowVersionMismatch()
        {
            await SeedMarkerAsync("job-1");

            await Assert.ThrowsAsync<SchedulerConcurrencyException>(() =>
                CreateSut().ResumeJobAsync("job-1", [9, 9, 9, 9]));
        }

        // ───────────────────────── Reconcile ─────────────────────────

        [Fact]
        public async Task ReconcileAsync_DeletesSystemNamespacedMarkers()
        {
            await SeedMarkerAsync("__scheduler:reconcile");
            _hangfireJobs = [];

            var outcome = await CreateSut().ReconcileAsync();

            Assert.Equal(1, outcome.SystemMarkersDeleted);
            Assert.Null(await _context.SchedulerJobStates.FirstOrDefaultAsync(s => s.RecurringJobId == "__scheduler:reconcile"));
        }

        [Fact]
        public async Task ReconcileAsync_HealsSplitBrainByRemovingRegistration()
        {
            await SeedMarkerAsync("job-1");
            _hangfireJobs = [BuildRecurringJob("job-1")];

            var outcome = await CreateSut().ReconcileAsync();

            Assert.Equal(1, outcome.SplitBrainHealed);
            _recurringJobManager.Received(1).RemoveIfExists("job-1");
        }

        [Fact]
        public async Task ReconcileAsync_LeavesNormalStateAlone()
        {
            await SeedMarkerAsync("paused-job");
            _hangfireJobs = [BuildRecurringJob("active-job")];

            var outcome = await CreateSut().ReconcileAsync();

            Assert.Equal(0, outcome.SplitBrainHealed);
            Assert.Equal(0, outcome.SystemMarkersDeleted);
            Assert.Equal(1, outcome.CorrectlyPaused);
            Assert.Equal(1, outcome.CorrectlyActive);
            _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
        }

        // Subclass that lets each test swap the Hangfire-side data without
        // mocking the full IStorageConnection extension-method surface.
        private sealed class TestableService : SchedulerJobsService
        {
            private readonly Func<List<HangfireRecurringJobDto>> _provider;

            public TestableService(
                VIPERContext context,
                Hangfire.JobStorage storage,
                IRecurringJobManager manager,
                ILogger<SchedulerJobsService> logger,
                Func<List<HangfireRecurringJobDto>> provider)
                : base(context, storage, manager, logger)
            {
                _provider = provider;
            }

            protected override List<HangfireRecurringJobDto> GetHangfireRecurringJobs()
                => _provider();
        }
    }
}
