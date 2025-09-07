using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.ClinicalScheduler.Constants;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler
{
    public class ScheduleAuditServiceTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<ILogger<ScheduleAuditService>> _mockLogger;
        private readonly ScheduleAuditService _service;
        public ScheduleAuditServiceTest()
        {
            _mockLogger = new Mock<ILogger<ScheduleAuditService>>();
            _service = new ScheduleAuditService(Context, _mockLogger.Object);
        }

        [Fact]
        public async Task LogInstructorAddedAsync_CreatesAuditEntry()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekId = 1;
            var modifiedBy = "modifier456";

            // Act
            var result = await _service.LogInstructorAddedAsync(mothraId, rotationId, weekId, modifiedBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mothraId, result.MothraId);
            Assert.Equal(rotationId, result.RotationId);
            Assert.Equal(weekId, result.WeekId);
            Assert.Equal(modifiedBy, result.ModifiedBy);
            Assert.Equal(ScheduleAuditActions.InstructorAdded, result.Action);
            Assert.True(result.TimeStamp > DateTime.MinValue);

            // Verify entry was saved to database
            var savedEntry = await Context.ScheduleAudits.FindAsync(result.ScheduleAuditId);
            Assert.NotNull(savedEntry);
            Assert.Equal(result.Action, savedEntry.Action);
        }

        [Fact]
        public async Task LogInstructorRemovedAsync_CreatesAuditEntry()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekId = 1;
            var modifiedBy = "modifier456";

            // Act
            var result = await _service.LogInstructorRemovedAsync(mothraId, rotationId, weekId, modifiedBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ScheduleAuditActions.InstructorRemoved, result.Action);
            Assert.Equal(mothraId, result.MothraId);
            Assert.Equal(rotationId, result.RotationId);
            Assert.Equal(weekId, result.WeekId);
            Assert.Equal(modifiedBy, result.ModifiedBy);
        }

        [Fact]
        public async Task LogPrimaryEvaluatorSetAsync_CreatesAuditEntry()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekId = 1;
            var modifiedBy = "modifier456";

            // Act
            var result = await _service.LogPrimaryEvaluatorSetAsync(mothraId, rotationId, weekId, modifiedBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ScheduleAuditActions.PrimaryEvaluatorSet, result.Action);
            Assert.Equal(mothraId, result.MothraId);
            Assert.Equal(rotationId, result.RotationId);
            Assert.Equal(weekId, result.WeekId);
            Assert.Equal(modifiedBy, result.ModifiedBy);
        }

        [Fact]
        public async Task LogPrimaryEvaluatorUnsetAsync_CreatesAuditEntry()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekId = 1;
            var modifiedBy = "modifier456";

            // Act
            var result = await _service.LogPrimaryEvaluatorUnsetAsync(mothraId, rotationId, weekId, modifiedBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ScheduleAuditActions.PrimaryEvaluatorUnset, result.Action);
        }

        [Fact]
        public async Task GetInstructorScheduleAuditHistoryAsync_ReturnsAuditEntriesForSchedule()
        {
            // Arrange
            // Create audit entries without InstructorScheduleId since it's no longer in the model
            var auditEntry1 = CreateAuditEntry("test123", 1, 1, "modifier", ScheduleAuditActions.InstructorAdded);
            var auditEntry2 = CreateAuditEntry("test123", 1, 1, "modifier", ScheduleAuditActions.PrimaryEvaluatorSet);
            var auditEntry3 = CreateAuditEntry("other456", 2, 1, "modifier", ScheduleAuditActions.InstructorAdded); // Different rotation

            await Context.ScheduleAudits.AddRangeAsync(auditEntry1, auditEntry2, auditEntry3);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetRotationWeekAuditHistoryAsync(1, 1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, entry => entry.Action == ScheduleAuditActions.InstructorAdded && entry.MothraId == "test123");
            Assert.Contains(result, entry => entry.Action == ScheduleAuditActions.PrimaryEvaluatorSet && entry.MothraId == "test123");

            // Should be ordered by timestamp descending (most recent first)
            Assert.True(result[0].TimeStamp >= result[1].TimeStamp);
        }

        [Fact]
        public async Task GetRotationWeekAuditHistoryAsync_ReturnsAuditEntriesForRotationWeek()
        {
            // Arrange
            var rotationId = 1;
            var weekId = 1;
            var auditEntry1 = CreateAuditEntry("test123", rotationId, weekId, "modifier", ScheduleAuditActions.InstructorAdded);
            var auditEntry2 = CreateAuditEntry("test456", rotationId, weekId, "modifier", ScheduleAuditActions.InstructorRemoved);
            var auditEntry3 = CreateAuditEntry("test789", 2, weekId, "modifier", ScheduleAuditActions.InstructorAdded); // Different rotation
            var auditEntry4 = CreateAuditEntry("test321", rotationId, 2, "modifier", ScheduleAuditActions.InstructorAdded); // Different week

            await Context.ScheduleAudits.AddRangeAsync(auditEntry1, auditEntry2, auditEntry3, auditEntry4);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetRotationWeekAuditHistoryAsync(rotationId, weekId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, entry =>
            {
                Assert.Equal(rotationId, entry.RotationId);
                Assert.Equal(weekId, entry.WeekId);
            });
            Assert.Contains(result, entry => entry.MothraId == "test123");
            Assert.Contains(result, entry => entry.MothraId == "test456");
        }

        [Fact]
        public async Task GetInstructorScheduleAuditHistoryAsync_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var nonExistentScheduleId = 999;

            // Act
            var result = await _service.GetInstructorScheduleAuditHistoryAsync(nonExistentScheduleId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRotationWeekAuditHistoryAsync_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var nonExistentRotationId = 999;
            var nonExistentWeekId = 999;

            // Act
            var result = await _service.GetRotationWeekAuditHistoryAsync(nonExistentRotationId, nonExistentWeekId);

            // Assert
            Assert.Empty(result);
        }

        private ScheduleAudit CreateAuditEntry(string mothraId, int rotationId, int weekId, string modifiedBy, string action)
        {
            return new ScheduleAudit
            {
                MothraId = mothraId,
                RotationId = rotationId,
                WeekId = weekId,
                Action = action,
                ModifiedBy = modifiedBy,
                TimeStamp = DateTime.UtcNow,
                Area = "ClinicalScheduler"
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
