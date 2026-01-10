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
/// Integration tests for EffortTypesController.
/// Tests the full stack: Controller -> Service -> DbContext.
/// </summary>
public class EffortTypesControllerIntegrationTests : EffortIntegrationTestBase
{
    private readonly EffortTypesController _controller;
    private readonly EffortTypeService _service;

    public EffortTypesControllerIntegrationTests()
    {
        // Create real service with real DbContext (in-memory)
        var mockAuditService = new Mock<IEffortAuditService>();
        var mapper = AutoMapperHelper.CreateMapper();
        _service = new EffortTypeService(EffortContext, mockAuditService.Object, mapper);

        var mockLogger = new Mock<ILogger<EffortTypesController>>();
        _controller = new EffortTypesController(_service, mockLogger.Object);
        SetupControllerContext(_controller);

        // Seed effort types for tests
        SeedEffortTypes();
    }

    private void SeedEffortTypes()
    {
        EffortContext.EffortTypes.AddRange(
            new EffortType { Id = "LEC", Description = "Lecture", UsesWeeks = false, IsActive = true },
            new EffortType { Id = "CLI", Description = "Clinical", UsesWeeks = true, IsActive = true },
            new EffortType { Id = "LAB", Description = "Laboratory", UsesWeeks = false, IsActive = true },
            new EffortType { Id = "OLD", Description = "Old Inactive", UsesWeeks = false, IsActive = false }
        );
        EffortContext.SaveChanges();
    }

    private EffortRecord CreateTestEffortRecord(int id, string effortType) => new()
    {
        Id = id,
        CourseId = 1,
        PersonId = TestUserAaudId,
        TermCode = TestTermCode,
        EffortTypeId = effortType,
        RoleId = 1,
        Hours = 10,
        Crn = "12345"
    };

    #region GetEffortTypes Tests

    [Fact]
    public async Task GetEffortTypes_ReturnsAllEffortTypes_OrderedByDescription()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.GetEffortTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortTypes = Assert.IsAssignableFrom<IEnumerable<EffortTypeDto>>(okResult.Value);
        var list = effortTypes.ToList();

