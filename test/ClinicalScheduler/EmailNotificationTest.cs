using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Moq;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Models.ClinicalScheduler;
using Viper.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Tests for email notification functionality when primary evaluators are changed.
    /// Covers email sending, content validation, and error handling scenarios.
    /// </summary>
    public class EmailNotificationTest : IDisposable
    {
        private readonly Mock<IScheduleAuditService> _mockAuditService;
        private readonly Mock<ILogger<ScheduleEditService>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IOptions<EmailNotificationSettings>> _mockEmailNotificationOptions;
        private readonly Mock<IGradYearService> _mockGradYearService;
        private readonly Mock<IPermissionValidator> _mockPermissionValidator;
        private readonly Mock<IUserHelper> _mockUserHelper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly TestableScheduleEditService _service;
        private readonly ClinicalSchedulerContext _context;

        public EmailNotificationTest()
        {
            // Create real in-memory database for Entity Framework operations
            var options = new DbContextOptionsBuilder<ClinicalSchedulerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore())
                .Options;

            _context = new ClinicalSchedulerContext(options);

            _mockAuditService = new Mock<IScheduleAuditService>();
            _mockLogger = new Mock<ILogger<ScheduleEditService>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailNotificationOptions = new Mock<IOptions<EmailNotificationSettings>>();
            _mockPermissionValidator = new Mock<IPermissionValidator>();
            _mockUserHelper = new Mock<IUserHelper>();
            _mockConfiguration = new Mock<IConfiguration>();

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

            // Setup audit service
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit { ScheduleAuditId = 1 });

            _service = new TestableScheduleEditService(
                _context,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockEmailNotificationOptions.Object,
                _mockGradYearService.Object,
                _mockPermissionValidator.Object,
                _mockConfiguration.Object);
        }

        private async Task AddTestPersonAsync(string mothraId, string firstName = "Test", string lastName = "User")
        {
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == mothraId))
            {
                await _context.Persons.AddAsync(new Viper.Models.ClinicalScheduler.Person
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
            if (!await _context.Weeks.AnyAsync(w => w.WeekId == weekId))
            {
                await _context.Weeks.AddAsync(new Week
                {
                    WeekId = weekId,
                    DateStart = DateTime.UtcNow.AddDays(-7 * (10 - weekId)),
                    DateEnd = DateTime.UtcNow.AddDays(-7 * (10 - weekId) + 6),
                    TermCode = 202501
                });
            }

            // Add WeekGradYear if it doesn't exist
            if (!await _context.WeekGradYears.AnyAsync(w => w.WeekId == weekId && w.GradYear == gradYear))
            {
                await _context.WeekGradYears.AddAsync(new WeekGradYear
                {
                    WeekId = weekId,
                    GradYear = gradYear,
                    WeekNum = weekNum
                });
            }
        }

        private async Task AddTestRotationAsync(int rotationId, string name = "Test Rotation", string abbreviation = "TR")
        {
            if (!await _context.Rotations.AnyAsync(r => r.RotId == rotationId))
            {
                await _context.Rotations.AddAsync(new Rotation
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
            await _context.SaveChangesAsync();

            // Create primary evaluator and another instructor (so removal is allowed)
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            // Refresh to get the generated IDs
            var savedPrimarySchedule = await _context.InstructorSchedules
                .FirstAsync(s => s.MothraId == mothraId && s.RotationId == rotationId && s.WeekId == weekId);

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await _service.RemoveInstructorScheduleAsync(savedPrimarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result.success);

            // Verify email was sent with correct content (now HTML format)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test@ucdavis.edu"),
                It.Is<string>(subject => subject == "Primary Evaluator Removed - Cardiology Rotation - Week 15"),
                It.Is<string>(body => body.Contains("Primary Evaluator Removed") &&
                                      body.Contains("Doe, John") &&
                                      body.Contains("Cardiology Rotation") &&
                                      body.Contains("Week 15") &&
                                      body.Contains("</html>")),
                It.Is<bool>(isHtml => isHtml),
                It.Is<string>(from => from == "testfrom@ucdavis.edu")
            ), Times.Once);

            // Verify audit logs were created
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(mothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveInstructorScheduleAsync_NonPrimaryEvaluator_DoesNotSendEmail()
        {
            // Arrange
            var rotationId = 1;
            var weekId = 1;

            // Add required service, week and rotation data
            // First add a service that the rotation will reference
            if (!await _context.Services.AnyAsync(s => s.ServiceId == 1))
            {
                await _context.Services.AddAsync(new Service
                {
                    ServiceId = 1,
                    ServiceName = "Test Service",
                    ShortName = "TEST"
                });
            }

            await AddTestWeekGradYearAsync(weekId, 2025, 1);
            await AddTestRotationAsync(rotationId, "Test Rotation", "TEST");

            // Add Person entity for the instructor
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == "test123"))
            {
                await _context.Persons.AddAsync(new Models.ClinicalScheduler.Person
                {
                    IdsMothraId = "test123",
                    PersonDisplayFullName = "Test Instructor",
                    PersonDisplayFirstName = "Test",
                    PersonDisplayLastName = "Instructor",
                    IdsMailId = "testinstructor@ucdavis.edu"
                });
            }

            await _context.SaveChangesAsync();

            var schedule = TestDataBuilder.CreateInstructorSchedule("test123", rotationId, weekId, false); // Not primary
            await _context.InstructorSchedules.AddAsync(schedule);
            await _context.SaveChangesAsync();

            // Get the actual saved entity with the database-assigned ID
            var savedSchedule = await _context.InstructorSchedules
                .FirstAsync(s => s.MothraId == "test123" && s.RotationId == rotationId && s.WeekId == 1 && !s.Evaluator);

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);

            // Setup permission validator with more specific logging
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user)
                .Callback<int, string, CancellationToken>((rotId, mothraId, token) =>
                {
                    System.Console.WriteLine($"Permission validator called with: rotationId={rotId}, mothraId={mothraId}");
                });
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Debug: Try the same query that RemoveInstructorScheduleAsync uses
            var debugSchedule = await _context.InstructorSchedules
                .Include(s => s.Rotation)
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.InstructorScheduleId == savedSchedule.InstructorScheduleId);
            System.Console.WriteLine($"Debug schedule found: {debugSchedule != null}, Rotation: {debugSchedule?.Rotation?.Name}, Person: {debugSchedule?.Person?.PersonDisplayFullName}");

            // Act
            (bool success, bool wasPrimaryEvaluator, string? instructorName) result = (false, false, null);
            Exception? caughtException = null;
            try
            {
                result = await _service.RemoveInstructorScheduleAsync(savedSchedule.InstructorScheduleId);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                System.Console.WriteLine($"Exception caught: {ex.GetType().Name}: {ex.Message}");
            }

            // Debug: Check if schedule exists after calling service
            var scheduleExistsAfterCall = await _context.InstructorSchedules
                .AnyAsync(s => s.InstructorScheduleId == savedSchedule.InstructorScheduleId);
            System.Console.WriteLine($"Schedule exists after call: {scheduleExistsAfterCall}, ID: {savedSchedule.InstructorScheduleId}");
            System.Console.WriteLine($"Result: success={result.success}, wasPrimary={result.wasPrimaryEvaluator}, name={result.instructorName}");
            if (caughtException != null)
            {
                System.Console.WriteLine($"Exception: {caughtException}");
            }

            // Assert
            Assert.True(result.success);

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
            await _context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
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
            Assert.True(result.success);
            var removedSchedule = await _context.InstructorSchedules.FindAsync(primarySchedule.InstructorScheduleId);
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

            // Add required service that the rotation will reference
            if (!await _context.Services.AnyAsync(s => s.ServiceId == 1))
            {
                await _context.Services.AddAsync(new Service
                {
                    ServiceId = 1,
                    ServiceName = "Test Service",
                    ShortName = "TEST"
                });
            }

            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Internal Medicine", "IM");

            // Add Person for "other456" so Include query works
            // Also add minimal Person for mothraId to allow Include query to work, but with minimal data to test graceful handling
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == "other456"))
            {
                await _context.Persons.AddAsync(new Models.ClinicalScheduler.Person
                {
                    IdsMothraId = "other456",
                    PersonDisplayFullName = "Other Person",
                    PersonDisplayFirstName = "Other",
                    PersonDisplayLastName = "Person",
                    IdsMailId = "other@ucdavis.edu"
                });
            }

            // Add minimal Person for unknown123 to allow Include query to work
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == mothraId))
            {
                await _context.Persons.AddAsync(new Models.ClinicalScheduler.Person
                {
                    IdsMothraId = mothraId,
                    // Deliberately use empty strings to test graceful handling of missing name data
                    PersonDisplayFullName = "",
                    PersonDisplayFirstName = "",
                    PersonDisplayLastName = "",
                    IdsMailId = null
                });
            }

            await _context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            // Get the actual saved entity with the database-assigned ID
            var savedPrimarySchedule = await _context.InstructorSchedules
                .FirstAsync(s => s.MothraId == mothraId && s.RotationId == rotationId && s.WeekId == weekId && s.Evaluator);

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Debug: Check the same query that the service uses
            var debugSchedule = await _context.InstructorSchedules
                .Include(s => s.Rotation)
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.InstructorScheduleId == savedPrimarySchedule.InstructorScheduleId);
            System.Console.WriteLine($"Debug schedule found: {debugSchedule != null}, Rotation: {debugSchedule?.Rotation?.Name}, Person: {debugSchedule?.Person?.PersonDisplayFullName}");

            // Act
            (bool success, bool wasPrimaryEvaluator, string? instructorName) result = (false, false, null);
            Exception? caughtException = null;
            try
            {
                result = await _service.RemoveInstructorScheduleAsync(savedPrimarySchedule.InstructorScheduleId);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                System.Console.WriteLine($"Exception caught: {ex.GetType().Name}: {ex.Message}");
            }

            System.Console.WriteLine($"Result: success={result.success}, wasPrimary={result.wasPrimaryEvaluator}, name={result.instructorName}");
            if (caughtException != null)
            {
                System.Console.WriteLine($"Exception: {caughtException}");
            }

            // Assert
            Assert.True(result.success);

            // Verify email was sent with fallback to "Unknown Instructor" format (HTML)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test@ucdavis.edu"),
                It.Is<string>(subject => subject == "Primary Evaluator Removed - Internal Medicine - Week 20"),
                It.Is<string>(body => body.Contains("Unknown Instructor") &&
                                      body.Contains("Internal Medicine") &&
                                      body.Contains("Week 20") &&
                                      body.Contains("</html>")), // HTML format with graceful fallback
                It.Is<bool>(isHtml => isHtml),
                It.Is<string>(from => from == "testfrom@ucdavis.edu")
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
            await _context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
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

            // Verify email was sent with weekId as fallback (HTML format)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test@ucdavis.edu"),
                It.Is<string>(subject => subject == "Primary Evaluator Removed - Surgery Rotation - Week 99"),
                It.Is<string>(body => body.Contains("Smith, Jane") &&
                                      body.Contains("Surgery Rotation") &&
                                      body.Contains("Week 99") && // Uses weekId as fallback
                                      body.Contains("</html>")),
                It.Is<bool>(isHtml => isHtml),
                It.Is<string>(from => from == "testfrom@ucdavis.edu")
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
                    From = "test-sender@test.edu"
                }
            };
            _mockEmailNotificationOptions.Setup(x => x.Value).Returns(emailNotificationSettings);

            // Create a new service instance with the updated configuration
            var serviceWithMultipleRecipients = new TestableScheduleEditService(
                _context,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockEmailNotificationOptions.Object,
                _mockGradYearService.Object,
                _mockPermissionValidator.Object,
                _mockConfiguration.Object);

            await AddTestPersonAsync(mothraId, "John", "Doe");
            await AddTestWeekGradYearAsync(weekId, 2025);
            await AddTestRotationAsync(rotationId, "Oncology", "ONC");
            await _context.SaveChangesAsync();

            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(mothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other456", rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorRemovedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act
            var result = await serviceWithMultipleRecipients.RemoveInstructorScheduleAsync(primarySchedule.InstructorScheduleId);

            // Assert
            Assert.True(result.success);

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
        public async Task SetPrimaryEvaluatorAsync_ReplacingPrimaryEvaluator_DoesNotSendEmail()
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
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == "currentuser"))
            {
                await _context.Persons.AddAsync(new Viper.Models.ClinicalScheduler.Person
                {
                    IdsMothraId = "currentuser",
                    PersonDisplayFullName = "Current User", // Set exact display name expected in test
                    PersonDisplayLastName = "User",
                    PersonDisplayFirstName = "Current"
                });
            }
            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Cardiology Rotation", "CARD");
            await _context.SaveChangesAsync();

            // Create existing primary evaluator and new instructor
            var oldPrimarySchedule = TestDataBuilder.CreateInstructorSchedule(oldPrimaryMothraId, rotationId, weekId, true);
            var newInstructorSchedule = TestDataBuilder.CreateInstructorSchedule(newPrimaryMothraId, rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(oldPrimarySchedule, newInstructorSchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Set new instructor as primary evaluator (should NOT trigger email for replacement)
            var result = await _service.SetPrimaryEvaluatorAsync(newInstructorSchedule.InstructorScheduleId, true);

            // Assert
            Assert.True(result.success);

            // Verify NO email was sent for primary evaluator replacement
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Never);

            // Verify audit logs were still created for both unset and set
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(oldPrimaryMothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorSetAsync(newPrimaryMothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddInstructorAsync_AsPrimaryEvaluator_DoesNotSendReplacementEmail()
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
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == "currentuser"))
            {
                await _context.Persons.AddAsync(new Viper.Models.ClinicalScheduler.Person
                {
                    IdsMothraId = "currentuser",
                    PersonDisplayFullName = "Current User", // Set exact display name expected in test
                    PersonDisplayLastName = "User",
                    PersonDisplayFirstName = "Current"
                });
            }
            await AddTestWeekGradYearAsync(weekIds[0], testYear, weekNum);
            await AddTestRotationAsync(rotationId, "Surgery Rotation", "SURG");
            await _context.SaveChangesAsync();

            // Create existing primary evaluator
            var oldPrimarySchedule = TestDataBuilder.CreateInstructorSchedule(oldPrimaryMothraId, rotationId, weekIds[0], true);
            await _context.InstructorSchedules.AddAsync(oldPrimarySchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogInstructorAddedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorSetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Add new instructor as primary evaluator (should NOT trigger email for replacement)
            var result = await _service.AddInstructorAsync(newPrimaryMothraId, rotationId, weekIds, testYear, true);

            // Assert
            Assert.NotEmpty(result);

            // Verify NO email was sent for primary evaluator replacement
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>()
            ), Times.Never);

            // Verify audit logs were still created
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
            if (!await _context.Persons.AnyAsync(p => p.IdsMothraId == "currentuser"))
            {
                await _context.Persons.AddAsync(new Viper.Models.ClinicalScheduler.Person
                {
                    IdsMothraId = "currentuser",
                    PersonDisplayFullName = "Current User", // Set exact display name expected in test
                    PersonDisplayLastName = "User",
                    PersonDisplayFirstName = "Current"
                });
            }
            await AddTestWeekGradYearAsync(weekId, 2025, weekNum);
            await AddTestRotationAsync(rotationId, "Neurology Rotation", "NEURO");
            await _context.SaveChangesAsync();

            // Create primary evaluator and another instructor (so unsetting is allowed)
            var primarySchedule = TestDataBuilder.CreateInstructorSchedule(primaryMothraId, rotationId, weekId, true);
            var otherSchedule = TestDataBuilder.CreateInstructorSchedule("other789", rotationId, weekId, false);

            await _context.InstructorSchedules.AddRangeAsync(primarySchedule, otherSchedule);
            await _context.SaveChangesAsync();

            var user = TestDataBuilder.CreateUser("currentuser");
            _mockUserHelper.Setup(x => x.GetCurrentUser()).Returns(user);
            _mockPermissionValidator.Setup(x => x.ValidateEditPermissionAndGetUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockAuditService.Setup(x => x.LogPrimaryEvaluatorUnsetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ScheduleAudit());

            // Act - Unset primary evaluator (no replacement)
            var result = await _service.SetPrimaryEvaluatorAsync(primarySchedule.InstructorScheduleId, false);

            // Assert
            Assert.True(result.success);

            // Verify removal-only email was sent (HTML format, without replacement)
            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.Is<string>(to => to == "test@ucdavis.edu"),
                It.Is<string>(subject => subject == "Primary Evaluator Removed - Neurology Rotation - Week 15"),
                It.Is<string>(body => body.Contains("Primary Evaluator Removed") &&
                                      body.Contains("Brown, Charlie") &&
                                      body.Contains("Neurology Rotation") &&
                                      body.Contains("Week 15") &&
                                      !body.Contains("Replaced by:") &&
                                      body.Contains("Current User") &&
                                      body.Contains("</html>")),
                It.Is<bool>(isHtml => isHtml),
                It.Is<string>(from => from == "testfrom@ucdavis.edu")
            ), Times.Once);

            // Verify audit log was created
            _mockAuditService.Verify(x => x.LogPrimaryEvaluatorUnsetAsync(primaryMothraId, rotationId, weekId,
                user.MothraId, It.IsAny<CancellationToken>()), Times.Once);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
        }
    }
}
