using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Models.ClinicalScheduler;

namespace Viper.test.ClinicalScheduler
{
    public class ScheduleEditServiceTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<ISchedulePermissionService> _mockPermissionService;
        private readonly Mock<IScheduleAuditService> _mockAuditService;
        private readonly Mock<IUserHelper> _mockUserHelper;
        private readonly Mock<ILogger<ScheduleEditService>> _mockLogger;
        private readonly ScheduleEditService _service;
        public ScheduleEditServiceTest()
        {
            _mockPermissionService = new Mock<ISchedulePermissionService>();
            _mockAuditService = new Mock<IScheduleAuditService>();
            _mockUserHelper = new Mock<IUserHelper>();
            _mockLogger = new Mock<ILogger<ScheduleEditService>>();

            _service = new ScheduleEditService(
                Context,
                _mockPermissionService.Object,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockUserHelper.Object);
        }

        [Fact]
        public async Task AddInstructorAsync_WithValidData_CreatesInstructorSchedules()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekIds = new[] { 1, 2 };
            var user = TestDataBuilder.CreateUser("currentuser");

            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.AddInstructorAsync(mothraId, rotationId, weekIds, false);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, schedule =>
            {
                Assert.Equal(mothraId, schedule.MothraId);
                Assert.Equal(rotationId, schedule.RotationId);
                Assert.Contains(schedule.WeekId, weekIds);
                Assert.False(schedule.Evaluator);
            });

            // Verify audit logging was called
            _mockAuditService.Verify(x => x.LogInstructorAddedAsync(mothraId, rotationId, It.IsAny<int>(),
                user.MothraId, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddInstructorAsync_WithPrimaryEvaluator_ClearsPreviousPrimaryAndSetsNew()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekId = 1;
            var weekIds = new[] { weekId };
            var user = TestDataBuilder.CreateUser("currentuser");

            // Create existing primary evaluator
            var existingPrimary = TestDataBuilder.CreateInstructorSchedule("existing456", rotationId, weekId, true);
            await Context.InstructorSchedules.AddAsync(existingPrimary);
            await Context.SaveChangesAsync();

            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.AddInstructorAsync(mothraId, rotationId, weekIds, true);

            // Assert
            var newSchedule = result[0];
            Assert.True(newSchedule.Evaluator);

            // Check that existing primary evaluator was cleared
            var updatedExisting = await Context.InstructorSchedules.FindAsync(existingPrimary.InstructorScheduleId);
            Assert.False(updatedExisting!.Evaluator);

            // Verify audit logging
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync(mothraId, rotationId, weekId,
                user.MothraId, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddInstructorAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekIds = new[] { 1 };

            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddInstructorAsync(mothraId, rotationId, weekIds, false));
        }

        [Fact]
        public async Task AddInstructorAsync_WithScheduleConflicts_ThrowsInvalidOperationException()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekId = 1;
            var weekIds = new[] { weekId };
            var user = TestDataBuilder.CreateUser("currentuser");

            // Create conflicting schedule
            var conflictingSchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, 2, weekId, false);
            await Context.InstructorSchedules.AddAsync(conflictingSchedule);
            await Context.SaveChangesAsync();

            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddInstructorAsync(mothraId, rotationId, weekIds, false));
            Assert.Contains("scheduling conflicts", ex.Message);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_ValidSchedule_RemovesSuccessfully()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await Context.InstructorSchedules.AddAsync(schedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(schedule.InstructorScheduleId);

            // Assert
            Assert.True(result);
            var removedSchedule = await Context.InstructorSchedules.FindAsync(schedule.InstructorScheduleId);
            Assert.Null(removedSchedule);

            // Verify audit logging
            _mockAuditService.Verify(x => x.LogInstructorRemovedAsync("test123", 1, 1,
                user.MothraId, schedule.InstructorScheduleId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluatorWithOtherInstructors_RemovesSuccessfully()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 1, 1, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", 1, 1, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);
            var removedSchedule = await Context.InstructorSchedules.FindAsync(primarySchedule.InstructorScheduleId);
            Assert.Null(removedSchedule);

            // Verify primary evaluator unset audit log
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync("primary123", 1, 1,
                user.MothraId, primarySchedule.InstructorScheduleId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluatorWithoutOtherInstructors_ThrowsInvalidOperationException()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 1, 1, true);
            await Context.InstructorSchedules.AddAsync(primarySchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId));
            Assert.Contains("Cannot remove primary evaluator", ex.Message);
        }

        [Fact]
        public async Task SetPrimaryEvaluatorAsync_ValidSchedule_UpdatesSuccessfully()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await Context.InstructorSchedules.AddAsync(schedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.SetPrimaryEvaluatorAsync(schedule.InstructorScheduleId, true);

            // Assert
            Assert.True(result);
            var updatedSchedule = await Context.InstructorSchedules.FindAsync(schedule.InstructorScheduleId);
            Assert.True(updatedSchedule!.Evaluator);

            // Verify audit logging
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync("test123", 1, 1,
                user.MothraId, schedule.InstructorScheduleId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CanRemoveInstructorAsync_NonPrimaryEvaluator_ReturnsTrue()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await Context.InstructorSchedules.AddAsync(schedule);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.CanRemoveInstructorAsync(schedule.InstructorScheduleId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanRemoveInstructorAsync_PrimaryEvaluatorWithOtherInstructors_ReturnsTrue()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 1, 1, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", 1, 1, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.CanRemoveInstructorAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanRemoveInstructorAsync_PrimaryEvaluatorWithoutOtherInstructors_ReturnsFalse()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 1, 1, true);
            await Context.InstructorSchedules.AddAsync(primarySchedule);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.CanRemoveInstructorAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetScheduleConflictsAsync_WithConflicts_ReturnsConflictingSchedules()
        {
            // Arrange
            var mothraId = "test123";
            var weekIds = new[] { 1, 2 };
            var conflictSchedule1 = TestDataBuilder.CreateInstructorSchedule(mothraId, 1, 1, false);
            var conflictSchedule2 = TestDataBuilder.CreateInstructorSchedule(mothraId, 2, 2, false);

            await Context.InstructorSchedules.AddRangeAsync(conflictSchedule1, conflictSchedule2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetScheduleConflictsAsync(mothraId, weekIds);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, schedule => Assert.Equal(mothraId, schedule.MothraId));
        }

        [Fact]
        public async Task GetScheduleConflictsAsync_WithExcludedRotation_ExcludesSpecifiedRotation()
        {
            // Arrange
            var mothraId = "test123";
            var weekIds = new[] { 1 };
            var excludeRotationId = 1;
            var includedSchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, excludeRotationId, 1, false);
            var excludedSchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, 2, 1, false);

            await Context.InstructorSchedules.AddRangeAsync(includedSchedule, excludedSchedule);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetScheduleConflictsAsync(mothraId, weekIds, excludeRotationId);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].RotationId); // Should only include rotation 2, not excluded rotation 1
        }


        [Fact]
        public async Task GetScheduleConflictsAsync_NoConflicts_ReturnsEmptyList()
        {
            // Arrange
            var service = new ScheduleEditService(Context, _mockPermissionService.Object, _mockAuditService.Object, _mockLogger.Object, _mockUserHelper.Object);

            // Create instructor schedule for different weeks (no conflicts)
            var scheduleNoConflict = TestDataBuilder.CreateInstructorSchedule("12345", 1, 5);
            Context.InstructorSchedules.Add(scheduleNoConflict);
            await Context.SaveChangesAsync();

            // Act - Check for conflicts on different weeks
            var result = await service.GetScheduleConflictsAsync("12345", new[] { 10, 15 }, cancellationToken: CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetScheduleConflictsAsync_WithMultipleConflicts_ReturnsAllConflictingSchedules()
        {
            // Arrange
            var service = new ScheduleEditService(Context, _mockPermissionService.Object, _mockAuditService.Object, _mockLogger.Object, _mockUserHelper.Object);

            // Create multiple conflicting schedules
            var schedule1 = TestDataBuilder.CreateInstructorSchedule("12345", 1, 10);
            var schedule2 = TestDataBuilder.CreateInstructorSchedule("12345", 2, 15);
            var schedule3 = TestDataBuilder.CreateInstructorSchedule("12345", 3, 20);
            Context.InstructorSchedules.AddRange(schedule1, schedule2, schedule3);
            await Context.SaveChangesAsync();

            // Act - Check for conflicts on all these weeks for a different rotation
            var result = await service.GetScheduleConflictsAsync("12345", new[] { 10, 15, 20 }, excludeRotationId: 4, cancellationToken: CancellationToken.None);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, s => s.RotationId == 1 && s.WeekId == 10);
            Assert.Contains(result, s => s.RotationId == 2 && s.WeekId == 15);
            Assert.Contains(result, s => s.RotationId == 3 && s.WeekId == 20);
        }

        [Fact]
        public async Task SetPrimaryEvaluatorAsync_SwitchPrimaryEvaluator_ClearsOldAndSetsNew()
        {
            // Arrange
            var rotationId = 1;
            var weekId = 1;

            // Create two instructor schedules for the same rotation/week
            var instructor1 = TestDataBuilder.CreateInstructorSchedule("test123", rotationId, weekId, true);  // Start as primary
            var instructor2 = TestDataBuilder.CreateInstructorSchedule("test456", rotationId, weekId, false); // Not primary

            Context.InstructorSchedules.AddRange(instructor1, instructor2);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Switch primary evaluator from instructor1 to instructor2
            var result = await _service.SetPrimaryEvaluatorAsync(instructor2.InstructorScheduleId, true);

            // Assert
            Assert.True(result);

            // Verify instructor1 is no longer primary
            var updatedInstructor1 = await Context.InstructorSchedules.FindAsync(instructor1.InstructorScheduleId);
            Assert.False(updatedInstructor1!.Evaluator);

            // Verify instructor2 is now primary
            var updatedInstructor2 = await Context.InstructorSchedules.FindAsync(instructor2.InstructorScheduleId);
            Assert.True(updatedInstructor2!.Evaluator);
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