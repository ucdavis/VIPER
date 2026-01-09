using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Integration tests for SessionTypesController.
/// Tests the full stack: Controller -> Service -> DbContext.
/// </summary>
public class SessionTypesControllerIntegrationTests : EffortIntegrationTestBase
{
    private readonly SessionTypesController _controller;
    private readonly SessionTypeService _service;

    public SessionTypesControllerIntegrationTests()
    {
        // Create real service with real DbContext (in-memory)
        var mockAuditService = new Mock<IEffortAuditService>();
        var mapper = AutoMapperHelper.CreateMapper();
        _service = new SessionTypeService(EffortContext, mockAuditService.Object, mapper);

        var mockLogger = new Mock<ILogger<SessionTypesController>>();
        _controller = new SessionTypesController(_service, mockLogger.Object);
        SetupControllerContext(_controller);

        // Seed session types for tests
        SeedSessionTypes();
    }

    private void SeedSessionTypes()
    {
        EffortContext.SessionTypes.AddRange(
            new SessionType { Id = "LEC", Description = "Lecture", UsesWeeks = false, IsActive = true },
            new SessionType { Id = "CLI", Description = "Clinical", UsesWeeks = true, IsActive = true },
            new SessionType { Id = "LAB", Description = "Laboratory", UsesWeeks = false, IsActive = true },
            new SessionType { Id = "OLD", Description = "Old Inactive", UsesWeeks = false, IsActive = false }
        );
        EffortContext.SaveChanges();
    }

    private EffortRecord CreateTestEffortRecord(int id, string sessionType) => new()
    {
        Id = id,
        CourseId = 1,
        PersonId = TestUserAaudId,
        TermCode = TestTermCode,
        SessionType = sessionType,
        Role = 1,
        Hours = 10,
        Crn = "12345"
    };

    #region GetSessionTypes Tests

    [Fact]
    public async Task GetSessionTypes_ReturnsAllSessionTypes_OrderedByDescription()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.GetSessionTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionTypes = Assert.IsAssignableFrom<IEnumerable<SessionTypeDto>>(okResult.Value);
        var list = sessionTypes.ToList();

