using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.EmailTemplates.Services;
using Viper.Models.ClinicalScheduler;
using Viper.Services;

namespace Viper.test.ClinicalScheduler
{
    public class ScheduleEditServiceTest : IDisposable
    {
        private readonly Mock<ISchedulePermissionService> _mockPermissionService;
        private readonly Mock<IScheduleAuditService> _mockAuditService;
        private readonly Mock<ILogger<ScheduleEditService>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IOptions<EmailNotificationSettings>> _mockEmailNotificationOptions;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<IPermissionValidator> _mockPermissionValidator;
        private readonly Mock<IUserHelper> _mockUserHelper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailTemplateRenderer> _mockEmailTemplateRenderer;
        private readonly ClinicalSchedulerContext _context;
        private readonly TestableScheduleEditService _service;
        private bool _disposed = false;

        public ScheduleEditServiceTest()
        {
            // Create real in-memory database for Entity Framework operations
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ClinicalSchedulerContext(options);

            _mockPermissionService = new Mock<ISchedulePermissionService>();
            _mockAuditService = new Mock<IScheduleAuditService>();
            _mockLogger = new Mock<ILogger<ScheduleEditService>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailNotificationOptions = new Mock<IOptions<EmailNotificationSettings>>();
            _mockPermissionValidator = new Mock<IPermissionValidator>();
            _mockUserHelper = new Mock<IUserHelper>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEmailTemplateRenderer = new Mock<IEmailTemplateRenderer>();

            // Setup default email notification configuration for tests
            var emailNotificationSettings = new EmailNotificationSettings
            {
                PrimaryEvaluatorRemoved = new PrimaryEvaluatorRemovedNotificationSettings
                {
                    To = new List<string> { "test@ucdavis.edu" },
                    From = "testfrom@ucdavis.edu"
                }
            };
            _mockEmailNotificationOptions.Setup(x => x.Value).Returns(emailNotificationSettings);

            _mockGradYearService = new Mock<IGradYearService>();
            // Set up default for grad year service - use current year for testing
            var currentYear = DateTime.Now.Year;
            _mockGradYearService.Setup(x => x.GetCurrentGradYearAsync())
                .ReturnsAsync(currentYear);

            // Set up default permission validator to return a test user
            var defaultUser = TestDataBuilder.CreateUser("testuser");
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultUser);

