using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.EmailTemplates.Services;
using Viper.Services;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Runs the real ScheduleEditService (with its production transaction handling)
    /// against SQLite, which supports transactions, unlike the in-memory provider
    /// used by ScheduleEditServiceTest via TestableScheduleEditService. These tests
    /// prove a failed audit write rolls the schedule change back, not just that the
    /// exception surfaces.
    /// </summary>
    public class ScheduleEditServiceRollbackTest : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ClinicalSchedulerContext _context;
        private readonly IScheduleAuditService _mockAuditService;
        private readonly ScheduleEditService _service;
        private bool _disposed;

        public ScheduleEditServiceRollbackTest()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new ClinicalSchedulerContext(options);
            _context.Database.EnsureCreated();

            var gradYear = DateTime.Now.Year + 1;
            _context.Services.Add(TestDataBuilder.CreateService(1, "Cardiology"));
            _context.Rotations.Add(TestDataBuilder.CreateRotation(1, "Cardiology Rotation", 1));
            var (week, weekGradYear) = TestDataBuilder.CreateWeekScenario(1, gradYear);
            _context.Weeks.Add(week);
            _context.WeekGradYears.Add(weekGradYear);
            _context.Persons.Add(TestDataBuilder.CreatePerson("test123"));
            _context.SaveChanges();

            _mockAuditService = Substitute.For<IScheduleAuditService>();

            var permissionValidator = Substitute.For<IPermissionValidator>();
            permissionValidator.ValidateEditPermissionAndGetUserAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(TestDataBuilder.CreateUser("currentuser"));

            var gradYearService = Substitute.For<IGradYearService>();
            gradYearService.GetCurrentGradYearAsync().Returns(DateTime.Now.Year);

            var emailNotificationOptions = Substitute.For<IOptions<EmailNotificationSettings>>();
            emailNotificationOptions.Value.Returns(new EmailNotificationSettings());
            var emailSettingsOptions = Substitute.For<IOptions<EmailSettings>>();
            emailSettingsOptions.Value.Returns(new EmailSettings());

            _service = new ScheduleEditService(
                _context,
                _mockAuditService,
                Substitute.For<ILogger<ScheduleEditService>>(),
                Substitute.For<IEmailService>(),
                emailNotificationOptions,
                emailSettingsOptions,
                gradYearService,
                permissionValidator,
                Substitute.For<IEmailTemplateRenderer>());
        }

        [Fact]
        public async Task AddInstructorAsync_AuditWriteFails_RollsBackScheduleInsert()
        {
            _mockAuditService.LogInstructorAddedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Failed to create audit entry"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddInstructorAsync("test123", 1, new[] { 1 }, DateTime.Now.Year + 1, false, TestContext.Current.CancellationToken));

            // The insert committed nothing: the schedule row must not survive the rollback
            Assert.Empty(await _context.InstructorSchedules.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_AuditWriteFails_RollsBackDelete()
        {
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1);
            _context.InstructorSchedules.Add(schedule);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAuditService.LogInstructorRemovedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Failed to create audit entry"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RemoveInstructorScheduleAsync(schedule.InstructorScheduleId, TestContext.Current.CancellationToken));

            // The delete rolled back: no unaudited removals
            Assert.NotNull(await _context.InstructorSchedules.AsNoTracking()
                .FirstOrDefaultAsync(s => s.InstructorScheduleId == schedule.InstructorScheduleId, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task SetPrimaryEvaluatorAsync_AuditWriteFails_RollsBackEvaluatorFlag()
        {
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1);
            _context.InstructorSchedules.Add(schedule);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAuditService.LogPrimaryEvaluatorSetAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Failed to create audit entry"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SetPrimaryEvaluatorAsync(schedule.InstructorScheduleId, true, TestContext.Current.CancellationToken));

            // The flag update rolled back
            var reloaded = await _context.InstructorSchedules.AsNoTracking()
                .FirstAsync(s => s.InstructorScheduleId == schedule.InstructorScheduleId, TestContext.Current.CancellationToken);
            Assert.False(reloaded.Evaluator);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _context.Dispose();
                _connection.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
