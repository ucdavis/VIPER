using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Models.ClinicalScheduler;
using Viper.Services;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Tests for email notification functionality when primary evaluators are changed.
    /// Covers email sending, content validation, and error handling scenarios.
    /// </summary>
    public class EmailNotificationTest : ClinicalSchedulerTestBase
    {
        private readonly Mock<ISchedulePermissionService> _mockPermissionService;
        private readonly Mock<IScheduleAuditService> _mockAuditService;
        private readonly Mock<IUserHelper> _mockUserHelper;
        private readonly Mock<ILogger<ScheduleEditService>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IOptions<EmailNotificationSettings>> _mockEmailNotificationOptions;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly ScheduleEditService _service;
        private readonly EmailNotificationSettings _testEmailSettings;

        public EmailNotificationTest()
        {
            _mockPermissionService = new Mock<ISchedulePermissionService>();
            _mockAuditService = new Mock<IScheduleAuditService>();
            _mockUserHelper = new Mock<IUserHelper>();
            _mockLogger = new Mock<ILogger<ScheduleEditService>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailNotificationOptions = new Mock<IOptions<EmailNotificationSettings>>();

            // Setup default email notification configuration for tests
            _testEmailSettings = new EmailNotificationSettings
            {
                PrimaryEvaluatorRemoved = new PrimaryEvaluatorRemovedNotificationSettings
                {
                    To = new List<string> { "test-recipient@test.edu" },
                    From = "test-sender@test.edu",
                    Subject = "Test - Primary Evaluator Removed"
                }
            };
            _mockEmailNotificationOptions.Setup(x => x.Value).Returns(_testEmailSettings);

            _mockGradYearService = new Mock<IGradYearService>();
            // Set up default for grad year service - use current year
            var currentYear = DateTime.Now.Year;
            _mockGradYearService.Setup(x => x.GetCurrentGradYearAsync())
                .ReturnsAsync(currentYear);

            _service = new ScheduleEditService(
                Context,
                _mockPermissionService.Object,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockEmailNotificationOptions.Object,
                _mockGradYearService.Object,
                _mockUserHelper.Object);
        }

        private async Task AddTestPersonAsync(string mothraId, string firstName = "Test", string lastName = "User")
        {
            if (!Context.Persons.Any(p => p.IdsMothraId == mothraId))
            {
                await Context.Persons.AddAsync(new Person
                {
                    IdsMothraId = mothraId,
                    PersonDisplayFullName = $"{lastName}, {firstName}",
                    PersonDisplayLastName = lastName,
                    PersonDisplayFirstName = firstName
                });
            }
        }

        private async Task AddTestWeekGradYearAsync(int weekId, int gradYear = 2025, int weekNum = 10)
        {
            // Add Week if it doesn't exist
            if (!Context.Weeks.Any(w => w.WeekId == weekId))
            {
                await Context.Weeks.AddAsync(new Week
                {
                    WeekId = weekId,
                    DateStart = DateTime.UtcNow.AddDays(-7 * (10 - weekId)),
                    DateEnd = DateTime.UtcNow.AddDays(-7 * (10 - weekId) + 6),
                    TermCode = 202501
                });
            }

            // Add WeekGradYear if it doesn't exist
            if (!Context.WeekGradYears.Any(w => w.WeekId == weekId && w.GradYear == gradYear))
            {
                await Context.WeekGradYears.AddAsync(new WeekGradYear
                {
                    WeekId = weekId,
                    GradYear = gradYear,
                    WeekNum = weekNum
                });
            }
        }

        private async Task AddTestRotationAsync(int rotationId, string name = "Test Rotation", string abbreviation = "TR")
        {
            if (!Context.Rotations.Any(r => r.RotId == rotationId))
            {
                await Context.Rotations.AddAsync(new Rotation
                {
                    RotId = rotationId,
                    Name = name,
                    Abbreviation = abbreviation,
                    ServiceId = 1
                });
            }
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluator_SendsEmailNotification()
        {
            // Arrange
            var mothraId = "primary123";
            var rotationId = 1;
            var weekId = 1;
            var weekNum = 15;

            // Add test data
            await AddTestPersonAsync(mothraId, "John", "Doe");
            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Cardiology Rotation", "CARD");
            await Context.SaveChangesAsync();

            // Create primary evaluator and another instructor (so removal is allowed)
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);

            // Verify email was sent with correct content
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == _testEmailSettings.PrimaryEvaluatorRemoved.To[0]),
                It.Is<string>(subject => subject == _testEmailSettings.PrimaryEvaluatorRemoved.Subject),
                It.Is<string>(body => body.Contains("Primary evaluator") &&
                                      body.Contains("(Doe, John)") &&
                                      body.Contains("removed from Cardiology Rotation week 15")),
                It.Is<bool>(isHtml => !isHtml),
                It.Is<string>(from => from == _testEmailSettings.PrimaryEvaluatorRemoved.From)
            ), Times.Once);

            // Verify audit logs were created
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(mothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_NonPrimaryEvaluator_DoesNotSendEmail()
        {
            // Arrange
            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", 1, 1, false); // Not primary
            await Context.InstructorSchedules.AddAsync(schedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(schedule.InstructorScheduleId);

            // Assert
            Assert.True(result);

            // Verify NO email was sent
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Never);

            // Verify removal was still logged
            _mockAuditService.Verify(x => x.LogInstructorRemovedAsync("test123", 1, 1,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_EmailServiceFails_StillCompletesRemoval()
        {
            // Arrange
            var mothraId = "primary123";
            var rotationId = 10; // Use unique rotation ID to avoid conflict
            var weekId = 10; // Use unique week ID to avoid conflict

            await AddTestPersonAsync(mothraId, "John", "Doe");
            await AddTestWeekGradYearAsync(weekId, 2025);
            await AddTestRotationAsync(rotationId);
            await Context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Setup email service to throw exception
            _mockEmailService.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert - removal should still succeed despite email failure
            Assert.True(result);
            var removedSchedule = await Context.InstructorSchedules.FindAsync(primarySchedule.InstructorScheduleId);
            Assert.Null(removedSchedule);

            // Verify email was attempted
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Once);

            // Verify audit logs were still created
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(mothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluatorWithoutPersonData_StillSendsEmail()
        {
            // Arrange - Note: NOT adding person data to test graceful handling
            var mothraId = "unknown123";
            var rotationId = 20; // Use unique rotation ID to avoid conflict
            var weekId = 20; // Use unique week ID to avoid conflict
            var weekNum = 20;

            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Internal Medicine", "IM");
            await Context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);

            // Verify email was sent without person name (no parentheses around name)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == _testEmailSettings.PrimaryEvaluatorRemoved.To[0]),
                It.Is<string>(subject => subject == _testEmailSettings.PrimaryEvaluatorRemoved.Subject),
                It.Is<string>(body => body.Contains("Primary evaluator removed from Internal Medicine week 20") &&
                                      !body.Contains("(") && !body.Contains(")")), // No parentheses when name is missing
                It.Is<bool>(isHtml => !isHtml),
                It.Is<string>(from => from == _testEmailSettings.PrimaryEvaluatorRemoved.From)
            ), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_PrimaryEvaluatorWithMissingWeekData_UsesWeekIdInEmail()
        {
            // Arrange - Test when WeekGradYear data is missing
            var mothraId = "primary123";
            var rotationId = 30; // Use unique rotation ID to avoid conflict
            var weekId = 99; // Use a week ID that won't have WeekGradYear data

            await AddTestPersonAsync(mothraId, "Jane", "Smith");
            await AddTestRotationAsync(rotationId, "Surgery Rotation", "SURG");

            // Note: NOT adding WeekGradYear data to test fallback behavior
            await Context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);

            // Verify email was sent with weekId as fallback
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == _testEmailSettings.PrimaryEvaluatorRemoved.To[0]),
                It.Is<string>(subject => subject == _testEmailSettings.PrimaryEvaluatorRemoved.Subject),
                It.Is<string>(body => body.Contains("Primary evaluator") &&
                                      body.Contains("(Smith, Jane)") &&
                                      body.Contains("removed from Surgery Rotation week 99")), // Uses weekId as fallback
                It.Is<bool>(isHtml => !isHtml),
                It.Is<string>(from => from == _testEmailSettings.PrimaryEvaluatorRemoved.From)
            ), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_MultipleEmailRecipients_SendsToAllRecipients()
        {
            // Arrange
            var mothraId = "primary123";
            var rotationId = 40; // Use unique rotation ID to avoid conflict
            var weekId = 40; // Use unique week ID to avoid conflict

            // Setup multiple email recipients for testing
            var emailNotificationSettings = new EmailNotificationSettings
            {
                PrimaryEvaluatorRemoved = new PrimaryEvaluatorRemovedNotificationSettings
                {
                    To = new List<string> { "test-recipient1@test.edu", "test-recipient2@test.edu", "test-recipient3@test.edu" },
                    From = "test-sender@test.edu",
                    Subject = "Test - Clinical Scheduler - Primary Evaluator Removed"
                }
            };
            _mockEmailNotificationOptions.Setup(x => x.Value).Returns(emailNotificationSettings);

            // Create a new service instance with the updated configuration
            var serviceWithMultipleRecipients = new ScheduleEditService(
                Context,
                _mockPermissionService.Object,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockEmailNotificationOptions.Object,
                _mockGradYearService.Object,
                _mockUserHelper.Object);

            await AddTestPersonAsync(mothraId, "John", "Doe");
            await AddTestWeekGradYearAsync(weekId, 2025);
            await AddTestRotationAsync(rotationId, "Oncology", "ONC");
            await Context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await serviceWithMultipleRecipients.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result);

            // Verify email was sent to all three recipients
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test-recipient1@test.edu"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Once);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test-recipient2@test.edu"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Once);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test-recipient3@test.edu"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Once);

            // Verify total of 3 email calls
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Exactly(3));
        }

        [Fact]
        public async Task SetPrimaryEvaluatorAsync_ReplacingPrimaryEvaluator_SendsReplacementEmail()
        {
            // Arrange
            var oldPrimaryMothraId = "oldprimary123";
            var newPrimaryMothraId = "newprimary456";
            var rotationId = 1;
            var weekId = 1;
            var weekNum = 15;

            // Add test data
            await AddTestPersonAsync(oldPrimaryMothraId, "Jane", "Smith");
            await AddTestPersonAsync(newPrimaryMothraId, "John", "Doe");
            // Add the current user person - need to manually set the display name to match expected output
            if (!Context.Persons.Any(p => p.IdsMothraId == "currentuser"))
            {
                await Context.Persons.AddAsync(new Person
                {
                    IdsMothraId = "currentuser",
                    PersonDisplayFullName = "Current User", // Set exact display name expected in test
                    PersonDisplayLastName = "User",
                    PersonDisplayFirstName = "Current"
                });
            }
            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Cardiology Rotation", "CARD");
            await Context.SaveChangesAsync();

            // Create existing primary evaluator and new instructor
            var oldPrimarySchedule = TestDataBuilder.CreateInstructorSchedule(oldPrimaryMothraId, rotationId, weekId, true);
            var newInstructorSchedule = TestDataBuilder.CreateInstructorSchedule(newPrimaryMothraId, rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(oldPrimarySchedule, newInstructorSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Set new instructor as primary evaluator (this should trigger replacement email)
            var result = await _service.SetPrimaryEvaluatorAsync(newInstructorSchedule.InstructorScheduleId, true);

            // Assert
            Assert.True(result.success);

            // Verify replacement email was sent with correct content including who made the change
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == _testEmailSettings.PrimaryEvaluatorRemoved.To[0]),
                It.Is<string>(subject => subject == _testEmailSettings.PrimaryEvaluatorRemoved.Subject),
                It.Is<string>(body => body.Contains("Primary evaluator") &&
                                      body.Contains("(Smith, Jane)") &&
                                      body.Contains("removed from Cardiology Rotation week 15") &&
                                      body.Contains("and replaced by Doe, John") &&
                                      body.Contains("by Current User")),
                It.Is<bool>(isHtml => !isHtml),
                It.Is<string>(from => from == _testEmailSettings.PrimaryEvaluatorRemoved.From)
            ), Times.Once);

            // Verify audit logs were created for both unset and set
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(oldPrimaryMothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync(newPrimaryMothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddInstructorAsync_AsPrimaryEvaluator_SendsReplacementEmail()
        {
            // Arrange
            var oldPrimaryMothraId = "oldprimary789";
            var newPrimaryMothraId = "newprimary012";
            var rotationId = 50; // Use unique rotation ID to avoid conflict
            var weekIds = new[] { 50 }; // Use unique week ID to avoid conflict
            var weekNum = 15;
            var testYear = 2025;

            // Add test data
            await AddTestPersonAsync(oldPrimaryMothraId, "Alice", "Johnson");
            await AddTestPersonAsync(newPrimaryMothraId, "Bob", "Wilson");
            // Add the current user person - need to manually set the display name to match expected output
            if (!Context.Persons.Any(p => p.IdsMothraId == "currentuser"))
            {
                await Context.Persons.AddAsync(new Person
                {
                    IdsMothraId = "currentuser",
                    PersonDisplayFullName = "Current User", // Set exact display name expected in test
                    PersonDisplayLastName = "User",
                    PersonDisplayFirstName = "Current"
                });
            }
            await AddTestWeekGradYearAsync(weekIds[0], testYear, weekNum);
            await AddTestRotationAsync(rotationId, "Surgery Rotation", "SURG");
            await Context.SaveChangesAsync();

            // Create existing primary evaluator
            var oldPrimarySchedule = TestDataBuilder.CreateInstructorSchedule(oldPrimaryMothraId, rotationId, weekIds[0], true);
            await Context.InstructorSchedules.AddAsync(oldPrimarySchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Add new instructor as primary evaluator (this should trigger replacement email)
            var result = await _service.AddInstructorAsync(newPrimaryMothraId, rotationId, weekIds, testYear, true);

            // Assert
            Assert.NotEmpty(result);

            // Verify replacement email was sent with correct content including who made the change
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == _testEmailSettings.PrimaryEvaluatorRemoved.To[0]),
                It.Is<string>(subject => subject == _testEmailSettings.PrimaryEvaluatorRemoved.Subject),
                It.Is<string>(body => body.Contains("Primary evaluator") &&
                                      body.Contains("(Johnson, Alice)") &&
                                      body.Contains("removed from Surgery Rotation week 15") &&
                                      body.Contains("and replaced by Wilson, Bob") &&
                                      body.Contains("by Current User")),
                It.Is<bool>(isHtml => !isHtml),
                It.Is<string>(from => from == _testEmailSettings.PrimaryEvaluatorRemoved.From)
            ), Times.Once);

            // Verify audit logs were created
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(oldPrimaryMothraId, rotationId, weekIds[0],
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync(newPrimaryMothraId, rotationId, weekIds[0],
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetPrimaryEvaluatorAsync_UnsettingPrimaryEvaluator_SendsRemovalOnlyEmail()
        {
            // Arrange
            var primaryMothraId = "primary345";
            var rotationId = 60; // Use unique rotation ID to avoid conflict
            var weekId = 60; // Use unique week ID to avoid conflict
            var weekNum = 15;

            // Add test data
            await AddTestPersonAsync(primaryMothraId, "Charlie", "Brown");
            // Add the current user person - need to manually set the display name to match expected output
            if (!Context.Persons.Any(p => p.IdsMothraId == "currentuser"))
            {
                await Context.Persons.AddAsync(new Person
                {
                    IdsMothraId = "currentuser",
                    PersonDisplayFullName = "Current User", // Set exact display name expected in test
                    PersonDisplayLastName = "User",
                    PersonDisplayFirstName = "Current"
                });
            }
            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Neurology Rotation", "NEURO");
            await Context.SaveChangesAsync();

            // Create primary evaluator and another instructor (so unsetting is allowed)
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(primaryMothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other789", rotationId, weekId, false);

            await Context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await Context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionService.Setup(x => x.HasEditPermissionForRotationAsync(rotationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Unset primary evaluator (no replacement)
            var result = await _service.SetPrimaryEvaluatorAsync(primarySchedule.InstructorScheduleId, false);

            // Assert
            Assert.True(result.success);

            // Verify removal-only email was sent (without replacement text) but with who made the change
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == _testEmailSettings.PrimaryEvaluatorRemoved.To[0]),
                It.Is<string>(subject => subject == _testEmailSettings.PrimaryEvaluatorRemoved.Subject),
                It.Is<string>(body => body.Contains("Primary evaluator") &&
                                      body.Contains("(Brown, Charlie)") &&
                                      body.Contains("removed from Neurology Rotation week 15") &&
                                      !body.Contains("and replaced by") &&
                                      body.Contains("by Current User")),
                It.Is<bool>(isHtml => !isHtml),
                It.Is<string>(from => from == _testEmailSettings.PrimaryEvaluatorRemoved.From)
            ), Times.Once);

            // Verify audit log was created
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(primaryMothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