        Assert.Equal(4, list.Count);
        // Verify ordered by Description: Clinical, Laboratory, Lecture, Old Inactive
        Assert.Equal("CLI", list[0].Id);
        Assert.Equal("LAB", list[1].Id);
        Assert.Equal("LEC", list[2].Id);
        Assert.Equal("OLD", list[3].Id);
    }

    [Fact]
    public async Task GetSessionTypes_WithActiveOnly_ReturnsOnlyActiveSessionTypes()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.GetSessionTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionTypes = Assert.IsAssignableFrom<IEnumerable<SessionTypeDto>>(okResult.Value);
        var list = sessionTypes.ToList();

        Assert.Equal(3, list.Count);
        Assert.DoesNotContain(list, s => s.Id == "OLD");
    }

    [Fact]
    public async Task GetSessionTypes_IncludesUsageCount()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Add an effort record referencing LEC
        EffortContext.Records.Add(CreateTestEffortRecord(1, "LEC"));
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.GetSessionTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionTypes = Assert.IsAssignableFrom<IEnumerable<SessionTypeDto>>(okResult.Value);
        var lecSessionType = sessionTypes.First(s => s.Id == "LEC");

        Assert.Equal(1, lecSessionType.UsageCount);
        Assert.False(lecSessionType.CanDelete);
    }

    #endregion

    #region GetSessionType Tests

    [Fact]
    public async Task GetSessionType_ReturnsSessionType_WhenExists()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.GetSessionType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(okResult.Value);

        Assert.Equal("LEC", sessionType.Id);
        Assert.Equal("Lecture", sessionType.Description);
        Assert.False(sessionType.UsesWeeks);
        Assert.True(sessionType.IsActive);
    }

    [Fact]
    public async Task GetSessionType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.GetSessionType("NOTFOUND", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSessionType_IsCaseInsensitive()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.GetSessionType("lec", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(okResult.Value);
        Assert.Equal("LEC", sessionType.Id);
    }

    #endregion

    #region CreateSessionType Tests

    [Fact]
    public async Task CreateSessionType_CreatesAndReturnsSessionType()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new CreateSessionTypeRequest
        {
            Id = "TST",
            Description = "Test Session",
            UsesWeeks = false,
            FacultyCanEnter = true,
            AllowedOnDvm = true,
            AllowedOn199299 = false,
            AllowedOnRCourses = true
        };

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(createdResult.Value);

        Assert.Equal("TST", sessionType.Id);
        Assert.Equal("Test Session", sessionType.Description);
        Assert.False(sessionType.UsesWeeks);
        Assert.True(sessionType.IsActive);
        Assert.True(sessionType.FacultyCanEnter);
        Assert.True(sessionType.AllowedOnDvm);
        Assert.False(sessionType.AllowedOn199299);
        Assert.True(sessionType.AllowedOnRCourses);
        Assert.Equal(0, sessionType.UsageCount);
        Assert.True(sessionType.CanDelete);

        // Verify persisted in database
        var fromDb = EffortContext.SessionTypes.Find("TST");
        Assert.NotNull(fromDb);
        Assert.Equal("Test Session", fromDb.Description);
    }

    [Fact]
    public async Task CreateSessionType_NormalizesIdToUppercase()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new CreateSessionTypeRequest
        {
            Id = "tst",
            Description = "Test Session"
        };

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(createdResult.Value);
        Assert.Equal("TST", sessionType.Id);
    }

    [Fact]
    public async Task CreateSessionType_TrimsWhitespace()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new CreateSessionTypeRequest
        {
            Id = " tst ",
            Description = "  Test Session  "
        };

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(createdResult.Value);
        Assert.Equal("TST", sessionType.Id);
        Assert.Equal("Test Session", sessionType.Description);
    }

    [Fact]
    public async Task CreateSessionType_ReturnsConflict_WhenDuplicateId()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new CreateSessionTypeRequest
        {
            Id = "LEC",
            Description = "Duplicate"
        };

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateSessionType_ReturnsConflict_WhenDuplicateIdDifferentCase()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new CreateSessionTypeRequest
        {
            Id = "lec",
            Description = "Duplicate"
        };

        // Act
        var result = await _controller.CreateSessionType(request, CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    #endregion

    #region UpdateSessionType Tests

    [Fact]
    public async Task UpdateSessionType_UpdatesAndReturnsSessionType()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new UpdateSessionTypeRequest
        {
            Description = "Updated Lecture",
            UsesWeeks = true,
            IsActive = false,
            FacultyCanEnter = false,
            AllowedOnDvm = false,
            AllowedOn199299 = true,
            AllowedOnRCourses = false
        };

        // Act
        var result = await _controller.UpdateSessionType("LEC", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(okResult.Value);

        Assert.Equal("LEC", sessionType.Id);
        Assert.Equal("Updated Lecture", sessionType.Description);
        Assert.True(sessionType.UsesWeeks);
        Assert.False(sessionType.IsActive);
        Assert.False(sessionType.FacultyCanEnter);
        Assert.False(sessionType.AllowedOnDvm);
        Assert.True(sessionType.AllowedOn199299);
        Assert.False(sessionType.AllowedOnRCourses);

        // Verify persisted in database
        var fromDb = EffortContext.SessionTypes.Find("LEC");
        Assert.NotNull(fromDb);
        Assert.Equal("Updated Lecture", fromDb.Description);
        Assert.True(fromDb.UsesWeeks);
    }

    [Fact]
    public async Task UpdateSessionType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new UpdateSessionTypeRequest
        {
            Description = "Test"
        };

        // Act
        var result = await _controller.UpdateSessionType("NOTFOUND", request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateSessionType_IsCaseInsensitive()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();
        var request = new UpdateSessionTypeRequest
        {
            Description = "Updated"
        };

        // Act
        var result = await _controller.UpdateSessionType("lec", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionType = Assert.IsType<SessionTypeDto>(okResult.Value);
        Assert.Equal("Updated", sessionType.Description);
    }

    #endregion

    #region DeleteSessionType Tests

    [Fact]
    public async Task DeleteSessionType_DeletesSessionType_WhenNoUsage()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Add a deletable session type
        EffortContext.SessionTypes.Add(new SessionType
        {
            Id = "DEL",
            Description = "To Delete",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteSessionType("DEL", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify removed from database
        var fromDb = EffortContext.SessionTypes.Find("DEL");
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task DeleteSessionType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.DeleteSessionType("NOTFOUND", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSessionType_ReturnsConflict_WhenHasUsage()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Add an effort record referencing LEC
        EffortContext.Records.Add(CreateTestEffortRecord(100, "LEC"));
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteSessionType("LEC", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("related effort record", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task DeleteSessionType_IsCaseInsensitive()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        EffortContext.SessionTypes.Add(new SessionType
        {
            Id = "DEL",
            Description = "To Delete",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteSessionType("del", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region CanDeleteSessionType Tests

    [Fact]
    public async Task CanDeleteSessionType_ReturnsTrue_WhenNoUsage()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.CanDeleteSessionType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.True(response.CanDelete);
        Assert.Equal(0, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteSessionType_ReturnsFalse_WhenHasUsage()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Add effort records
        EffortContext.Records.Add(CreateTestEffortRecord(200, "LEC"));
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.CanDeleteSessionType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.False(response.CanDelete);
        Assert.Equal(1, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteSessionType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.CanDeleteSessionType("NOTFOUND", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("NOTFOUND", notFoundResult.Value?.ToString());
    }

    #endregion

    #region Empty/Whitespace ID Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSessionType_ReturnsNotFound_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Act
        var result = await _controller.GetSessionType(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Full CRUD Workflow Test

    [Fact]
    public async Task FullCrudWorkflow_SuccessfullyPerformsAllOperations()
    {
        // Arrange
        SetupUserWithManageSessionTypesPermission();

        // Create
        var createRequest = new CreateSessionTypeRequest
        {
            Id = "WRK",
            Description = "Workshop",
            UsesWeeks = false,
            FacultyCanEnter = true
        };
        var createResult = await _controller.CreateSessionType(createRequest, CancellationToken.None);
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var created = Assert.IsType<SessionTypeDto>(createdAtResult.Value);
        Assert.Equal("WRK", created.Id);

        // Read
        var getResult = await _controller.GetSessionType("WRK", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
        var retrieved = Assert.IsType<SessionTypeDto>(okResult.Value);
        Assert.Equal("Workshop", retrieved.Description);

        // Update
        var updateRequest = new UpdateSessionTypeRequest
        {
            Description = "Updated Workshop",
            UsesWeeks = true,
            IsActive = true,
            FacultyCanEnter = false
        };
        var updateResult = await _controller.UpdateSessionType("WRK", updateRequest, CancellationToken.None);
        var updateOkResult = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updated = Assert.IsType<SessionTypeDto>(updateOkResult.Value);
        Assert.Equal("Updated Workshop", updated.Description);
        Assert.True(updated.UsesWeeks);
        Assert.False(updated.FacultyCanEnter);

        // Can Delete (should be true - no usage)
        var canDeleteResult = await _controller.CanDeleteSessionType("WRK", CancellationToken.None);
        var canDeleteOk = Assert.IsType<OkObjectResult>(canDeleteResult.Result);
        var canDeleteResponse = Assert.IsType<CanDeleteResponse>(canDeleteOk.Value);
        Assert.True(canDeleteResponse.CanDelete);

        // Delete
        var deleteResult = await _controller.DeleteSessionType("WRK", CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify deleted
        var afterDeleteResult = await _controller.GetSessionType("WRK", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(afterDeleteResult.Result);
    }

    #endregion
}
