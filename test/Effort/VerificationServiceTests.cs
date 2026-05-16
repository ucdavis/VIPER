using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.EmailTemplates.Models;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.EmailTemplates.Services;
using Viper.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for VerificationService - effort verification and email operations.
/// </summary>
public sealed class VerificationServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditServiceMock;
    private readonly IEffortPermissionService _permissionServiceMock;
    private readonly ITermService _termServiceMock;
    private readonly IEmailService _emailServiceMock;
    private readonly ICourseClassificationService _classificationServiceMock;
    private readonly ILogger<VerificationService> _loggerMock;
    private readonly EffortSettings _settings;
    private readonly IEmailTemplateRenderer _emailTemplateRendererMock;
    private readonly VerificationService _service;

    private const int TestTermCode = 202410;
    private const int TestPersonId = 100;
    private const int TestCourseId = 1;

    public VerificationServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var viperOptions = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EffortDbContext(effortOptions);
        _viperContext = new VIPERContext(viperOptions);

        _auditServiceMock = Substitute.For<IEffortAuditService>();
        _permissionServiceMock = Substitute.For<IEffortPermissionService>();
        _termServiceMock = Substitute.For<ITermService>();
        _emailServiceMock = Substitute.For<IEmailService>();
        _classificationServiceMock = Substitute.For<ICourseClassificationService>();
        _loggerMock = Substitute.For<ILogger<VerificationService>>();

        _settings = new EffortSettings
        {
            VerificationEmailSubject = "Please Verify Your Effort",
            VerificationReplyDays = 7
        };

        var settingsOptions = Options.Create(_settings);
        var emailSettings = new EmailSettings
        {
            BaseUrl = "https://test.example.com"
        };
        var emailSettingsOptions = Options.Create(emailSettings);

        _emailTemplateRendererMock = Substitute.For<IEmailTemplateRenderer>();
        _emailTemplateRendererMock
            .RenderAsync<VerificationReminderViewModel>(
                Arg.Any<string>(),
                Arg.Any<VerificationReminderViewModel>(),
                Arg.Any<Dictionary<string, object>?>())
            .Returns("<html>Mock email body</html>");

        _auditServiceMock
            .LogPersonChangeAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(),
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _auditServiceMock
            .LogRecordChangeAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(),
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _service = new VerificationService(
            _context,
            _viperContext,
            _auditServiceMock,
            _permissionServiceMock,
            _termServiceMock,
            _emailServiceMock,
            _classificationServiceMock,
            _loggerMock,
            settingsOptions,
            emailSettingsOptions,
            _emailTemplateRendererMock);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, OpenedDate = DateTime.Now });

        _context.EffortTypes.AddRange(
            new EffortType { Id = "LEC", Description = "Lecture", IsActive = true, UsesWeeks = false },
            new EffortType { Id = "CLI", Description = "Clinical", IsActive = true, UsesWeeks = true }
        );

        _context.Roles.Add(new EffortRole { Id = 1, Description = "Instructor of Record", IsActive = true });

        _context.Courses.Add(new EffortCourse
        {
            Id = TestCourseId,
            TermCode = TestTermCode,
            Crn = "12345",
            SubjCode = "VET",
            CrseNumb = "410",
            SeqNumb = "01",
            Enrollment = 20,
            Units = 4,
            CustDept = "VME"
        });

        _context.Persons.Add(new EffortPerson
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            FirstName = "Test",
            LastName = "Instructor",
            EffortDept = "VME"
        });

        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = TestPersonId,
            MothraId = "testuser",
            MailId = "testuser",
            FirstName = "Test",
            LastName = "Instructor",
            FullName = "Instructor, Test",
            ClientId = "SVM",
            Current = 1
        });

        _context.SaveChanges();
        _viperContext.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _viperContext.Dispose();
    }

    #region GetMyEffortAsync Tests

    [Fact]
    public async Task GetMyEffortAsync_ReturnsNull_WhenUserNotLoggedIn()
    {
        // Arrange
        _permissionServiceMock.GetCurrentPersonId().Returns(0);

        // Act
        var result = await _service.GetMyEffortAsync(TestTermCode);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMyEffortAsync_ReturnsNull_WhenNoInstructorRecord()
    {
        // Arrange
        _permissionServiceMock.GetCurrentPersonId().Returns(9999);

        // Act
        var result = await _service.GetMyEffortAsync(TestTermCode);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMyEffortAsync_ReturnsDto_WhenInstructorExists()
    {
        // Arrange
        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);
        _permissionServiceMock.HasSelfServiceAccessAsync(Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.CanEditPersonEffortAsync(TestPersonId, TestTermCode, Arg.Any<CancellationToken>()).Returns(false);

        // OpenedDate makes status "Opened"
        _termServiceMock.GetTermAsync(TestTermCode, Arg.Any<CancellationToken>()).Returns(new TermDto { TermCode = TestTermCode, OpenedDate = DateTime.Now });
        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act
        var result = await _service.GetMyEffortAsync(TestTermCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestPersonId, result.Instructor.PersonId);
        Assert.Equal("Fall 2024", result.TermName);
        Assert.True(result.HasVerifyPermission);
    }

    [Fact]
    public async Task GetMyEffortAsync_IdentifiesZeroEffortRecords()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 0
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);
        _permissionServiceMock.HasSelfServiceAccessAsync(Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.CanEditPersonEffortAsync(TestPersonId, TestTermCode, Arg.Any<CancellationToken>()).Returns(false);

        // OpenedDate makes status "Opened"
        _termServiceMock.GetTermAsync(TestTermCode, Arg.Any<CancellationToken>()).Returns(new TermDto { TermCode = TestTermCode, OpenedDate = DateTime.Now });
        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act
        var result = await _service.GetMyEffortAsync(TestTermCode);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasZeroEffort);
        Assert.Contains(1, result.ZeroEffortRecordIds);
        Assert.False(result.CanVerify);
    }

    #endregion

    #region VerifyEffortAsync Tests

    [Fact]
    public async Task VerifyEffortAsync_ReturnsError_WhenNotLoggedIn()
    {
        // Arrange
        _permissionServiceMock.GetCurrentPersonId().Returns(0);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(VerificationErrorCodes.PersonNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task VerifyEffortAsync_ReturnsError_WhenNoInstructorRecord()
    {
        // Arrange
        _permissionServiceMock.GetCurrentPersonId().Returns(9999);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(VerificationErrorCodes.PersonNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task VerifyEffortAsync_ReturnsError_WhenAlreadyVerified()
    {
        // Arrange
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        person.EffortVerified = DateTime.Now.AddDays(-1);
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(VerificationErrorCodes.AlreadyVerified, result.ErrorCode);
        Assert.NotNull(result.VerifiedDate);
    }

    [Fact]
    public async Task VerifyEffortAsync_ReturnsError_WhenZeroEffortExists()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 0
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(VerificationErrorCodes.ZeroEffort, result.ErrorCode);
        Assert.NotNull(result.ZeroEffortCourses);
        Assert.Contains("VET 410-01", result.ZeroEffortCourses);
    }

    [Fact]
    public async Task VerifyEffortAsync_Succeeds_WhenNoEffortRecords()
    {
        // Arrange - Instructors with no records can verify "no effort" for the term
        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VerifiedDate);

        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(person.EffortVerified);

        await _auditServiceMock.Received(1).LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifiedEffort,
                Arg.Any<object?>(), Arg.Is<object>(o => o.ToString()!.Contains("VerifiedNoEffort")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEffortAsync_Succeeds_WhenValidEffortExists()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VerifiedDate);

        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(person.EffortVerified);

        await _auditServiceMock.Received(1).LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifiedEffort,
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEffortAsync_DeletesGenericRCourseWithZeroEffort()
    {
        // Arrange: Create a generic R-course
        var genericRCourse = new EffortCourse
        {
            Id = 998,
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "RES",
            CrseNumb = "000R",
            SeqNumb = "01",
            Enrollment = 0,
            Units = 0,
            CustDept = "VME"
        };
        _context.Courses.Add(genericRCourse);

        await _context.SaveChangesAsync();

        // Add generic R-course effort record with 0 hours and 0 weeks
        var genericRRecord = new EffortRecord
        {
            Id = 100,
            CourseId = 998,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 0,
            Weeks = 0,
            Course = genericRCourse
        };
        _context.Records.Add(genericRRecord);

        // Add regular course with non-zero effort (so verification can proceed)
        var regularRecord = new EffortRecord
        {
            Id = 101,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        };
        _context.Records.Add(regularRecord);

        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentPersonId().Returns(TestPersonId);

        // Verify initial state - generic R-course record exists
        var recordCountBefore = await _context.Records
            .CountAsync(r => r.PersonId == TestPersonId && r.TermCode == TestTermCode);
        Assert.Equal(2, recordCountBefore);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert: Verification succeeds
        Assert.True(result.Success);
        Assert.NotNull(result.VerifiedDate);

        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(person.EffortVerified);

        // Assert: Generic R-course record is deleted
        var recordCountAfter = await _context.Records
            .CountAsync(r => r.PersonId == TestPersonId && r.TermCode == TestTermCode);
        Assert.Equal(1, recordCountAfter);

        var remainingRecord = await _context.Records
            .Include(r => r.Course)
            .Where(r => r.PersonId == TestPersonId && r.TermCode == TestTermCode)
            .ToListAsync();
        Assert.Single(remainingRecord);
        Assert.Equal(TestCourseId, remainingRecord[0].CourseId);

        // Assert: Audit entry for RCourseAutoDeleted is created
        await _auditServiceMock.Received(1).LogRecordChangeAsync(
                100,
                TestTermCode,
                EffortAuditActions.RCourseAutoDeleted,
                Arg.Any<object?>(),
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>());
        // Assert: Audit entry for VerifiedEffort is also created
        await _auditServiceMock.Received(1).LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifiedEffort,
                Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region CanVerifyAsync Tests

    [Fact]
    public async Task CanVerifyAsync_ReturnsTrue_WhenNoRecords()
    {
        // Act - Instructors with no records can verify "no effort" for the term
        var result = await _service.CanVerifyAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.CanVerify);
        Assert.Equal(0, result.ZeroEffortCount);
    }

    [Fact]
    public async Task CanVerifyAsync_ReturnsFalse_WhenZeroEffortExists()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 0
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanVerifyAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.False(result.CanVerify);
        Assert.Equal(1, result.ZeroEffortCount);
        Assert.Contains(1, result.ZeroEffortRecordIds);
    }

    [Fact]
    public async Task CanVerifyAsync_ReturnsTrue_WhenAllRecordsHaveEffort()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanVerifyAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.CanVerify);
        Assert.Equal(0, result.ZeroEffortCount);
        Assert.Empty(result.ZeroEffortRecordIds);
    }

    [Fact]
    public async Task CanVerifyAsync_UsesClinicalWeeks_AfterThreshold()
    {
        // Arrange: Clinical effort should use Weeks after threshold term
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "CLI",
            RoleId = 1,
            Hours = 40,
            Weeks = 0
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanVerifyAsync(TestPersonId, TestTermCode);

        // Assert: Should be zero effort because Weeks=0 for CLI after 201604
        Assert.False(result.CanVerify);
        Assert.Equal(1, result.ZeroEffortCount);
    }

    [Fact]
    public async Task CanVerifyAsync_ExcludesGenericRCourse_FromZeroEffortCheck()
    {
        // Arrange: Add generic R-course with zero hours
        var genericRCourse = new EffortCourse
        {
            Id = 999,
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "VME",
            CrseNumb = "299",
            SeqNumb = "01",
            Enrollment = 0,
            Units = 0,
            CustDept = "VME"
        };
        _context.Courses.Add(genericRCourse);
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = 999,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 0
        });
        await _context.SaveChangesAsync();

        // Clear the change tracker so Include() must resolve from the database
        _context.ChangeTracker.Clear();

        // Act
        var result = await _service.CanVerifyAsync(TestPersonId, TestTermCode);

        // Assert: Generic R-course with zero hours should NOT prevent verification
        Assert.True(result.CanVerify);
        Assert.Equal(0, result.ZeroEffortCount);
        Assert.Empty(result.ZeroEffortRecordIds);
    }

    #endregion

    #region SendVerificationEmailAsync Tests

    [Fact]
    public async Task SendVerificationEmailAsync_ReturnsError_WhenInstructorNotFound()
    {
        // Arrange
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        // Act
        var result = await _service.SendVerificationEmailAsync(9999, TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Instructor not found", result.Error);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_ReturnsError_WhenNoSenderEmail()
    {
        // Arrange: Current user has no email
        _permissionServiceMock.GetCurrentUserEmail().ReturnsNull();

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unable to determine sender email address", result.Error);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_ReturnsError_WhenNoRecipientEmailAddress()
    {
        // Arrange: Current user has email, but recipient doesn't
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        var person = await _viperContext.People.FirstAsync(p => p.PersonId == TestPersonId);
        person.MailId = null;
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No email address found", result.Error);

        await _auditServiceMock.Received(1).LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifyEmail,
                Arg.Is<object?>(x => x == null), Arg.Is<object>(o => o.ToString()!.Contains("Failed")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendVerificationEmailAsync_ReturnsError_WhenBaseUrlNotConfigured()
    {
        // Arrange: Create service with missing BaseUrl configuration
        var badSettings = new EffortSettings
        {
            VerificationEmailSubject = "Please Verify Your Effort",
            VerificationReplyDays = 7
        };
        var badEmailSettings = new EmailSettings
        {
            BaseUrl = ""  // Missing/empty BaseUrl
        };

        var serviceWithBadConfig = new VerificationService(
            _context,
            _viperContext,
            _auditServiceMock,
            _permissionServiceMock,
            _termServiceMock,
            _emailServiceMock,
            _classificationServiceMock,
            _loggerMock,
            Options.Create(badSettings),
            Options.Create(badEmailSettings),
            _emailTemplateRendererMock);

        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        // Act
        var result = await serviceWithBadConfig.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email system configuration error. Please contact support.", result.Error);

        // Verify audit was logged for the configuration failure
        await _auditServiceMock.Received(1).LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifyEmail,
                Arg.Is<object?>(x => x == null), Arg.Is<object>(o => o.ToString()!.Contains("Configuration error")), Arg.Any<CancellationToken>());
        // Verify no email was attempted
        await _emailServiceMock.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SendVerificationEmailAsync_Succeeds_WhenValidEmail()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);

        // Verify email sent from current user (sender), not a static address
        await _emailServiceMock.Received(1).SendEmailAsync(
                "testuser@ucdavis.edu",
                _settings.VerificationEmailSubject,
                Arg.Any<string>(),
                true,
                "sender@ucdavis.edu");

        await _auditServiceMock.Received(1).LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifyEmail,
                Arg.Is<object?>(x => x == null), Arg.Is<object>(o => o.ToString()!.Contains("Success")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendVerificationEmailAsync_UpdatesLastEmailedFields_OnSuccess()
    {
        // Arrange
        var senderPersonId = 999;
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");
        _permissionServiceMock.GetCurrentPersonId().Returns(senderPersonId);

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Verify initial state - no LastEmailed data
        var personBefore = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.Null(personBefore.LastEmailed);
        Assert.Null(personBefore.LastEmailedBy);

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);

        // Reload entity to verify database was updated
        await _context.Entry(personBefore).ReloadAsync();
        Assert.NotNull(personBefore.LastEmailed);
        Assert.Equal(senderPersonId, personBefore.LastEmailedBy);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_SetsLastEmailedByToNull_WhenSenderIdIsZero()
    {
        // Arrange: Sender ID of 0 means user not found - should set LastEmailedBy to null to avoid FK violation
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");
        _permissionServiceMock.GetCurrentPersonId().Returns(0);

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);

        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(person.LastEmailed);
        Assert.Null(person.LastEmailedBy); // Should be null, not 0, to avoid FK violation
    }

    [Fact]
    public async Task SendVerificationEmailAsync_UsesFallbackDueDate_WhenNoExpectedCloseDate()
    {
        // Arrange: Term without ExpectedCloseDate - should fall back to Now + 7 days
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Capture the view model passed to the template renderer
        VerificationReminderViewModel? capturedViewModel = null;
        _emailTemplateRendererMock
            .RenderAsync<VerificationReminderViewModel>(
                Arg.Any<string>(),
                Arg.Any<VerificationReminderViewModel>(),
                Arg.Any<Dictionary<string, object>?>())
            .Returns(callInfo =>
            {
                capturedViewModel = callInfo.ArgAt<VerificationReminderViewModel>(1);
                return "<html>Mock email body</html>";
            });

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(capturedViewModel);

        // Fallback due date is Now + 7 days, formatted as M/d/yy
        var expectedDueDate = DateTime.Now.AddDays(_settings.VerificationReplyDays);
        Assert.Equal(expectedDueDate.ToString("M/d/yy"), capturedViewModel.ReplyByDate);
        Assert.False(capturedViewModel.IsPastDue);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_UsesExpectedCloseDateMinus7_ForDueDate()
    {
        // Arrange: Term with ExpectedCloseDate in the future
        var expectedCloseDate = DateTime.Now.AddDays(30);
        var effortTerm = await _context.Terms.FindAsync(TestTermCode);
        effortTerm!.ExpectedCloseDate = expectedCloseDate;
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Capture the view model passed to the template renderer
        VerificationReminderViewModel? capturedViewModel = null;
        _emailTemplateRendererMock
            .RenderAsync<VerificationReminderViewModel>(
                Arg.Any<string>(),
                Arg.Any<VerificationReminderViewModel>(),
                Arg.Any<Dictionary<string, object>?>())
            .Returns(callInfo =>
            {
                capturedViewModel = callInfo.ArgAt<VerificationReminderViewModel>(1);
                return "<html>Mock email body</html>";
            });

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(capturedViewModel);

        // Due date should be ExpectedCloseDate - 7 days
        var expectedDueDate = expectedCloseDate.AddDays(-_settings.VerificationReplyDays);
        Assert.Equal(expectedDueDate.ToString("M/d/yy"), capturedViewModel.ReplyByDate);
        Assert.False(capturedViewModel.IsPastDue);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_SetsIsPastDue_WhenDueDateHasPassed()
    {
        // Arrange: Term with ExpectedCloseDate in the past so due date is past
        // ExpectedCloseDate = 3 days ago => due date = 10 days ago (3 + 7), which is past
        var expectedCloseDate = DateTime.Now.AddDays(-3);
        var effortTerm = await _context.Terms.FindAsync(TestTermCode);
        effortTerm!.ExpectedCloseDate = expectedCloseDate;
        await _context.SaveChangesAsync();

        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Capture the view model passed to the template renderer
        VerificationReminderViewModel? capturedViewModel = null;
        _emailTemplateRendererMock
            .RenderAsync<VerificationReminderViewModel>(
                Arg.Any<string>(),
                Arg.Any<VerificationReminderViewModel>(),
                Arg.Any<Dictionary<string, object>?>())
            .Returns(callInfo =>
            {
                capturedViewModel = callInfo.ArgAt<VerificationReminderViewModel>(1);
                return "<html>Mock email body</html>";
            });

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(capturedViewModel);

        // Due date should be ExpectedCloseDate - 7 days = 10 days in the past
        var expectedDueDate = expectedCloseDate.AddDays(-_settings.VerificationReplyDays);
        Assert.Equal(expectedDueDate.ToString("M/d/yy"), capturedViewModel.ReplyByDate);
        Assert.True(capturedViewModel.IsPastDue);
    }

    #endregion

    #region SendBulkVerificationEmailsAsync Tests

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_ReturnsError_WhenNoPermission()
    {
        // Arrange
        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert
        Assert.Equal(0, result.TotalInstructors);
        Assert.Single(result.Failures);
        Assert.Contains("Access denied", result.Failures[0].Reason);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_SendsToUnverifiedOnly()
    {
        // Arrange
        // Add effort record for TestPersonId so email will be sent
        _context.Records.Add(new EffortRecord
        {
            Id = 100,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 10,
            Crn = "12345"
        });

        var verifiedPerson = new EffortPerson
        {
            PersonId = 200,
            TermCode = TestTermCode,
            FirstName = "Verified",
            LastName = "Person",
            EffortDept = "VME",
            EffortVerified = DateTime.Now
        };
        _context.Persons.Add(verifiedPerson);
        await _context.SaveChangesAsync();

        _viperContext.People.Add(new Viper.Models.VIPER.Person
        {
            PersonId = 200,
            MothraId = "verified",
            MailId = "verified",
            FirstName = "Verified",
            LastName = "Person",
            FullName = "Person, Verified",
            ClientId = "SVM",
            Current = 1
        });
        await _viperContext.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert: Should only send to TestPersonId (unverified), not to person 200 (verified)
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(1, result.EmailsSent);
        Assert.Equal(0, result.EmailsFailed);

        await _emailServiceMock.Received(1).SendEmailAsync("testuser@ucdavis.edu", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>());
        await _emailServiceMock.DidNotReceive().SendEmailAsync("verified@ucdavis.edu", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_ExcludesRecentlyEmailed_WhenFlagIsFalse()
    {
        // Arrange: Set LastEmailed to recent date (within 7-day reply period)
        var person = await _context.Persons.FindAsync(TestPersonId, TestTermCode);
        person!.LastEmailed = DateTime.Now.AddDays(-3); // 3 days ago, within 7-day window
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);

        // Act: Call without includeRecentlyEmailed (defaults to false)
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert: Should not include recently emailed instructor
        Assert.Equal(0, result.TotalInstructors);
        Assert.Equal(0, result.EmailsSent);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_IncludesRecentlyEmailed_WhenFlagIsTrue()
    {
        // Arrange: Set LastEmailed to recent date (within 7-day reply period)
        var person = await _context.Persons.FindAsync(TestPersonId, TestTermCode);
        person!.LastEmailed = DateTime.Now.AddDays(-3); // 3 days ago, within 7-day window
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act: Call with includeRecentlyEmailed = true
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode, includeRecentlyEmailed: true);

        // Assert: Should include the recently emailed instructor
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(1, result.EmailsSent);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_IncludesOldEmailed_WhenFlagIsFalse()
    {
        // Arrange: Set LastEmailed to old date (outside 7-day reply period)
        var person = await _context.Persons.FindAsync(TestPersonId, TestTermCode);
        person!.LastEmailed = DateTime.Now.AddDays(-10); // 10 days ago, outside 7-day window
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");

        _emailServiceMock
            .SendEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);

        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act: Call without includeRecentlyEmailed (defaults to false)
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert: Should include instructors emailed outside the reply window
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(1, result.EmailsSent);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_ReturnsEmailedPersonIds()
    {
        // Arrange
        _context.Records.Add(new EffortRecord
        {
            Id = 100,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 10,
            Crn = "12345"
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");
        _emailServiceMock
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);
        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert
        Assert.Single(result.EmailedPersonIds);
        Assert.Contains(TestPersonId, result.EmailedPersonIds);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_UseDueDateLogic_ExcludesEmailedBeforeDeadline()
    {
        // Arrange: Term has ExpectedCloseDate far in the future, instructor was emailed recently
        var effortTerm = await _context.Terms.FindAsync(TestTermCode);
        effortTerm!.ExpectedCloseDate = DateTime.Now.AddDays(30);
        var person = await _context.Persons.FindAsync(TestPersonId, TestTermCode);
        person!.LastEmailed = DateTime.Now.AddDays(-3);
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);

        // Act: Before deadline → all emailed instructors are "recently emailed"
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert: Should exclude the emailed instructor
        Assert.Equal(0, result.TotalInstructors);
        Assert.Equal(0, result.EmailsSent);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_UseDueDateLogic_IncludesEmailedPastDeadline()
    {
        // Arrange: Term has ExpectedCloseDate in the past, instructor was emailed recently
        var effortTerm = await _context.Terms.FindAsync(TestTermCode);
        effortTerm!.ExpectedCloseDate = DateTime.Now.AddDays(-1);
        var person = await _context.Persons.FindAsync(TestPersonId, TestTermCode);
        person!.LastEmailed = DateTime.Now.AddDays(-3);
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 100,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 10,
            Crn = "12345"
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");
        _emailServiceMock
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);
        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act: Past deadline → all instructors eligible for resend
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert: Should include the recently emailed instructor
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(1, result.EmailsSent);
    }

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_UseDueDateLogic_IncludesNeverEmailed()
    {
        // Arrange: Term has ExpectedCloseDate far in the future, instructor was NEVER emailed
        var effortTerm = await _context.Terms.FindAsync(TestTermCode);
        effortTerm!.ExpectedCloseDate = DateTime.Now.AddDays(30);
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 100,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 10,
            Crn = "12345"
        });
        await _context.SaveChangesAsync();

        _permissionServiceMock.CanViewDepartmentAsync("VME", Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.GetCurrentUserEmail().Returns("sender@ucdavis.edu");
        _emailServiceMock
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.CompletedTask);
        _termServiceMock.GetTermName(TestTermCode).Returns("Fall 2024");

        // Act: Before deadline but never emailed → should still be included
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(1, result.EmailsSent);
    }

    #endregion

    #region GetEmailHistoryAsync Tests

    [Fact]
    public async Task GetEmailHistoryAsync_ReturnsEmptyList_WhenNoHistory()
    {
        // Act
        var result = await _service.GetEmailHistoryAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEmailHistoryAsync_ReturnsHistory_WhenExists()
    {
        // Arrange: Add audit entry for email send
        _context.Audits.Add(new Audit
        {
            Id = 1,
            TableName = EffortAuditTables.Persons,
            RecordId = TestPersonId,
            TermCode = TestTermCode,
            Action = EffortAuditActions.VerifyEmail,
            ChangedBy = TestPersonId,
            ChangedDate = DateTime.Now,
            Changes = "{\"RecipientEmail\":{\"OldValue\":null,\"NewValue\":\"test@ucdavis.edu\"},\"RecipientName\":{\"OldValue\":null,\"NewValue\":\"Test User\"}}"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEmailHistoryAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.Single(result);
        Assert.Equal("test@ucdavis.edu", result[0].RecipientEmail);
        Assert.Equal("Test User", result[0].RecipientName);
    }

    #endregion
}