        Assert.Equal(4, list.Count);
        // Verify ordered by Description: Clinical, Laboratory, Lecture, Old Inactive
        Assert.Equal("CLI", list[0].Id);
        Assert.Equal("LAB", list[1].Id);
        Assert.Equal("LEC", list[2].Id);
        Assert.Equal("OLD", list[3].Id);
    }

    [Fact]
    public async Task GetEffortTypes_WithActiveOnly_ReturnsOnlyActiveEffortTypes()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.GetEffortTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortTypes = Assert.IsAssignableFrom<IEnumerable<EffortTypeDto>>(okResult.Value);
        var list = effortTypes.ToList();

        Assert.Equal(3, list.Count);
        Assert.DoesNotContain(list, s => s.Id == "OLD");
    }

    [Fact]
    public async Task GetEffortTypes_IncludesUsageCount()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Add an effort record referencing LEC
        EffortContext.Records.Add(CreateTestEffortRecord(1, "LEC"));
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.GetEffortTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortTypes = Assert.IsAssignableFrom<IEnumerable<EffortTypeDto>>(okResult.Value);
        var lecEffortType = effortTypes.First(s => s.Id == "LEC");

        Assert.Equal(1, lecEffortType.UsageCount);
        Assert.False(lecEffortType.CanDelete);
    }

    #endregion

    #region GetEffortType Tests

    [Fact]
    public async Task GetEffortType_ReturnsEffortType_WhenExists()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.GetEffortType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(okResult.Value);

        Assert.Equal("LEC", effortType.Id);
        Assert.Equal("Lecture", effortType.Description);
        Assert.False(effortType.UsesWeeks);
        Assert.True(effortType.IsActive);
    }

    [Fact]
    public async Task GetEffortType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.GetEffortType("NOTFOUND", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEffortType_IsCaseInsensitive()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.GetEffortType("lec", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("LEC", effortType.Id);
    }

    #endregion

    #region CreateEffortType Tests

    [Fact]
    public async Task CreateEffortType_CreatesAndReturnsEffortType()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new CreateEffortTypeRequest
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
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(createdResult.Value);

        Assert.Equal("TST", effortType.Id);
        Assert.Equal("Test Session", effortType.Description);
        Assert.False(effortType.UsesWeeks);
        Assert.True(effortType.IsActive);
        Assert.True(effortType.FacultyCanEnter);
        Assert.True(effortType.AllowedOnDvm);
        Assert.False(effortType.AllowedOn199299);
        Assert.True(effortType.AllowedOnRCourses);
        Assert.Equal(0, effortType.UsageCount);
        Assert.True(effortType.CanDelete);

        // Verify persisted in database
        var fromDb = EffortContext.EffortTypes.Find("TST");
        Assert.NotNull(fromDb);
        Assert.Equal("Test Session", fromDb.Description);
    }

    [Fact]
    public async Task CreateEffortType_NormalizesIdToUppercase()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new CreateEffortTypeRequest
        {
            Id = "tst",
            Description = "Test Session"
        };

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(createdResult.Value);
        Assert.Equal("TST", effortType.Id);
    }

    [Fact]
    public async Task CreateEffortType_TrimsWhitespace()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new CreateEffortTypeRequest
        {
            Id = " tst ",
            Description = "  Test Session  "
        };

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(createdResult.Value);
        Assert.Equal("TST", effortType.Id);
        Assert.Equal("Test Session", effortType.Description);
    }

    [Fact]
    public async Task CreateEffortType_ReturnsConflict_WhenDuplicateId()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new CreateEffortTypeRequest
        {
            Id = "LEC",
            Description = "Duplicate"
        };

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already exists", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateEffortType_ReturnsConflict_WhenDuplicateIdDifferentCase()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new CreateEffortTypeRequest
        {
            Id = "lec",
            Description = "Duplicate"
        };

        // Act
        var result = await _controller.CreateEffortType(request, CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    #endregion

    #region UpdateEffortType Tests

    [Fact]
    public async Task UpdateEffortType_UpdatesAndReturnsEffortType()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new UpdateEffortTypeRequest
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
        var result = await _controller.UpdateEffortType("LEC", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(okResult.Value);

        Assert.Equal("LEC", effortType.Id);
        Assert.Equal("Updated Lecture", effortType.Description);
        Assert.True(effortType.UsesWeeks);
        Assert.False(effortType.IsActive);
        Assert.False(effortType.FacultyCanEnter);
        Assert.False(effortType.AllowedOnDvm);
        Assert.True(effortType.AllowedOn199299);
        Assert.False(effortType.AllowedOnRCourses);

        // Verify persisted in database
        var fromDb = EffortContext.EffortTypes.Find("LEC");
        Assert.NotNull(fromDb);
        Assert.Equal("Updated Lecture", fromDb.Description);
        Assert.True(fromDb.UsesWeeks);
    }

    [Fact]
    public async Task UpdateEffortType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new UpdateEffortTypeRequest
        {
            Description = "Test"
        };

        // Act
        var result = await _controller.UpdateEffortType("NOTFOUND", request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateEffortType_IsCaseInsensitive()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();
        var request = new UpdateEffortTypeRequest
        {
            Description = "Updated"
        };

        // Act
        var result = await _controller.UpdateEffortType("lec", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("Updated", effortType.Description);
    }

    #endregion

    #region DeleteEffortType Tests

    [Fact]
    public async Task DeleteEffortType_DeletesEffortType_WhenNoUsage()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Add a deletable effort type
        EffortContext.EffortTypes.Add(new EffortType
        {
            Id = "DEL",
            Description = "To Delete",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteEffortType("DEL", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify removed from database
        var fromDb = EffortContext.EffortTypes.Find("DEL");
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task DeleteEffortType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.DeleteEffortType("NOTFOUND", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteEffortType_ReturnsConflict_WhenHasUsage()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Add an effort record referencing LEC
        EffortContext.Records.Add(CreateTestEffortRecord(100, "LEC"));
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteEffortType("LEC", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("related effort record", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task DeleteEffortType_IsCaseInsensitive()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        EffortContext.EffortTypes.Add(new EffortType
        {
            Id = "DEL",
            Description = "To Delete",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteEffortType("del", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region CanDeleteEffortType Tests

    [Fact]
    public async Task CanDeleteEffortType_ReturnsTrue_WhenNoUsage()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.CanDeleteEffortType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.True(response.CanDelete);
        Assert.Equal(0, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteEffortType_ReturnsFalse_WhenHasUsage()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Add effort records
        EffortContext.Records.Add(CreateTestEffortRecord(200, "LEC"));
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.CanDeleteEffortType("LEC", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);

        Assert.False(response.CanDelete);
        Assert.Equal(1, response.UsageCount);
    }

    [Fact]
    public async Task CanDeleteEffortType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.CanDeleteEffortType("NOTFOUND", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("NOTFOUND", notFoundResult.Value?.ToString());
    }

    #endregion

    #region Empty/Whitespace ID Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEffortType_ReturnsBadRequest_WhenIdIsEmptyOrWhitespace(string id)
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Act
        var result = await _controller.GetEffortType(id, CancellationToken.None);

        // Assert - BadRequest for invalid input, not NotFound
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Special Character ID Tests

    [Fact]
    public async Task GetEffortType_HandlesIdWithSlash()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Add an effort type with "/" in the ID (like the seeded "D/L" and "L/D")
        EffortContext.EffortTypes.Add(new EffortType
        {
            Id = "D/L",
            Description = "Distance Learning",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act - The controller now uses query parameter [FromQuery] to handle "/" in IDs
        var result = await _controller.GetEffortType("D/L", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("D/L", effortType.Id);
        Assert.Equal("Distance Learning", effortType.Description);
    }

    [Fact]
    public async Task UpdateEffortType_HandlesIdWithSlash()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        EffortContext.EffortTypes.Add(new EffortType
        {
            Id = "L/D",
            Description = "Lab Discussion",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        var request = new UpdateEffortTypeRequest
        {
            Description = "Lab/Discussion Updated"
        };

        // Act
        var result = await _controller.UpdateEffortType("L/D", request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var effortType = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("L/D", effortType.Id);
        Assert.Equal("Lab/Discussion Updated", effortType.Description);
    }

    [Fact]
    public async Task DeleteEffortType_HandlesIdWithSlash()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        EffortContext.EffortTypes.Add(new EffortType
        {
            Id = "T/D",
            Description = "Team Discussion",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.DeleteEffortType("T/D", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify deleted
        var fromDb = EffortContext.EffortTypes.Find("T/D");
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task CanDeleteEffortType_HandlesIdWithSlash()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        EffortContext.EffortTypes.Add(new EffortType
        {
            Id = "T-D",
            Description = "Team-Discussion",
            UsesWeeks = false,
            IsActive = true
        });
        EffortContext.SaveChanges();

        // Act
        var result = await _controller.CanDeleteEffortType("T-D", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CanDeleteResponse>(okResult.Value);
        Assert.True(response.CanDelete);
    }

    #endregion

    #region Full CRUD Workflow Test

    [Fact]
    public async Task FullCrudWorkflow_SuccessfullyPerformsAllOperations()
    {
        // Arrange
        SetupUserWithManageEffortTypesPermission();

        // Create
        var createRequest = new CreateEffortTypeRequest
        {
            Id = "WRK",
            Description = "Workshop",
            UsesWeeks = false,
            FacultyCanEnter = true
        };
        var createResult = await _controller.CreateEffortType(createRequest, CancellationToken.None);
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var created = Assert.IsType<EffortTypeDto>(createdAtResult.Value);
        Assert.Equal("WRK", created.Id);

        // Read
        var getResult = await _controller.GetEffortType("WRK", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
        var retrieved = Assert.IsType<EffortTypeDto>(okResult.Value);
        Assert.Equal("Workshop", retrieved.Description);

        // Update
        var updateRequest = new UpdateEffortTypeRequest
        {
            Description = "Updated Workshop",
            UsesWeeks = true,
            IsActive = true,
            FacultyCanEnter = false
        };
        var updateResult = await _controller.UpdateEffortType("WRK", updateRequest, CancellationToken.None);
        var updateOkResult = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updated = Assert.IsType<EffortTypeDto>(updateOkResult.Value);
        Assert.Equal("Updated Workshop", updated.Description);
        Assert.True(updated.UsesWeeks);
        Assert.False(updated.FacultyCanEnter);

        // Can Delete (should be true - no usage)
        var canDeleteResult = await _controller.CanDeleteEffortType("WRK", CancellationToken.None);
        var canDeleteOk = Assert.IsType<OkObjectResult>(canDeleteResult.Result);
        var canDeleteResponse = Assert.IsType<CanDeleteResponse>(canDeleteOk.Value);
        Assert.True(canDeleteResponse.CanDelete);

        // Delete
        var deleteResult = await _controller.DeleteEffortType("WRK", CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify deleted
        var afterDeleteResult = await _controller.GetEffortType("WRK", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(afterDeleteResult.Result);
    }

    #endregion
}
