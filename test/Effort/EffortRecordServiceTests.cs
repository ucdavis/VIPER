using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Constants;
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
    private readonly Mock<IUserHelper> _userHelperMock;
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

        var testUser = new AaudUser { AaudUserId = TestUserId, MothraId = "testuser" };
        _userHelperMock.Setup(x => x.GetCurrentUser()).Returns(testUser);

        _service = new EffortRecordService(
            _context,
            _rapsContext,
            _auditServiceMock.Object,
            _userHelperMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Terms.Add(new EffortTerm { TermCode = TestTermCode, Status = "Opened" });

        _context.EffortTypes.AddRange(
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
            new EffortCourse { Id = TestCourseId, TermCode = TestTermCode, Crn = "12345", SubjCode = "VET", CrseNumb = "410", SeqNumb = "01", Enrollment = 20, Units = 4, CustDept = "VME" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "01", Enrollment = 15, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "VET", CrseNumb = "199", SeqNumb = "01", Enrollment = 5, Units = 1, CustDept = "VME" },
            new EffortCourse { Id = 4, TermCode = TestTermCode, Crn = "12348", SubjCode = "VET", CrseNumb = "290R", SeqNumb = "01", Enrollment = 8, Units = 2, CustDept = "VME" }
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEffortRecordAsync(request));
        Assert.Contains("Person 9999 not found", ex.Message);
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

        // Act
        var result = await _service.DeleteEffortRecordAsync(1);

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
        var result = await _service.DeleteEffortRecordAsync(999);

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

        // Act
        await _service.DeleteEffortRecordAsync(1);

        // Assert
        var updatedPerson = await _context.Persons.FirstAsync(p => p.PersonId == TestPersonId);
        Assert.Null(updatedPerson.EffortVerified);
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

    #endregion

    #region GetEffortTypeOptionsAsync Tests

    [Fact]
    public async Task GetEffortTypeOptionsAsync_ReturnsActiveTypesOnly()
    {
        // Act
        var result = await _service.GetEffortTypeOptionsAsync();

        // Assert: 7 seeded, 1 inactive (OLD) = 6 active
        Assert.Equal(6, result.Count);
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
        var closedTerm = new EffortTerm { TermCode = 202301, Status = "Closed" };
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
        var closedTerm = new EffortTerm { TermCode = 202301, Status = "Closed" };
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
}