            // Set up default audit service methods
            var defaultAudit = new ScheduleAudit { ScheduleAuditId = 1 };
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultAudit);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultAudit);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultAudit);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultAudit);

            // Set up default email service
            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Set up default user helper
            var defaultTestUser = TestDataBuilder.CreateUser("testuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(defaultTestUser);

            // Seed the context with required test data
            SeedTestData();

            _service = new TestableScheduleEditService(
                _context,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockEmailNotificationOptions.Object,
                _mockGradYearService.Object,
                _mockPermissionValidator.Object,
                _mockConfiguration.Object,
                _mockEmailTemplateRenderer.Object);
        }

        private void SeedTestData()
        {
            // Use centralized TestDataBuilder for consistent test data
            var currentYear = DateTime.Now.Year;
            var testGradYear = currentYear + 1;

            // Add basic services using TestDataBuilder
            var services = new[]
            {
                TestDataBuilder.CreateService(1, "Cardiology", "Cardio"),
                TestDataBuilder.CreateService(2, "Surgery", "Surg")
            };
            _context.Services.AddRange(services);

            // Add basic rotations using TestDataBuilder
            var rotations = new[]
            {
                TestDataBuilder.CreateRotation(1, "Cardiology Rotation", 1, "CARD"),
                TestDataBuilder.CreateRotation(2, "Surgery Rotation", 2, "SURG"),
                TestDataBuilder.CreateRotation(3, "Test Rotation 3", 1, "TR3")
            };
            _context.Rotations.AddRange(rotations);

            // Add basic weeks and week grad years using TestDataBuilder
            var weekData = new[]
            {
                TestDataBuilder.CreateWeekScenario(1, testGradYear, new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                TestDataBuilder.CreateWeekScenario(2, testGradYear, new DateTime(currentYear, 1, 8, 0, 0, 0, DateTimeKind.Utc)),
                TestDataBuilder.CreateWeekScenario(10, testGradYear, new DateTime(currentYear, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
                TestDataBuilder.CreateWeekScenario(15, testGradYear, new DateTime(currentYear, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
                TestDataBuilder.CreateWeekScenario(20, testGradYear, new DateTime(currentYear, 5, 1, 0, 0, 0, DateTimeKind.Utc))
            };

            foreach (var (week, weekGradYear) in weekData)
            {
                _context.Weeks.Add(week);
                _context.WeekGradYears.Add(weekGradYear);
            }

            // Add basic persons using TestDataBuilder
            var persons = new[]
            {
                TestDataBuilder.CreatePerson("test123", "Test", "User123"),
                TestDataBuilder.CreatePerson("test456", "Test", "User456"),
                TestDataBuilder.CreatePerson("existing456", "Existing", "User"),
                TestDataBuilder.CreatePerson("primary123", "Primary", "User"),
                TestDataBuilder.CreatePerson("other456", "Other", "User"),
                TestDataBuilder.CreatePerson("12345", "John", "Doe"),
                TestDataBuilder.CreatePerson("currentuser", "Current", "User"),
                TestDataBuilder.CreatePerson("testuser", "Test", "User")
            };
            _context.Persons.AddRange(persons);

            _context.SaveChanges();
        }

        [Fact]
        public async Task AddInstructorAsync_WithValidData_CreatesInstructorSchedules()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekIds = new[] { 1, 2 };
            var testYear = DateTime.Now.Year + 1;
            var user = TestDataBuilder.CreateUser("currentuser");

            // Override default permission validator to return the specific user for this test
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.AddInstructorAsync(mothraId, rotationId, weekIds, testYear, false, CancellationToken.None);

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
                user.MothraId, It.IsAny<CancellationToken>()), Times.Exactly(2));
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
            await _context.InstructorSchedules.AddAsync(existingPrimary);
            await _context.SaveChangesAsync();

            // Override default permission validator to return the specific user for this test
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - use current year + 1 for future year testing
            var testYear = DateTime.Now.Year + 1;
            var result = await _service.AddInstructorAsync(mothraId, rotationId, weekIds, testYear, true, CancellationToken.None);

            // Assert
            var newSchedule = result[0];
            Assert.True(newSchedule.Evaluator);

            // Check that existing primary evaluator was cleared
            var updatedExisting = await _context.InstructorSchedules.FindAsync(existingPrimary.InstructorScheduleId);
            Assert.False(updatedExisting!.Evaluator);

            // Verify audit logging
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync(mothraId, rotationId, weekId,
                "currentuser", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddInstructorAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var mothraId = "test123";
            var rotationId = 1;
            var weekIds = new[] { 1 };

            // Set up permission service to deny permission for this rotation
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Set up permission validator to throw UnauthorizedAccessException when permissions are checked
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert - use current year + 1 for future year testing
            var testYear = DateTime.Now.Year + 1;
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddInstructorAsync(mothraId, rotationId, weekIds, testYear, false, CancellationToken.None));
        }

        [Fact]
        public async Task AddInstructorAsync_WithEditOwnSchedulePermissionButWrongUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange - User has EditOwnSchedule permission but tries to edit someone else's schedule
            var targetMothraId = "other123";
            var currentUserMothraId = "current456";
            var rotationId = 1;
            var weekIds = new[] { 1 };

            // Set up that user doesn't have general edit permission for rotation
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Set up permission validator to throw UnauthorizedAccessException for mismatched user
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(rotationId, targetMothraId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException($"User {currentUserMothraId} does not have permission to edit rotation {rotationId} or their own schedule (target: {targetMothraId})"));

            // Act & Assert
            var testYear = DateTime.Now.Year + 1;
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddInstructorAsync(targetMothraId, rotationId, weekIds, testYear, false, CancellationToken.None));

            Assert.Contains("does not have permission", exception.Message);
        }

        [Fact]
        public async Task AddInstructorAsync_NoAuthenticatedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange - No authenticated user
            var targetMothraId = "target123";
            var rotationId = 1;
            var weekIds = new[] { 1 };

            // Set up permission validator to throw UnauthorizedAccessException for no authenticated user
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(rotationId, targetMothraId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("No authenticated user found"));

            // Act & Assert
            var testYear = DateTime.Now.Year + 1;
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddInstructorAsync(targetMothraId, rotationId, weekIds, testYear, false, CancellationToken.None));

            Assert.Contains("No authenticated user found", exception.Message);
        }

        [Fact]
        public async Task AddInstructorAsync_UserWithoutAnyRelevantPermissions_ThrowsUnauthorizedAccessException()
        {
            // Arrange - User has neither general edit permission nor EditOwnSchedule permission
            var targetMothraId = "target123";
            var currentUserMothraId = "current456";
            var rotationId = 1;
            var weekIds = new[] { 1 };

            // Set up that user doesn't have general edit permission for rotation
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Set up permission validator to throw UnauthorizedAccessException for insufficient permissions
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(rotationId, targetMothraId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException($"User {currentUserMothraId} does not have permission to edit rotation {rotationId} or their own schedule (target: {targetMothraId})"));

            // Act & Assert
            var testYear = DateTime.Now.Year + 1;
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AddInstructorAsync(targetMothraId, rotationId, weekIds, testYear, false, CancellationToken.None));

            Assert.Contains("does not have permission", exception.Message);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_WithoutPermissionForOwnSchedule_ThrowsUnauthorizedAccessException()
        {
            // Arrange - User tries to remove their own schedule but lacks EditOwnSchedule permission
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await _context.InstructorSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            // Set up audit service mock
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Permission validator throws exception when user lacks permission for their own schedule
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(1, "test123", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("User test123 does not have permission to edit rotation 1 or their own schedule"));

            // Act & Assert - Should throw UnauthorizedAccessException
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RemoveInstructorScheduleAsync(schedule.InstructorScheduleId));

            Assert.Contains("does not have permission", exception.Message);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_UserTriesToRemoveOtherUserScheduleWithoutGeneralPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange - User tries to remove another user's schedule without general rotation permission
            var schedule = TestDataBuilder.CreateInstructorSchedule("other456", 1, 1, false);
            await _context.InstructorSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            // Set up audit service mock
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Permission validator throws exception when user lacks general permission to edit other user's schedule
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(1, "other456", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("User does not have permission to edit rotation 1 or other user's schedule"));

            // Act & Assert - Should throw UnauthorizedAccessException
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RemoveInstructorScheduleAsync(schedule.InstructorScheduleId));

            Assert.Contains("does not have permission", exception.Message);
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

            // Create conflicting schedule - same instructor, same rotation, same week
            var conflictingSchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, false);
            await _context.InstructorSchedules.AddAsync(conflictingSchedule);
            await _context.SaveChangesAsync();

            // Override default permission validator to return the specific user for this test
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert - use current year + 1 for future year testing
            var testYear = DateTime.Now.Year + 1;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddInstructorAsync(mothraId, rotationId, weekIds, testYear, false, CancellationToken.None));
            // The service now wraps duplicate errors in a generic message
            // Check either the main message or inner exception for the duplicate indicator
            Assert.True(
                ex.Message.Contains("already scheduled") ||
                ex.Message.Contains("Failed to add instructor") ||
                (ex.InnerException != null && ex.InnerException.Message.Contains("already scheduled")),
                $"Expected duplicate schedule error but got: {ex.Message}");
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_ValidSchedule_RemovesSuccessfully()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await _context.InstructorSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(schedule.InstructorScheduleId);

            // Assert
            Assert.True(result.success);
            var removedSchedule = await _context.InstructorSchedules.FindAsync(schedule.InstructorScheduleId);
            Assert.Null(removedSchedule);

            // Verify the permission validator was called
            _mockPermissionValidator.Verify(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            // Verify audit logging
            _mockAuditService.Verify(x => x.LogInstructorRemovedAsync("test123", 1, 1,
                "currentuser", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluatorWithOtherInstructors_RemovesSuccessfully()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 1, 1, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", 1, 1, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result.success);
            var removedSchedule = await _context.InstructorSchedules.FindAsync(primarySchedule.InstructorScheduleId);
            Assert.Null(removedSchedule);

            // Verify primary evaluator unset audit log
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync("primary123", 1, 1,
                "currentuser", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluatorWithoutOtherInstructors_RemovesSuccessfully()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 2, 2, true);
            await _context.InstructorSchedules.AddAsync(primarySchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(2, "primary123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result.success);
            var removedSchedule = await _context.InstructorSchedules.FindAsync(primarySchedule.InstructorScheduleId);
            Assert.Null(removedSchedule);
        }

        [Fact]
        public async Task SetPrimaryEvaluatorAsync_ValidSchedule_UpdatesSuccessfully()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await _context.InstructorSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.SetPrimaryEvaluatorAsync(schedule.InstructorScheduleId, true);

            // Assert
            Assert.True(result.success);
            var updatedSchedule = await _context.InstructorSchedules.FindAsync(schedule.InstructorScheduleId);
            Assert.True(updatedSchedule!.Evaluator);

            // Verify audit logging
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync("test123", 1, 1,
                "currentuser", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CanRemoveInstructorAsync_NonPrimaryEvaluator_ReturnsTrue()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false);
            await _context.InstructorSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

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

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CanRemoveInstructorAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanRemoveInstructorAsync_PrimaryEvaluatorWithoutOtherInstructors_ReturnsTrue()
        {
            // Arrange
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule("primary123", 2, 2, true);
            await _context.InstructorSchedules.AddAsync(primarySchedule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CanRemoveInstructorAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetOtherRotationSchedulesAsync_WithConflicts_ReturnsConflictingSchedules()
        {
            // Arrange
            var mothraId = "test123";
            var weekIds = new[] { 1, 2 };

            var conflictSchedule1 = TestDataBuilder.CreateInstructorSchedule(mothraId, 1, 1, false);
            var conflictSchedule2 = TestDataBuilder.CreateInstructorSchedule(mothraId, 2, 2, false);

            await _context.InstructorSchedules.AddRangeAsync(conflictSchedule1, conflictSchedule2);
            await _context.SaveChangesAsync();

            // Act
            var testYear = DateTime.Now.Year + 1;
            var result = await _service.GetOtherRotationSchedulesAsync(mothraId, weekIds, testYear);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, schedule => Assert.Equal(mothraId, schedule.MothraId));
        }

        [Fact]
        public async Task GetOtherRotationSchedulesAsync_WithExcludedRotation_ExcludesSpecifiedRotation()
        {
            // Arrange
            var mothraId = "test123";
            var weekIds = new[] { 1 };
            var excludeRotationId = 1;

            var includedSchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, excludeRotationId, 1, false);
            var excludedSchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, 2, 1, false);

            await _context.InstructorSchedules.AddRangeAsync(includedSchedule, excludedSchedule);
            await _context.SaveChangesAsync();

            // Act
            var testYear = DateTime.Now.Year + 1;
            var result = await _service.GetOtherRotationSchedulesAsync(mothraId, weekIds, testYear, excludeRotationId);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].RotationId); // Should only include rotation 2, not excluded rotation 1
        }

        [Fact]
        public async Task GetOtherRotationSchedulesAsync_NoConflicts_ReturnsEmptyList()
        {
            // Arrange
            // Create instructor schedule for different weeks (no conflicts)
            var scheduleNoConflict = TestDataBuilder.CreateInstructorSchedule("12345", 1, 5);
            await _context.InstructorSchedules.AddAsync(scheduleNoConflict);
            await _context.SaveChangesAsync();

            // Act - Check for conflicts on different weeks
            var testYear = DateTime.Now.Year + 1;
            var result = await _service.GetOtherRotationSchedulesAsync("12345", new[] { 10, 15 }, testYear, cancellationToken: CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetOtherRotationSchedulesAsync_WithMultipleConflicts_ReturnsAllConflictingSchedules()
        {
            // Arrange
            // Create multiple conflicting schedules
            var schedule1 = TestDataBuilder.CreateInstructorSchedule("12345", 1, 10);
            var schedule2 = TestDataBuilder.CreateInstructorSchedule("12345", 2, 15);
            var schedule3 = TestDataBuilder.CreateInstructorSchedule("12345", 3, 20);
            await _context.InstructorSchedules.AddRangeAsync(schedule1, schedule2, schedule3);
            await _context.SaveChangesAsync();

            // Act - Check for conflicts on all these weeks for a different rotation
            var testYear = DateTime.Now.Year + 1;
            var result = await _service.GetOtherRotationSchedulesAsync("12345", new[] { 10, 15, 20 }, testYear, excludeRotationId: 4, cancellationToken: CancellationToken.None);

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

            await _context.InstructorSchedules.AddRangeAsync(instructor1, instructor2);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Switch primary evaluator from instructor1 to instructor2
            var result = await _service.SetPrimaryEvaluatorAsync(instructor2.InstructorScheduleId, true);

            // Assert
            Assert.True(result.success);

            // Verify instructor1 is no longer primary
            var updatedInstructor1 = await _context.InstructorSchedules.FindAsync(instructor1.InstructorScheduleId);
            Assert.False(updatedInstructor1!.Evaluator);

            // Verify instructor2 is now primary
            var updatedInstructor2 = await _context.InstructorSchedules.FindAsync(instructor2.InstructorScheduleId);
            Assert.True(updatedInstructor2!.Evaluator);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
