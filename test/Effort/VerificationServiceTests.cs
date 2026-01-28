using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
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
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ITermService> _termServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<VerificationService>> _loggerMock;
    private readonly EffortSettings _settings;
    private readonly Mock<IEmailTemplateRenderer> _emailTemplateRendererMock;
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

        _auditServiceMock = new Mock<IEffortAuditService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _termServiceMock = new Mock<ITermService>();
        _emailServiceMock = new Mock<IEmailService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<VerificationService>>();

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

        _emailTemplateRendererMock = new Mock<IEmailTemplateRenderer>();
        _emailTemplateRendererMock
            .Setup(r => r.RenderAsync<VerificationReminderViewModel>(
                It.IsAny<string>(),
                It.IsAny<VerificationReminderViewModel>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync("<html>Mock email body</html>");

        _auditServiceMock
            .Setup(s => s.LogPersonChangeAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new VerificationService(
            _context,
            _viperContext,
            _auditServiceMock.Object,
            _permissionServiceMock.Object,
            _termServiceMock.Object,
            _emailServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            settingsOptions,
            emailSettingsOptions,
            _emailTemplateRendererMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, Status = "Open" });

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
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(0);

        // Act
        var result = await _service.GetMyEffortAsync(TestTermCode);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMyEffortAsync_ReturnsNull_WhenNoInstructorRecord()
    {
        // Arrange
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(9999);

        // Act
        var result = await _service.GetMyEffortAsync(TestTermCode);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMyEffortAsync_ReturnsDto_WhenInstructorExists()
    {
        // Arrange
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(TestPersonId);
        _permissionServiceMock.Setup(p => p.HasSelfServiceAccessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissionServiceMock.Setup(p => p.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _termServiceMock.Setup(t => t.GetTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TermDto { TermCode = TestTermCode, Status = "Opened" });
        _termServiceMock.Setup(t => t.GetTermName(TestTermCode)).Returns("Fall 2024");

        _mapperMock.Setup(m => m.Map<PersonDto>(It.IsAny<EffortPerson>()))
            .Returns(new PersonDto { PersonId = TestPersonId, FirstName = "Test", LastName = "Instructor" });
        _mapperMock.Setup(m => m.Map<List<InstructorEffortRecordDto>>(It.IsAny<List<EffortRecord>>()))
            .Returns(new List<InstructorEffortRecordDto>());

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

        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(TestPersonId);
        _permissionServiceMock.Setup(p => p.HasSelfServiceAccessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissionServiceMock.Setup(p => p.CanEditPersonEffortAsync(TestPersonId, TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _termServiceMock.Setup(t => t.GetTermAsync(TestTermCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TermDto { TermCode = TestTermCode, Status = "Opened" });
        _termServiceMock.Setup(t => t.GetTermName(TestTermCode)).Returns("Fall 2024");

        _mapperMock.Setup(m => m.Map<PersonDto>(It.IsAny<EffortPerson>()))
            .Returns(new PersonDto { PersonId = TestPersonId });
        _mapperMock.Setup(m => m.Map<List<InstructorEffortRecordDto>>(It.IsAny<List<EffortRecord>>()))
            .Returns(new List<InstructorEffortRecordDto> { new() { Id = 1, CourseId = TestCourseId } });

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
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(0);

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
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(9999);

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

        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(TestPersonId);

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

        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(TestPersonId);

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
        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(TestPersonId);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VerifiedDate);

        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(person.EffortVerified);

        _auditServiceMock.Verify(
            a => a.LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifiedEffort,
                It.IsAny<object?>(), It.Is<object>(o => o.ToString()!.Contains("VerifiedNoEffort")), It.IsAny<CancellationToken>()),
            Times.Once);
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

        _permissionServiceMock.Setup(p => p.GetCurrentPersonId()).Returns(TestPersonId);

        // Act
        var result = await _service.VerifyEffortAsync(TestTermCode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VerifiedDate);

        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(person.EffortVerified);

        _auditServiceMock.Verify(
            a => a.LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifiedEffort,
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()),
            Times.Once);
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

    #endregion

    #region SendVerificationEmailAsync Tests

    [Fact]
    public async Task SendVerificationEmailAsync_ReturnsError_WhenInstructorNotFound()
    {
        // Arrange
        _permissionServiceMock.Setup(p => p.GetCurrentUserEmail()).Returns("sender@ucdavis.edu");

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
        _permissionServiceMock.Setup(p => p.GetCurrentUserEmail()).Returns((string?)null);

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
        _permissionServiceMock.Setup(p => p.GetCurrentUserEmail()).Returns("sender@ucdavis.edu");

        var person = await _viperContext.People.FirstAsync(p => p.PersonId == TestPersonId);
        person.MailId = null;
        await _viperContext.SaveChangesAsync();

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No email address found", result.Error);

        _auditServiceMock.Verify(
            a => a.LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifyEmail,
                null, It.Is<object>(o => o.ToString()!.Contains("Failed")), It.IsAny<CancellationToken>()),
            Times.Once);
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
            _auditServiceMock.Object,
            _permissionServiceMock.Object,
            _termServiceMock.Object,
            _emailServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            Options.Create(badSettings),
            Options.Create(badEmailSettings),
            _emailTemplateRendererMock.Object);

        _permissionServiceMock.Setup(p => p.GetCurrentUserEmail()).Returns("sender@ucdavis.edu");

        // Act
        var result = await serviceWithBadConfig.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email system configuration error. Please contact support.", result.Error);

        // Verify audit was logged for the configuration failure
        _auditServiceMock.Verify(
            a => a.LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifyEmail,
                null, It.Is<object>(o => o.ToString()!.Contains("Configuration error")), It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify no email was attempted
        _emailServiceMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Never);
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

        _permissionServiceMock.Setup(p => p.GetCurrentUserEmail()).Returns("sender@ucdavis.edu");

        _emailServiceMock
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _termServiceMock.Setup(t => t.GetTermName(TestTermCode)).Returns("Fall 2024");

        // Act
        var result = await _service.SendVerificationEmailAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.True(result.Success);

        // Verify email sent from current user (sender), not a static address
        _emailServiceMock.Verify(
            e => e.SendEmailAsync(
                "testuser@ucdavis.edu",
                _settings.VerificationEmailSubject,
                It.IsAny<string>(),
                true,
                "sender@ucdavis.edu"),
            Times.Once);

        _auditServiceMock.Verify(
            a => a.LogPersonChangeAsync(
                TestPersonId, TestTermCode, EffortAuditActions.VerifyEmail,
                null, It.Is<object>(o => o.ToString()!.Contains("Success")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region SendBulkVerificationEmailsAsync Tests

    [Fact]
    public async Task SendBulkVerificationEmailsAsync_ReturnsError_WhenNoPermission()
    {
        // Arrange
        _permissionServiceMock.Setup(p => p.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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

        _permissionServiceMock.Setup(p => p.CanViewDepartmentAsync("VME", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissionServiceMock.Setup(p => p.GetCurrentUserEmail()).Returns("sender@ucdavis.edu");

        _emailServiceMock
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _termServiceMock.Setup(t => t.GetTermName(TestTermCode)).Returns("Fall 2024");

        // Act
        var result = await _service.SendBulkVerificationEmailsAsync("VME", TestTermCode);

        // Assert: Should only send to TestPersonId (unverified), not to person 200 (verified)
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(1, result.EmailsSent);
        Assert.Equal(0, result.EmailsFailed);

        _emailServiceMock.Verify(
            e => e.SendEmailAsync("testuser@ucdavis.edu", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Once);
        _emailServiceMock.Verify(
            e => e.SendEmailAsync("verified@ucdavis.edu", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Never);
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
            Changes = "{\"RecipientEmail\":\"test@ucdavis.edu\",\"RecipientName\":\"Test User\"}"
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
