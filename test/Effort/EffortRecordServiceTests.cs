using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Exceptions;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for EffortRecordService CRUD operations.
/// </summary>
public sealed class EffortRecordServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<IInstructorService> _instructorServiceMock;
    private readonly Mock<IRCourseService> _rCourseServiceMock;
    private readonly Mock<IUserHelper> _userHelperMock;
    private readonly Mock<ILogger<EffortRecordService>> _loggerMock;
    private readonly EffortRecordService _service;

    private const int TestTermCode = 202410;
    private const int TestPersonId = 100;
    private const int TestCourseId = 1;
    private const int TestUserId = 999;

    public EffortRecordServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var rapsOptions = new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EffortDbContext(effortOptions);
        _rapsContext = new RAPSContext(rapsOptions);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _userHelperMock = new Mock<IUserHelper>();

        _auditServiceMock
            .Setup(s => s.LogRecordChangeAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _instructorServiceMock = new Mock<IInstructorService>();
        _rCourseServiceMock = new Mock<IRCourseService>();
        _loggerMock = new Mock<ILogger<EffortRecordService>>();

        var testUser = new AaudUser { AaudUserId = TestUserId, MothraId = "testuser" };
        _userHelperMock.Setup(x => x.GetCurrentUser()).Returns(testUser);

        var courseClassificationService = new CourseClassificationService();

        _service = new EffortRecordService(
            _context,
            _rapsContext,
            _auditServiceMock.Object,
            _instructorServiceMock.Object,
            courseClassificationService,
            _rCourseServiceMock.Object,
            _userHelperMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // OpenedDate makes status "Opened"
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, OpenedDate = DateTime.Now });

        _context.EffortTypes.AddRange(
            new EffortType { Id = "ADM", Description = "Admin", IsActive = true, UsesWeeks = false, AllowedOnRCourses = true },
            new EffortType { Id = "LEC", Description = "Lecture", IsActive = true, UsesWeeks = false },
            new EffortType { Id = "LAB", Description = "Laboratory", IsActive = true, UsesWeeks = false },
            new EffortType { Id = "CLI", Description = "Clinical", IsActive = true, UsesWeeks = true },
            new EffortType { Id = "OLD", Description = "Deprecated", IsActive = false },
            new EffortType { Id = "NDV", Description = "Non-DVM Only", IsActive = true, UsesWeeks = false, AllowedOnDvm = false },
            new EffortType { Id = "N19", Description = "No 199/299", IsActive = true, UsesWeeks = false, AllowedOn199299 = false },
            new EffortType { Id = "NRC", Description = "No R-Courses", IsActive = true, UsesWeeks = false, AllowedOnRCourses = false }
        );

        _context.Roles.AddRange(
            new EffortRole { Id = 1, Description = "Instructor of Record", IsActive = true, SortOrder = 1 },
            new EffortRole { Id = 2, Description = "Co-Instructor", IsActive = true, SortOrder = 2 },
            new EffortRole { Id = 3, Description = "Retired Role", IsActive = false }
        );

        _context.Courses.AddRange(
            new EffortCourse { Id = TestCourseId, TermCode = TestTermCode, Crn = "12345", SubjCode = "APC", CrseNumb = "410", SeqNumb = "01", Enrollment = 20, Units = 4, CustDept = "APC" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "01", Enrollment = 15, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "DVM", CrseNumb = "199", SeqNumb = "01", Enrollment = 5, Units = 1, CustDept = "DVM" },
            new EffortCourse { Id = 4, TermCode = TestTermCode, Crn = "12348", SubjCode = "DVM", CrseNumb = "290R", SeqNumb = "01", Enrollment = 8, Units = 2, CustDept = "DVM" }
        );

        _context.Persons.Add(new EffortPerson
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            FirstName = "Test",
            LastName = "Instructor",
            EffortDept = "VME"
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    #region GetEffortRecordAsync Tests

    [Fact]
    public async Task GetEffortRecordAsync_ReturnsRecord_WhenExists()
    {
        // Arrange
        var record = new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        };
        _context.Records.Add(record);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEffortRecordAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("LEC", result.EffortType);
        Assert.Equal(40, result.Hours);
    }

    [Fact]
    public async Task GetEffortRecordAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetEffortRecordAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEffortRecordAsync_PopulatesDvmFlag_ForDvmCourse()
    {
        // Arrange: Course 2 is a DVM course (SubjCode = "DVM")
        var record = new EffortRecord
        {
            Id = 1,
            CourseId = 2,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        };
        _context.Records.Add(record);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEffortRecordAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Course.IsDvm);
        Assert.False(result.Course.Is199299);
        Assert.False(result.Course.IsRCourse);
    }

    [Fact]
    public async Task GetEffortRecordAsync_Populates199299Flag_For199Course()
    {
        // Arrange: Course 3 is DVM 199 (SubjCode="DVM", CrseNumb="199")
        var record = new EffortRecord
        {
            Id = 1,
            CourseId = 3,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        };
        _context.Records.Add(record);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEffortRecordAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Course.IsDvm);
        Assert.True(result.Course.Is199299);
        Assert.False(result.Course.IsRCourse);
    }

    [Fact]
    public async Task GetEffortRecordAsync_PopulatesRCourseFlag_ForRCourse()
    {
        // Arrange: Course 4 is DVM 290R (SubjCode="DVM", CrseNumb="290R")
        var record = new EffortRecord
        {
            Id = 1,
            CourseId = 4,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40
        };
        _context.Records.Add(record);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEffortRecordAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Course.IsDvm);
        Assert.False(result.Course.Is199299);
        Assert.True(result.Course.IsRCourse);
    }

    #endregion

    #region CreateEffortRecordAsync Tests

    [Fact]
    public async Task CreateEffortRecordAsync_CreatesRecord_WithValidData()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act
        var (result, warning) = await _service.CreateEffortRecordAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LEC", result.EffortType);
        Assert.Equal(40, result.Hours);
        Assert.Null(result.Weeks);
        Assert.Null(warning);

        _auditServiceMock.Verify(
            s => s.LogRecordChangeAsync(
                result.Id, TestTermCode, EffortAuditActions.CreateEffort,
                null, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenPersonNotFound()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = 9999,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Set up the instructor service to throw when person not found in VIPER
        _instructorServiceMock
            .Setup(s => s.CreateInstructorAsync(
                It.IsAny<CreateInstructorRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Person with PersonId {request.PersonId} not found"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("Failed to create instructor for PersonId 9999", ex.Message);
        Assert.Contains("Person with PersonId 9999 not found", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenCourseNotFound()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 9999,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("Course 9999 not found", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenEffortTypeInactive()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "OLD",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("Effort type 'OLD' not found or inactive", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenRoleInactive()
    {
        // Arrange
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 3,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("Role 3 not found or inactive", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenDuplicateRecord()
    {
        // Arrange: Create an existing record
        _context.Records.Add(new EffortRecord
        {
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_CoercesRole_WhenExistingRecordsHaveDifferentRole()
    {
        // Arrange: Create an existing record with role 1
        _context.Records.Add(new EffortRecord
        {
            Id = 10,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 10
        };

        // Act
        var (_, warning) = await _service.CreateEffortRecordAsync(request);

        // Assert
        Assert.NotNull(warning);
        Assert.Contains("Role was updated", warning);

        var existingRecord = await _context.Records.FindAsync(10);
        Assert.Equal(2, existingRecord!.RoleId);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ClearsEffortVerified_OnPerson()
    {
        // Arrange
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        person.EffortVerified = DateTime.Now;
        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act
        await _service.CreateEffortRecordAsync(request);

        // Assert
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.Null(updatedPerson.EffortVerified);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_PreservesEffortVerified_OnSelfEdit()
    {
        // Arrange: Set up a verified instructor
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        var originalVerificationDate = DateTime.Now.AddDays(-1);
        person.EffortVerified = originalVerificationDate;
        await _context.SaveChangesAsync();

        // Mock the current user as the instructor themselves (self-edit)
        var selfUser = new AaudUser { AaudUserId = TestPersonId, MothraId = "selfinstructor" };
        _userHelperMock.Setup(x => x.GetCurrentUser()).Returns(selfUser);

        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act
        await _service.CreateEffortRecordAsync(request);

        // Assert: Verification should be preserved for self-edit
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(updatedPerson.EffortVerified);
        Assert.Equal(originalVerificationDate, updatedPerson.EffortVerified);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenCourseIsChild()
    {
        // Arrange: Make course 1 a child of course 2
        _context.CourseRelationships.Add(new CourseRelationship
        {
            ParentCourseId = 2,
            ChildCourseId = TestCourseId,
            RelationshipType = "Parent"
        });
        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("Cannot add effort to a child course", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_Succeeds_WhenCourseIsParent()
    {
        // Arrange: Make course 1 a parent of course 2
        // This test ensures parent courses can still receive effort
        // (would fail if we incorrectly check ParentRelationships instead of ChildRelationships)
        _context.CourseRelationships.Add(new CourseRelationship
        {
            ParentCourseId = TestCourseId,
            ChildCourseId = 2,
            RelationshipType = "Parent"
        });
        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert - parent course should accept effort
        Assert.NotNull(result);
        Assert.Equal(TestCourseId, result.CourseId);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenEffortTypeNotAllowedOnDvm()
    {
        // Arrange: Course 2 is a DVM course, NDV effort type is not allowed on DVM
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 2,
            EffortTypeId = "NDV",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("not allowed on DVM courses", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenEffortTypeNotAllowedOn199299()
    {
        // Arrange: Course 3 is a 199 course, N19 effort type is not allowed on 199/299
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 3,
            EffortTypeId = "N19",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("not allowed on 199/299 courses", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_ThrowsInvalidOperation_WhenEffortTypeNotAllowedOnRCourse()
    {
        // Arrange: Course 4 is an R course, NRC effort type is not allowed on R courses
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 4,
            EffortTypeId = "NRC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("not allowed on R courses", ex.Message);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_Succeeds_WhenEffortTypeAllowedOnCourse()
    {
        // Arrange: LEC is allowed on all course types, course 2 is DVM
        var request = new CreateEffortRecordRequest
        {
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            CourseId = 2,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LEC", result.EffortType);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_HandlesRaceCondition_WhenInstructorCreatedConcurrently()
    {
        // Arrange - person does NOT exist initially in Effort DB
        var newPersonId = 999;
        var request = new CreateEffortRecordRequest
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId,
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Simulate race condition: CreateInstructorAsync throws because another request already created the instructor
        _instructorServiceMock
            .Setup(s => s.CreateInstructorAsync(
                It.Is<CreateInstructorRequest>(r => r.PersonId == newPersonId && r.TermCode == TestTermCode),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Areas.Effort.Exceptions.InstructorAlreadyExistsException(newPersonId, TestTermCode));

        // After the exception, the person now exists (created by the concurrent request)
        _context.Persons.Add(new EffortPerson
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            FirstName = "Concurrent",
            LastName = "Instructor",
            EffortDept = "VME"
        });
        await _context.SaveChangesAsync();

        // Act - should catch the exception and continue successfully
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newPersonId, result.PersonId);
        Assert.Equal("LEC", result.EffortType);
        Assert.Equal(40, result.Hours);
    }

    #endregion

    #region UpdateEffortRecordAsync Tests

    [Fact]
    public async Task UpdateEffortRecordAsync_UpdatesRecord_WithValidData()
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
            Hours = 20
        });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30
        };

        // Act
        var (result, warning) = await _service.UpdateEffortRecordAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LAB", result.EffortType);
        Assert.Equal(30, result.Hours);
        Assert.Equal(2, result.Role);
        Assert.Null(warning);

        _auditServiceMock.Verify(
            s => s.LogRecordChangeAsync(
                1, TestTermCode, EffortAuditActions.UpdateEffort,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_ReturnsNull_WhenRecordNotFound()
    {
        // Arrange
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act
        var (result, _) = await _service.UpdateEffortRecordAsync(999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_ThrowsInvalidOperation_WhenDuplicateEffortType()
    {
        // Arrange: Create two records
        _context.Records.AddRange(
            new EffortRecord
            {
                Id = 1,
                CourseId = TestCourseId,
                PersonId = TestPersonId,
                TermCode = TestTermCode,
                EffortTypeId = "LEC",
                RoleId = 1,
                Hours = 20
            },
            new EffortRecord
            {
                Id = 2,
                CourseId = TestCourseId,
                PersonId = TestPersonId,
                TermCode = TestTermCode,
                EffortTypeId = "LAB",
                RoleId = 1,
                Hours = 10
            }
        );
        await _context.SaveChangesAsync();

        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 1,
            EffortValue = 25
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateEffortRecordAsync(1, request));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_ThrowsInvalidOperation_WhenEffortTypeNotAllowedOnCourse()
    {
        // Arrange: Create a record on DVM course (course 2)
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = 2,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        // Try to change to NDV which is not allowed on DVM
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "NDV",
            RoleId = 1,
            EffortValue = 25
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateEffortRecordAsync(1, request));
        Assert.Contains("not allowed on DVM courses", ex.Message);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_ClearsEffortVerified_OnPerson()
    {
        // Arrange: Set up a verified instructor with an existing record
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        person.EffortVerified = DateTime.Now;
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 30  // Change hours from 20 to 30
        };

        // Act
        await _service.UpdateEffortRecordAsync(1, request);

        // Assert
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.Null(updatedPerson.EffortVerified);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_PreservesEffortVerified_OnSelfEdit()
    {
        // Arrange: Set up a verified instructor with an existing record
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        var originalVerificationDate = DateTime.Now.AddDays(-1);
        person.EffortVerified = originalVerificationDate;
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        // Mock the current user as the instructor themselves (self-edit)
        var selfUser = new AaudUser { AaudUserId = TestPersonId, MothraId = "selfinstructor" };
        _userHelperMock.Setup(x => x.GetCurrentUser()).Returns(selfUser);

        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 30  // Change hours from 20 to 30
        };

        // Act
        await _service.UpdateEffortRecordAsync(1, request);

        // Assert: Verification should be preserved for self-edit
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(updatedPerson.EffortVerified);
        Assert.Equal(originalVerificationDate, updatedPerson.EffortVerified);
    }

    #endregion

    #region DeleteEffortRecordAsync Tests

    [Fact]
    public async Task DeleteEffortRecordAsync_DeletesRecord_WhenExists()
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
            Hours = 20
        });
        await _context.SaveChangesAsync();

        // Act - pass null for originalModifiedDate (legacy record)
        var result = await _service.DeleteEffortRecordAsync(1, null);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Records.FindAsync(1));

        _auditServiceMock.Verify(
            s => s.LogRecordChangeAsync(
                1, TestTermCode, EffortAuditActions.DeleteEffort,
                It.IsAny<object>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEffortRecordAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _service.DeleteEffortRecordAsync(999, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEffortRecordAsync_ClearsEffortVerified_OnPerson()
    {
        // Arrange
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        person.EffortVerified = DateTime.Now;
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        // Act - pass null for originalModifiedDate (legacy record)
        await _service.DeleteEffortRecordAsync(1, null);

        // Assert
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.Null(updatedPerson.EffortVerified);
    }

    [Fact]
    public async Task DeleteEffortRecordAsync_PreservesEffortVerified_OnSelfEdit()
    {
        // Arrange: Set up a verified instructor with an existing record
        var person = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        var originalVerificationDate = DateTime.Now.AddDays(-1);
        person.EffortVerified = originalVerificationDate;
        await _context.SaveChangesAsync();

        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        // Mock the current user as the instructor themselves (self-edit)
        var selfUser = new AaudUser { AaudUserId = TestPersonId, MothraId = "selfinstructor" };
        _userHelperMock.Setup(x => x.GetCurrentUser()).Returns(selfUser);

        // Act - pass null for originalModifiedDate (legacy record)
        await _service.DeleteEffortRecordAsync(1, null);

        // Assert: Verification should be preserved for self-edit
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.NotNull(updatedPerson.EffortVerified);
        Assert.Equal(originalVerificationDate, updatedPerson.EffortVerified);
    }

    #endregion

    #region Concurrency Conflict Tests

    [Fact]
    public async Task UpdateEffortRecordAsync_ThrowsConcurrencyConflict_WhenModifiedDateMismatch()
    {
        // Arrange: Create record with a ModifiedDate
        var originalDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20,
            ModifiedDate = originalDate
        });
        await _context.SaveChangesAsync();

        // Request with a DIFFERENT ModifiedDate (simulating stale data)
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30,
            OriginalModifiedDate = new DateTime(2024, 1, 14, 10, 30, 0, DateTimeKind.Local) // One day earlier
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.UpdateEffortRecordAsync(1, request));
        Assert.Contains("modified by another user", ex.Message);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_Succeeds_WhenModifiedDateMatches()
    {
        // Arrange: Create record with a ModifiedDate
        var originalDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20,
            ModifiedDate = originalDate
        });
        await _context.SaveChangesAsync();

        // Request with the SAME ModifiedDate
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30,
            OriginalModifiedDate = originalDate
        };

        // Act
        var (result, _) = await _service.UpdateEffortRecordAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LAB", result.EffortType);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_Succeeds_WhenBothModifiedDatesAreNull()
    {
        // Arrange: Create legacy record with NULL ModifiedDate
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20,
            ModifiedDate = null // Legacy record
        });
        await _context.SaveChangesAsync();

        // Request with NULL OriginalModifiedDate
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30,
            OriginalModifiedDate = null
        };

        // Act
        var (result, _) = await _service.UpdateEffortRecordAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LAB", result.EffortType);
    }

    [Fact]
    public async Task UpdateEffortRecordAsync_ThrowsConcurrencyConflict_WhenLegacyRecordWasUpdated()
    {
        // Arrange: Create record that WAS legacy but has since been updated
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20,
            ModifiedDate = DateTime.Now // Record was updated after loading
        });
        await _context.SaveChangesAsync();

        // Request still has NULL (thinks record is legacy)
        var request = new UpdateEffortRecordRequest
        {
            EffortTypeId = "LAB",
            RoleId = 2,
            EffortValue = 30,
            OriginalModifiedDate = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.UpdateEffortRecordAsync(1, request));
        Assert.Contains("modified by another user", ex.Message);
    }

    [Fact]
    public async Task DeleteEffortRecordAsync_ThrowsConcurrencyConflict_WhenModifiedDateMismatch()
    {
        // Arrange: Create record with a ModifiedDate
        var originalDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20,
            ModifiedDate = originalDate
        });
        await _context.SaveChangesAsync();

        // Act & Assert: Try to delete with stale date
        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.DeleteEffortRecordAsync(1, new DateTime(2024, 1, 14, 10, 30, 0, DateTimeKind.Local)));
        Assert.Contains("modified by another user", ex.Message);

        // Verify record was NOT deleted
        Assert.NotNull(await _context.Records.FindAsync(1));
    }

    [Fact]
    public async Task DeleteEffortRecordAsync_Succeeds_WhenModifiedDateMatches()
    {
        // Arrange: Create record with a ModifiedDate
        var originalDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);
        _context.Records.Add(new EffortRecord
        {
            Id = 1,
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20,
            ModifiedDate = originalDate
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteEffortRecordAsync(1, originalDate);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Records.FindAsync(1));
    }

    #endregion

    #region GetAvailableCoursesAsync Tests

    [Fact]
    public async Task GetAvailableCoursesAsync_ReturnsCoursesGrouped()
    {
        // Arrange: Add a record to make course 1 "existing"
        _context.Records.Add(new EffortRecord
        {
            CourseId = TestCourseId,
            PersonId = TestPersonId,
            TermCode = TestTermCode,
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 20
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailableCoursesAsync(TestPersonId, TestTermCode);

        // Assert
        Assert.Single(result.ExistingCourses);
        Assert.Equal(4, result.AllCourses.Count);
    }

    [Fact]
    public async Task GetAvailableCoursesAsync_ExcludesChildCourses()
    {
        // Arrange: Make course 2 a child of course 1
        _context.CourseRelationships.Add(new CourseRelationship
        {
            ParentCourseId = TestCourseId,
            ChildCourseId = 2,
            RelationshipType = "Parent"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailableCoursesAsync(TestPersonId, TestTermCode);

        // Assert: 4 courses seeded, 1 excluded as child = 3 remaining
        Assert.Equal(3, result.AllCourses.Count);
        Assert.DoesNotContain(result.AllCourses, c => c.Id == 2);
    }

    [Fact]
    public async Task GetAvailableCoursesAsync_PopulatesClassificationFlags()
    {
        // Act
        var result = await _service.GetAvailableCoursesAsync(TestPersonId, TestTermCode);

        // Assert: Course 1 (APC 410) - IsDvm=false (non-DVM subject code)
        var apcCourse = result.AllCourses.First(c => c.Id == TestCourseId);
        Assert.False(apcCourse.IsDvm);
        Assert.False(apcCourse.Is199299);
        Assert.False(apcCourse.IsRCourse);

        // Assert: Course 2 (DVM 443) - IsDvm=true
        var dvmCourse = result.AllCourses.First(c => c.Id == 2);
        Assert.True(dvmCourse.IsDvm);
        Assert.False(dvmCourse.Is199299);
        Assert.False(dvmCourse.IsRCourse);

        // Assert: Course 3 (DVM 199) - IsDvm=true, Is199299=true
        var course199 = result.AllCourses.First(c => c.Id == 3);
        Assert.True(course199.IsDvm);
        Assert.True(course199.Is199299);
        Assert.False(course199.IsRCourse);

        // Assert: Course 4 (DVM 290R) - IsDvm=true, IsRCourse=true
        var rCourse = result.AllCourses.First(c => c.Id == 4);
        Assert.True(rCourse.IsDvm);
        Assert.False(rCourse.Is199299);
        Assert.True(rCourse.IsRCourse);
    }

    #endregion

    #region GetEffortTypeOptionsAsync Tests

    [Fact]
    public async Task GetEffortTypeOptionsAsync_ReturnsActiveTypesOnly()
    {
        // Act
        var result = await _service.GetEffortTypeOptionsAsync();

        // Assert: 8 seeded, 1 inactive (OLD) = 7 active
        Assert.Equal(7, result.Count);
        Assert.All(result, et => Assert.NotEqual("OLD", et.Id));
    }

    #endregion

    #region GetRoleOptionsAsync Tests

    [Fact]
    public async Task GetRoleOptionsAsync_ReturnsActiveRolesOnly()
    {
        // Act
        var result = await _service.GetRoleOptionsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.NotEqual(3, r.Id));
    }

    [Fact]
    public async Task GetRoleOptionsAsync_ReturnsSortedBySortOrder()
    {
        // Act
        var result = await _service.GetRoleOptionsAsync();

        // Assert
        Assert.Equal("Instructor of Record", result[0].Description);
        Assert.Equal("Co-Instructor", result[1].Description);
    }

    #endregion

    #region CanEditTermAsync Tests

    [Fact]
    public async Task CanEditTermAsync_ReturnsTrue_WhenTermIsOpened()
    {
        // Act
        var result = await _service.CanEditTermAsync(TestTermCode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditTermAsync_ReturnsFalse_WhenTermNotFound()
    {
        // Act
        var result = await _service.CanEditTermAsync(999999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditTermAsync_ReturnsFalse_WhenTermClosedAndNoPermission()
    {
        // Arrange
        // ClosedDate makes status "Closed"
        var closedTerm = new EffortTerm { TermCode = 202301, OpenedDate = DateTime.Now.AddDays(-30), ClosedDate = DateTime.Now.AddDays(-1) };
        _context.Terms.Add(closedTerm);
        await _context.SaveChangesAsync();

        _userHelperMock.Setup(x => x.HasPermission(
            It.IsAny<RAPSContext>(), It.IsAny<AaudUser>(), EffortPermissions.EditWhenClosed))
            .Returns(false);

        // Act
        var result = await _service.CanEditTermAsync(202301);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditTermAsync_ReturnsTrue_WhenTermClosedButHasPermission()
    {
        // Arrange
        // ClosedDate makes status "Closed"
        var closedTerm = new EffortTerm { TermCode = 202301, OpenedDate = DateTime.Now.AddDays(-30), ClosedDate = DateTime.Now.AddDays(-1) };
        _context.Terms.Add(closedTerm);
        await _context.SaveChangesAsync();

        _userHelperMock.Setup(x => x.HasPermission(
            It.IsAny<RAPSContext>(), It.IsAny<AaudUser>(), EffortPermissions.EditWhenClosed))
            .Returns(true);

        // Act
        var result = await _service.CanEditTermAsync(202301);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region UsesWeeks Tests

    [Fact]
    public void UsesWeeks_ReturnsFalse_ForNonClinical()
    {
        // Act & Assert
        Assert.False(_service.UsesWeeks("LEC", 202410));
        Assert.False(_service.UsesWeeks("LAB", 202410));
    }

    [Fact]
    public void UsesWeeks_ReturnsFalse_ForClinical_BeforeThreshold()
    {
        // CLI uses weeks starting at 201604
        // Act & Assert
        Assert.False(_service.UsesWeeks("CLI", 201603));
        Assert.False(_service.UsesWeeks("CLI", 201510));
    }

    [Fact]
    public void UsesWeeks_ReturnsTrue_ForClinical_AtOrAfterThreshold()
    {
        // Act & Assert
        Assert.True(_service.UsesWeeks("CLI", 201604));
        Assert.True(_service.UsesWeeks("CLI", 202410));
    }

    #endregion

    #region R-Course Auto-Creation Tests

    [Fact]
    public async Task CreateEffortRecordAsync_CallsRCourseService_WhenFirstNonRCourseAdded()
    {
        // Arrange - create a new instructor with no existing effort records
        var newPersonId = 200;
        _context.Persons.Add(new EffortPerson
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            FirstName = "New",
            LastName = "Instructor",
            EffortDept = "VME"
        });

        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId, // VET 410 - not an R-course
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 40
        };

        // Act - create the first non-R-course effort record
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert - verify the effort record was created
        Assert.NotNull(result);
        Assert.Equal(newPersonId, result.PersonId);
        Assert.Equal("LEC", result.EffortType);

        // Verify RCourseService was called to create R-course (first non-R-course triggers creation)
        _rCourseServiceMock.Verify(s => s.CreateRCourseEffortRecordAsync(
            newPersonId,
            TestTermCode,
            TestUserId,
            RCourseCreationContext.OnDemand,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_DoesNotCallRCourseService_WhenSecondNonRCourseAdded()
    {
        // Arrange - create an instructor with one existing non-R-course record
        var existingPersonId = 201;
        _context.Persons.Add(new EffortPerson
        {
            PersonId = existingPersonId,
            TermCode = TestTermCode,
            FirstName = "Existing",
            LastName = "Instructor",
            EffortDept = "VME"
        });

        // Add first effort record
        _context.Records.Add(new EffortRecord
        {
            PersonId = existingPersonId,
            TermCode = TestTermCode,
            CourseId = TestCourseId, // VET 410 - not an R-course
            EffortTypeId = "LEC",
            RoleId = 1,
            Hours = 40,
            Crn = "12345"
        });

        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = existingPersonId,
            TermCode = TestTermCode,
            CourseId = 2, // DVM 443 - also not an R-course
            EffortTypeId = "LAB",
            RoleId = 1,
            EffortValue = 20
        };

        // Act - add a second non-R-course effort record
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert - verify the effort record was created
        Assert.NotNull(result);
        Assert.Equal(existingPersonId, result.PersonId);

        // Verify RCourseService was NOT called (not the first non-R-course)
        _rCourseServiceMock.Verify(s => s.CreateRCourseEffortRecordAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RCourseCreationContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_DoesNotCallRCourseService_WhenAddingRCourseItself()
    {
        // Arrange - create a new instructor with no existing effort records
        var newPersonId = 202;
        _context.Persons.Add(new EffortPerson
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            FirstName = "Another",
            LastName = "Instructor",
            EffortDept = "VME"
        });

        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            CourseId = 4, // VET 290R - this IS an R-course
            EffortTypeId = "LEC",
            RoleId = 1,
            EffortValue = 10
        };

        // Act - add an R-course as the first effort record
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert - verify the effort record was created
        Assert.NotNull(result);

        // Verify RCourseService was NOT called (adding R-course doesn't trigger auto-creation)
        _rCourseServiceMock.Verify(s => s.CreateRCourseEffortRecordAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RCourseCreationContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateEffortRecordAsync_DoesNotCallRCourseService_WhenAddingGenericRCourse()
    {
        // Arrange - create a new instructor and the generic R-course
        var newPersonId = 203;
        _context.Persons.Add(new EffortPerson
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            FirstName = "Generic",
            LastName = "RCourse",
            EffortDept = "VME"
        });

        // Create the generic R-course (RESID)
        var genericRCourse = new EffortCourse
        {
            Id = 100,
            TermCode = TestTermCode,
            Crn = "RESID",
            SubjCode = "RES",
            CrseNumb = "000R",
            SeqNumb = "001",
            Units = 0,
            Enrollment = 0,
            CustDept = "UNK"
        };
        _context.Courses.Add(genericRCourse);
        await _context.SaveChangesAsync();

        var request = new CreateEffortRecordRequest
        {
            PersonId = newPersonId,
            TermCode = TestTermCode,
            CourseId = 100, // RESID - the generic R-course
            EffortTypeId = "ADM",
            RoleId = 1,
            EffortValue = 10
        };

        // Act - add the generic R-course as the first effort record
        var (result, _) = await _service.CreateEffortRecordAsync(request);

        // Assert - verify the effort record was created
        Assert.NotNull(result);

        // Verify RCourseService was NOT called (adding generic R-course doesn't trigger auto-creation)
        _rCourseServiceMock.Verify(s => s.CreateRCourseEffortRecordAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<RCourseCreationContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
