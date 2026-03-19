using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
    private readonly ICourseRelationshipService _relationshipServiceMock;
    private readonly ICourseService _courseServiceMock;
    private readonly IEffortPermissionService _permissionServiceMock;
    private readonly ILogger<CourseRelationshipsController> _loggerMock;
    private readonly CourseRelationshipsController _controller;

    private const int TestTermCode = 202410;
    private const string DvmDept = "DVM";
    private const string VmeDept = "VME";
    private const string CrossListType = "CrossList";

    public CourseRelationshipsControllerTests()
    {
        _relationshipServiceMock = Substitute.For<ICourseRelationshipService>();
        _courseServiceMock = Substitute.For<ICourseService>();
        _permissionServiceMock = Substitute.For<IEffortPermissionService>();
        _loggerMock = Substitute.For<ILogger<CourseRelationshipsController>>();

        _controller = new CourseRelationshipsController(
            _relationshipServiceMock,
            _courseServiceMock,
            _permissionServiceMock,
            _loggerMock);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.GetRelationshipsForCourseAsync(1, Arg.Any<CancellationToken>()).Returns(result);

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
        _courseServiceMock.GetCourseAsync(999, Arg.Any<CancellationToken>()).Returns((CourseDto?)null);

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
        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(course);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(false);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(course);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.GetRelationshipsForCourseAsync(1, Arg.Any<CancellationToken>()).Returns(result);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.GetRelationshipsForCourseAsync(1, Arg.Any<CancellationToken>()).Returns(result);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(false);
        _permissionServiceMock.GetAuthorizedDepartmentsAsync(Arg.Any<CancellationToken>()).Returns(new List<string> { DvmDept });
        _relationshipServiceMock.GetRelationshipsForCourseAsync(1, Arg.Any<CancellationToken>()).Returns(result);

        // Act
        var actionResult = await _controller.GetRelationships(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedResult = Assert.IsType<CourseRelationshipsResult>(okResult.Value);
        Assert.Single(returnedResult.ChildRelationships);
        Assert.Equal(DvmDept, returnedResult.ChildRelationships[0].ChildCourse?.CustDept);
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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(false);
        _permissionServiceMock.GetAuthorizedDepartmentsAsync(Arg.Any<CancellationToken>()).Returns(new List<string> { DvmDept });
        _relationshipServiceMock.GetRelationshipsForCourseAsync(1, Arg.Any<CancellationToken>()).Returns(result);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.GetAvailableChildCoursesAsync(1, Arg.Any<CancellationToken>()).Returns(availableCourses);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.HasFullAccessAsync(Arg.Any<CancellationToken>()).Returns(false);
        _permissionServiceMock.GetAuthorizedDepartmentsAsync(Arg.Any<CancellationToken>()).Returns(new List<string> { DvmDept });
        _relationshipServiceMock.GetAvailableChildCoursesAsync(1, Arg.Any<CancellationToken>()).Returns(availableCourses);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _courseServiceMock.GetCourseAsync(2, Arg.Any<CancellationToken>()).Returns(childCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.CanViewDepartmentAsync(VmeDept, Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.CreateRelationshipAsync(1, request, Arg.Any<CancellationToken>()).Returns(relationship);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(false);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _courseServiceMock.GetCourseAsync(2, Arg.Any<CancellationToken>()).Returns(childCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.CanViewDepartmentAsync(VmeDept, Arg.Any<CancellationToken>()).Returns(false);

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

        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _courseServiceMock.GetCourseAsync(2, Arg.Any<CancellationToken>()).Returns(childCourse);
        _permissionServiceMock.CanViewDepartmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.CreateRelationshipAsync(1, request, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("Child course is already a child of another course"));

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

        _relationshipServiceMock.GetRelationshipAsync(1, Arg.Any<CancellationToken>()).Returns(relationship);
        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _courseServiceMock.GetCourseAsync(2, Arg.Any<CancellationToken>()).Returns(childCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.CanViewDepartmentAsync(VmeDept, Arg.Any<CancellationToken>()).Returns(true);
        _relationshipServiceMock.DeleteRelationshipAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 1);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task DeleteRelationship_ReturnsNotFound_WhenRelationshipNotFound()
    {
        // Arrange
        _relationshipServiceMock.GetRelationshipAsync(999, Arg.Any<CancellationToken>()).Returns((CourseRelationshipDto?)null);

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

        _relationshipServiceMock.GetRelationshipAsync(1, Arg.Any<CancellationToken>()).Returns(relationship);

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

        _relationshipServiceMock.GetRelationshipAsync(1, Arg.Any<CancellationToken>()).Returns(relationship);
        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(false);

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

        _relationshipServiceMock.GetRelationshipAsync(1, Arg.Any<CancellationToken>()).Returns(relationship);
        _courseServiceMock.GetCourseAsync(1, Arg.Any<CancellationToken>()).Returns(parentCourse);
        _courseServiceMock.GetCourseAsync(2, Arg.Any<CancellationToken>()).Returns(childCourse);
        _permissionServiceMock.CanViewDepartmentAsync(DvmDept, Arg.Any<CancellationToken>()).Returns(true);
        _permissionServiceMock.CanViewDepartmentAsync(VmeDept, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var actionResult = await _controller.DeleteRelationship(1, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    #endregion
}
