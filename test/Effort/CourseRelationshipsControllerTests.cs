using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for CourseRelationshipsController API endpoints.
/// </summary>
public sealed class CourseRelationshipsControllerTests
{
    private readonly Mock<ICourseRelationshipService> _relationshipServiceMock;
    private readonly Mock<ICourseService> _courseServiceMock;
    private readonly Mock<IEffortPermissionService> _permissionServiceMock;
    private readonly Mock<ILogger<CourseRelationshipsController>> _loggerMock;
    private readonly CourseRelationshipsController _controller;

    private const int TestTermCode = 202410;
    private const string DvmDept = "DVM";
    private const string VmeDept = "VME";
    private const string CrossListType = "CrossList";

    public CourseRelationshipsControllerTests()
    {
        _relationshipServiceMock = new Mock<ICourseRelationshipService>();
        _courseServiceMock = new Mock<ICourseService>();
        _permissionServiceMock = new Mock<IEffortPermissionService>();
        _loggerMock = new Mock<ILogger<CourseRelationshipsController>>();

        _controller = new CourseRelationshipsController(
            _relationshipServiceMock.Object,
            _courseServiceMock.Object,
            _permissionServiceMock.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            }
        };
    }

    private CourseDto CreateCourse(int id, string dept) => new()
    {
        Id = id,
        TermCode = TestTermCode,
        Crn = $"1234{id}",
        SubjCode = dept,
        CrseNumb = "443",
        SeqNumb = "001",
        Enrollment = 20,
        Units = 4,
        CustDept = dept
    };

    #region GetRelationships Tests

    [Fact]
    public async Task GetRelationships_ReturnsOk_WithRelationships()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var result = new CourseRelationshipsResult
        {
            ParentRelationship = null,
            ChildRelationships = new List<CourseRelationshipDto>
            {
                new() { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType, ChildCourse = CreateCourse(2, DvmDept) }
            }
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.GetRelationshipsForCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<CourseRelationshipsResult>(okResult.Value);
        Assert.Single(returnedResult.ChildRelationships);
    }

    [Fact]
    public async Task GetRelationships_ReturnsNotFound_WhenCourseNotFound()
    {
        // Arrange
        _courseServiceMock.Setup(s => s.GetCourseAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((CourseDto?)null);

        // Act
        var actionResult = await _controller.GetRelationships(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task GetRelationships_ReturnsNotFound_WhenUserUnauthorized()
    {
        // Arrange
        var course = CreateCourse(1, DvmDept);
        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetRelationships_ReturnsOk_WithEmptyList()
    {
        // Arrange
        var course = CreateCourse(1, DvmDept);
        var result = new CourseRelationshipsResult
        {
            ParentRelationship = null,
            ChildRelationships = new List<CourseRelationshipDto>()
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.GetRelationshipsForCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<CourseRelationshipsResult>(okResult.Value);
        Assert.Empty(returnedResult.ChildRelationships);
    }

    [Fact]
    public async Task GetRelationships_ReturnsAllRelationships_WhenFullAccess()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var result = new CourseRelationshipsResult
        {
            ParentRelationship = new CourseRelationshipDto
            {
                Id = 10,
                ParentCourseId = 99,
                ChildCourseId = 1,
                RelationshipType = CrossListType,
                ParentCourse = CreateCourse(99, VmeDept)
            },
            ChildRelationships = new List<CourseRelationshipDto>
            {
                new() { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType, ChildCourse = CreateCourse(2, VmeDept) },
                new() { Id = 2, ParentCourseId = 1, ChildCourseId = 3, RelationshipType = CrossListType, ChildCourse = CreateCourse(3, DvmDept) }
            }
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.GetRelationshipsForCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<CourseRelationshipsResult>(okResult.Value);
        Assert.Equal(2, returnedResult.ChildRelationships.Count);
        Assert.NotNull(returnedResult.ParentRelationship);
    }

    [Fact]
    public async Task GetRelationships_FiltersChildRelationshipsByAuthorizedDepartments()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var result = new CourseRelationshipsResult
        {
            ParentRelationship = null,
            ChildRelationships = new List<CourseRelationshipDto>
            {
                new() { Id = 1, ParentCourseId = 1, ChildCourseId = 2, RelationshipType = CrossListType, ChildCourse = CreateCourse(2, VmeDept) },
                new() { Id = 2, ParentCourseId = 1, ChildCourseId = 3, RelationshipType = CrossListType, ChildCourse = CreateCourse(3, DvmDept) }
            }
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { DvmDept });
        _relationshipServiceMock.Setup(s => s.GetRelationshipsForCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<CourseRelationshipsResult>(okResult.Value);
        Assert.Single(returnedResult.ChildRelationships);
        Assert.Equal(DvmDept, returnedResult.ChildRelationships.First().ChildCourse?.CustDept);
    }

    [Fact]
    public async Task GetRelationships_FiltersParentRelationshipByAuthorizedDepartment()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var result = new CourseRelationshipsResult
        {
            ParentRelationship = new CourseRelationshipDto
            {
                Id = 10,
                ParentCourseId = 99,
                ChildCourseId = 1,
                RelationshipType = CrossListType,
                ParentCourse = CreateCourse(99, VmeDept) // User doesn't have access to VME
            },
            ChildRelationships = new List<CourseRelationshipDto>()
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { DvmDept });
        _relationshipServiceMock.Setup(s => s.GetRelationshipsForCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<CourseRelationshipsResult>(okResult.Value);
        Assert.Null(returnedResult.ParentRelationship);
    }

    #endregion

    #region GetAvailableChildren Tests

    [Fact]
    public async Task GetAvailableChildren_ReturnsAllCourses_WhenFullAccess()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var availableCourses = new List<CourseDto>
        {
            CreateCourse(2, VmeDept),
            CreateCourse(3, DvmDept)
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.GetAvailableChildCoursesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(availableCourses);

        // Act
        var actionResult = await _controller.GetAvailableChildren(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(okResult.Value);
        Assert.Equal(2, returnedCourses.Count());
    }

    [Fact]
    public async Task GetAvailableChildren_FiltersResultsByAuthorizedDepartments()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var availableCourses = new List<CourseDto>
        {
            CreateCourse(2, VmeDept),
            CreateCourse(3, DvmDept)
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.HasFullAccessAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _permissionServiceMock.Setup(s => s.GetAuthorizedDepartmentsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { DvmDept });
        _relationshipServiceMock.Setup(s => s.GetAvailableChildCoursesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(availableCourses);

        // Act
        var actionResult = await _controller.GetAvailableChildren(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(okResult.Value);
        Assert.Single(returnedCourses);
        Assert.Equal(DvmDept, returnedCourses.First().CustDept);
    }

    #endregion

    #region CreateRelationship Tests

    [Fact]
    public async Task CreateRelationship_ReturnsCreatedAtAction_OnSuccess()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var childCourse = CreateCourse(2, VmeDept);
        var request = new CreateCourseRelationshipRequest { ChildCourseId = 2, RelationshipType = CrossListType };
        var relationship = new CourseRelationshipDto
        {
            Id = 1,
            ParentCourseId = 1,
            ChildCourseId = 2,
            RelationshipType = CrossListType,
            ChildCourse = childCourse
        };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _courseServiceMock.Setup(s => s.GetCourseAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(childCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(VmeDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.CreateRelationshipAsync(1, request, It.IsAny<CancellationToken>())).ReturnsAsync(relationship);

        // Act
        var actionResult = await _controller.CreateRelationship(1, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(201, createdResult.StatusCode);
        var returnedRelationship = Assert.IsType<CourseRelationshipDto>(createdResult.Value);
        Assert.Equal(1, returnedRelationship.Id);
    }

    [Fact]
    public async Task CreateRelationship_ReturnsNotFound_WhenParentUnauthorized()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var request = new CreateCourseRelationshipRequest { ChildCourseId = 2, RelationshipType = CrossListType };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var actionResult = await _controller.CreateRelationship(1, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task CreateRelationship_ReturnsNotFound_WhenChildUnauthorized()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var childCourse = CreateCourse(2, VmeDept);
        var request = new CreateCourseRelationshipRequest { ChildCourseId = 2, RelationshipType = CrossListType };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _courseServiceMock.Setup(s => s.GetCourseAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(childCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(VmeDept, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var actionResult = await _controller.CreateRelationship(1, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task CreateRelationship_ReturnsBadRequest_ForInvalidOperation()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var childCourse = CreateCourse(2, VmeDept);
        var request = new CreateCourseRelationshipRequest { ChildCourseId = 2, RelationshipType = CrossListType };

        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _courseServiceMock.Setup(s => s.GetCourseAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(childCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.CreateRelationshipAsync(1, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Child course is already a child of another course"));

        // Act
        var actionResult = await _controller.CreateRelationship(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Contains("already a child", badRequestResult.Value?.ToString());
    }

    #endregion

    #region DeleteRelationship Tests

    [Fact]
    public async Task DeleteRelationship_ReturnsNoContent_OnSuccess()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var childCourse = CreateCourse(2, VmeDept);
        var relationship = new CourseRelationshipDto
        {
            Id = 1,
            ParentCourseId = 1,
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        _relationshipServiceMock.Setup(s => s.GetRelationshipAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(relationship);
        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _courseServiceMock.Setup(s => s.GetCourseAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(childCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(VmeDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _relationshipServiceMock.Setup(s => s.DeleteRelationshipAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 1);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task DeleteRelationship_ReturnsNotFound_WhenRelationshipNotFound()
    {
        // Arrange
        _relationshipServiceMock.Setup(s => s.GetRelationshipAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((CourseRelationshipDto?)null);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task DeleteRelationship_ReturnsNotFound_WhenWrongParent()
    {
        // Arrange
        var relationship = new CourseRelationshipDto
        {
            Id = 1,
            ParentCourseId = 2, // Different parent
            ChildCourseId = 3,
            RelationshipType = CrossListType
        };

        _relationshipServiceMock.Setup(s => s.GetRelationshipAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(relationship);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 1); // Trying to delete via parent 1

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Contains("not found for course", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task DeleteRelationship_ReturnsNotFound_WhenParentUnauthorized()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var relationship = new CourseRelationshipDto
        {
            Id = 1,
            ParentCourseId = 1,
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        _relationshipServiceMock.Setup(s => s.GetRelationshipAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(relationship);
        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task DeleteRelationship_ReturnsNotFound_WhenChildUnauthorized()
    {
        // Arrange
        var parentCourse = CreateCourse(1, DvmDept);
        var childCourse = CreateCourse(2, VmeDept);
        var relationship = new CourseRelationshipDto
        {
            Id = 1,
            ParentCourseId = 1,
            ChildCourseId = 2,
            RelationshipType = CrossListType
        };

        _relationshipServiceMock.Setup(s => s.GetRelationshipAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(relationship);
        _courseServiceMock.Setup(s => s.GetCourseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parentCourse);
        _courseServiceMock.Setup(s => s.GetCourseAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(childCourse);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(DvmDept, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _permissionServiceMock.Setup(s => s.CanViewDepartmentAsync(VmeDept, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    #endregion
}
