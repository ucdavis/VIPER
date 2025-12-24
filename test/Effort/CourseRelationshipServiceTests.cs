using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for CourseRelationshipService operations.
/// </summary>
public sealed class CourseRelationshipServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly Mock<IEffortAuditService> _auditServiceMock;
    private readonly Mock<ILogger<CourseRelationshipService>> _loggerMock;
    private readonly CourseRelationshipService _service;

    private const int TestTermCode = 202410;
    private const int DifferentTermCode = 202420;
    private const string CrossListType = "CrossList";
    private const string SectionType = "Section";

    public CourseRelationshipServiceTests()
    {
        var options = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(options);
        _auditServiceMock = new Mock<IEffortAuditService>();
        _loggerMock = new Mock<ILogger<CourseRelationshipService>>();

        _auditServiceMock
            .Setup(s => s.AddCourseChangeAudit(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()));

        _service = new CourseRelationshipService(_context, _auditServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateRelationshipAsync Validation Tests

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenParentNotFound()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(999, request)
        );
        Assert.Contains("999", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenChildNotFound()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 999,
            RelationshipType = CrossListType
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(1, request)
        );
        Assert.Contains("999", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenTermsDoNotMatch()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = DifferentTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(1, request)
        );
        Assert.Contains("same term", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenSelfLinking()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 1,
            RelationshipType = CrossListType
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(1, request)
        );
        Assert.Contains("itself", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenChildAlreadyLinked()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 3, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(1, request)
        );
        Assert.Contains("already a child", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenDuplicateExists()
    {
        // Arrange - Course 2 is already a child of course 1
        // The "already a child" check happens before "duplicate exists" check
        // So attempting to add the same relationship will hit "already a child" first
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = SectionType
        };

        // Act & Assert - Will fail on "already a child" check since child is already linked
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(1, request)
        );
        Assert.Contains("already a child", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenParentIsAlreadyChild()
    {
        // Arrange: Create hierarchy A -> B, then try to make B a parent of C (B -> C)
        // This would create multi-level hierarchy A -> B -> C which should be prevented
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        // Course 2 is already a child of course 1
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Try to make course 2 (which is a child) a parent of course 3
        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 3,
            RelationshipType = CrossListType
        };

        // Act & Assert - Should fail because course 2 is already a child
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(2, request)
        );
        Assert.Contains("cannot be a parent", exception.Message);
        Assert.Contains("already a child", exception.Message);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ThrowsInvalidOperationException_WhenChildIsAlreadyParent()
    {
        // Arrange: Create hierarchy B -> C, then try to make A a parent of B (A -> B)
        // This would create multi-level hierarchy A -> B -> C which should be prevented
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        // Course 2 is already a parent of course 3
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 2, ChildCourseId = 3, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Try to make course 2 (which has children) a child of course 1
        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        // Act & Assert - Should fail because course 2 already has linked children
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRelationshipAsync(1, request)
        );
        Assert.Contains("cannot be a child", exception.Message);
        Assert.Contains("already has linked children", exception.Message);
    }

    #endregion

    #region CreateRelationshipAsync Success Tests

    [Fact]
    public async Task CreateRelationshipAsync_CreatesRelationship_WithValidData()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        // Act
        var result = await _service.CreateRelationshipAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ParentCourseId);
        Assert.Equal(2, result.ChildCourseId);
        Assert.Equal(CrossListType, result.RelationshipType);
        Assert.NotNull(result.ChildCourse);
        Assert.Equal("VME", result.ChildCourse.SubjCode);

        var savedRelationship = await _context.CourseRelationships.FirstOrDefaultAsync();
        Assert.NotNull(savedRelationship);
    }

    [Fact]
    public async Task CreateRelationshipAsync_CreatesAuditEntry()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        await _context.SaveChangesAsync();

        var request = new CreateCourseRelationshipRequest
        {
            ChildCourseId = 2,
            RelationshipType = SectionType
        };

        // Act
        await _service.CreateRelationshipAsync(1, request);

        // Assert
        _auditServiceMock.Verify(
            s => s.AddCourseChangeAudit(
                1,
                TestTermCode,
                "CreateCourseRelationship",
                null,
                It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRelationshipAsync_AllowsMultipleChildrenForSameParent()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        // Pre-seed the first relationship to avoid tracking issues with sequential creates
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Act - Add second child
        await _service.CreateRelationshipAsync(1, new CreateCourseRelationshipRequest { ChildCourseId = 3, RelationshipType = SectionType });

        // Assert
        var relationships = await _context.CourseRelationships.Where(r => r.ParentCourseId == 1).ToListAsync();
        Assert.Equal(2, relationships.Count);
    }

    #endregion

    #region DeleteRelationshipAsync Tests

    [Fact]
    public async Task DeleteRelationshipAsync_DeletesAndCreatesAuditEntry()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteRelationshipAsync(1);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.CourseRelationships.FindAsync(1));

        _auditServiceMock.Verify(
            s => s.AddCourseChangeAudit(
                1,
                TestTermCode,
                "DeleteCourseRelationship",
                It.IsAny<object>(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRelationshipAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _service.DeleteRelationshipAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAvailableChildCoursesAsync Tests

    [Fact]
    public async Task GetAvailableChildCoursesAsync_ExcludesParentCourse()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        await _context.SaveChangesAsync();

        // Act
        var available = await _service.GetAvailableChildCoursesAsync(1);

        // Assert
        Assert.Single(available);
        Assert.DoesNotContain(available, c => c.Id == 1);
    }

    [Fact]
    public async Task GetAvailableChildCoursesAsync_ExcludesAlreadyLinkedChildren()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" },
            new EffortCourse { Id = 4, TermCode = TestTermCode, Crn = "12348", SubjCode = "PMI", CrseNumb = "300", SeqNumb = "001", Enrollment = 12, Units = 3, CustDept = "PMI" }
        );
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 3, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Act
        var available = await _service.GetAvailableChildCoursesAsync(1);

        // Assert:
        // - Course 2 is already a child of course 3, should not be available
        // - Course 3 is already a parent, should not be available (prevents multi-level hierarchy)
        // - Only course 4 should be available
        Assert.Single(available);
        Assert.DoesNotContain(available, c => c.Id == 2);
        Assert.DoesNotContain(available, c => c.Id == 3);
        Assert.Contains(available, c => c.Id == 4);
    }

    [Fact]
    public async Task GetAvailableChildCoursesAsync_ExcludesCoursesWithChildren()
    {
        // Arrange: Parent courses cannot become children (prevents multi-level hierarchies)
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        // Course 2 has a child (course 3), so course 2 cannot become a child of course 1
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 2, ChildCourseId = 3, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Act
        var available = await _service.GetAvailableChildCoursesAsync(1);

        // Assert:
        // - Course 2 is a parent, should not be available as a child
        // - Course 3 is already a child, should not be available
        // - No courses available
        Assert.Empty(available);
    }

    [Fact]
    public async Task GetAvailableChildCoursesAsync_OnlyIncludesSameTerm()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = DifferentTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        await _context.SaveChangesAsync();

        // Act
        var available = await _service.GetAvailableChildCoursesAsync(1);

        // Assert
        Assert.Single(available);
        Assert.DoesNotContain(available, c => c.TermCode == DifferentTermCode);
    }

    [Fact]
    public async Task GetAvailableChildCoursesAsync_ReturnsEmpty_WhenParentNotFound()
    {
        // Act
        var available = await _service.GetAvailableChildCoursesAsync(999);

        // Assert
        Assert.Empty(available);
    }

    #endregion

    #region GetRelationshipsForCourseAsync Tests

    [Fact]
    public async Task GetRelationshipsForCourseAsync_ReturnsParentWhenCourseIsChild()
    {
        // Arrange - Course 2 is a child of Course 1
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        _context.CourseRelationships.Add(
            new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType }
        );
        await _context.SaveChangesAsync();

        // Act - Get relationships for the child course
        var result = await _service.GetRelationshipsForCourseAsync(2);

        // Assert - Course 2 should show its parent relationship
        Assert.NotNull(result.ParentRelationship);
        Assert.Equal(1, result.ParentRelationship.ParentCourseId);
        Assert.Empty(result.ChildRelationships);
    }

    [Fact]
    public async Task GetRelationshipsForCourseAsync_ReturnsChildrenWhenCourseIsParent()
    {
        // Arrange - Course 1 is parent of Course 2 and Course 3
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" },
            new EffortCourse { Id = 3, TermCode = TestTermCode, Crn = "12347", SubjCode = "APC", CrseNumb = "100", SeqNumb = "001", Enrollment = 15, Units = 2, CustDept = "APC" }
        );
        _context.CourseRelationships.AddRange(
            new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType },
            new CourseRelationship { Id = 2, ParentCourseId = 1, ChildCourseId = 3, RelationshipType = SectionType }
        );
        await _context.SaveChangesAsync();

        // Act - Get relationships for the parent course
        var result = await _service.GetRelationshipsForCourseAsync(1);

        // Assert - Course 1 should show its child relationships
        Assert.Null(result.ParentRelationship);
        Assert.Equal(2, result.ChildRelationships.Count);
        Assert.Contains(result.ChildRelationships, r => r.ChildCourseId == 2);
        Assert.Contains(result.ChildRelationships, r => r.ChildCourseId == 3);
    }

    [Fact]
    public async Task GetParentRelationshipAsync_ReturnsNull_WhenNoParent()
    {
        // Arrange
        _context.Courses.Add(new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetParentRelationshipAsync(1);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetRelationshipAsync Tests

    [Fact]
    public async Task GetRelationshipAsync_ReturnsRelationship_WithBothCourses()
    {
        // Arrange
        _context.Courses.AddRange(
            new EffortCourse { Id = 1, TermCode = TestTermCode, Crn = "12345", SubjCode = "DVM", CrseNumb = "443", SeqNumb = "001", Enrollment = 20, Units = 4, CustDept = "DVM" },
            new EffortCourse { Id = 2, TermCode = TestTermCode, Crn = "12346", SubjCode = "VME", CrseNumb = "200", SeqNumb = "001", Enrollment = 10, Units = 3, CustDept = "VME" }
        );
        _context.CourseRelationships.Add(new CourseRelationship { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRelationshipAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ParentCourse);
        Assert.NotNull(result.ChildCourse);
        Assert.Equal("DVM", result.ParentCourse.SubjCode);
        Assert.Equal("VME", result.ChildCourse.SubjCode);
    }

    [Fact]
    public async Task GetRelationshipAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetRelationshipAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
