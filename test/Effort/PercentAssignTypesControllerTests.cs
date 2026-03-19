using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentAssignTypesController API endpoints.
/// </summary>
public sealed class PercentAssignTypesControllerTests
{
    private readonly IPercentAssignTypeService _typeServiceMock;
    private readonly ILogger<PercentAssignTypesController> _loggerMock;
    private readonly PercentAssignTypesController _controller;

    public PercentAssignTypesControllerTests()
    {
        _typeServiceMock = Substitute.For<IPercentAssignTypeService>();
        _loggerMock = Substitute.For<ILogger<PercentAssignTypesController>>();

        _controller = new PercentAssignTypesController(
            _typeServiceMock,
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

    #region GetPercentAssignTypes Tests

    [Fact]
    public async Task GetPercentAssignTypes_ReturnsOk_WithTypeList()
    {
        // Arrange
        var types = new List<PercentAssignTypeDto>
        {
            new() { Id = 1, Class = "Teaching", Name = "Lecture", IsActive = true, InstructorCount = 5 },
            new() { Id = 2, Class = "Admin", Name = "Chair", IsActive = true, InstructorCount = 2 }
        };
        _typeServiceMock.GetPercentAssignTypesAsync(false, Arg.Any<CancellationToken>()).Returns(types);

        // Act
        var result = await _controller.GetPercentAssignTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTypes = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        Assert.Equal(2, returnedTypes.Count());
    }

    [Fact]
    public async Task GetPercentAssignTypes_PassesActiveOnlyFilter()
    {
        // Arrange
        var types = new List<PercentAssignTypeDto>
        {
            new() { Id = 1, Class = "Teaching", Name = "Active Type", IsActive = true, InstructorCount = 3 }
        };
        _typeServiceMock.GetPercentAssignTypesAsync(true, Arg.Any<CancellationToken>()).Returns(types);

        // Act
        var result = await _controller.GetPercentAssignTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTypes = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        Assert.Single(returnedTypes);
        await _typeServiceMock.Received(1).GetPercentAssignTypesAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPercentAssignTypes_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _typeServiceMock.GetPercentAssignTypesAsync(false, Arg.Any<CancellationToken>()).Returns(new List<PercentAssignTypeDto>());
        // Act
        var result = await _controller.GetPercentAssignTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTypes = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        Assert.Empty(returnedTypes);
    }

    #endregion

    #region GetPercentAssignType Tests

    [Fact]
    public async Task GetPercentAssignType_ReturnsOk_WhenFound()
    {
        // Arrange
        var type = new PercentAssignTypeDto { Id = 1, Class = "Teaching", Name = "Lecture", IsActive = true };
        _typeServiceMock.GetPercentAssignTypeAsync(1, Arg.Any<CancellationToken>()).Returns(type);

        // Act
        var result = await _controller.GetPercentAssignType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedType = Assert.IsType<PercentAssignTypeDto>(okResult.Value);
        Assert.Equal("Lecture", returnedType.Name);
    }

    [Fact]
    public async Task GetPercentAssignType_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _typeServiceMock.GetPercentAssignTypeAsync(999, Arg.Any<CancellationToken>()).Returns((PercentAssignTypeDto?)null);

        // Act
        var result = await _controller.GetPercentAssignType(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    #endregion

    #region GetClasses Tests

    [Fact]
    public async Task GetClasses_ReturnsOk_WithClassList()
    {
        // Arrange
        var classes = new List<string> { "Admin", "Research", "Teaching" };
        _typeServiceMock.GetPercentAssignTypeClassesAsync(Arg.Any<CancellationToken>()).Returns(classes);

        // Act
        var result = await _controller.GetClasses(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedClasses = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Equal(3, returnedClasses.Count());
    }

    [Fact]
    public async Task GetClasses_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _typeServiceMock.GetPercentAssignTypeClassesAsync(Arg.Any<CancellationToken>()).Returns(new List<string>());
        // Act
        var result = await _controller.GetClasses(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedClasses = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Empty(returnedClasses);
    }

    #endregion

    #region GetInstructorsByType Tests

    [Fact]
    public async Task GetInstructorsByType_ReturnsOk_WithInstructors()
    {
        // Arrange
        var response = new InstructorsByPercentAssignTypeResponseDto
        {
            TypeId = 1,
            TypeName = "Lecture",
            TypeClass = "Teaching",
            Instructors = new List<InstructorByPercentAssignTypeDto>
            {
                new() { PersonId = 1, FirstName = "John", LastName = "Doe", FullName = "Doe, John", AcademicYear = "2024-25" },
                new() { PersonId = 2, FirstName = "Jane", LastName = "Smith", FullName = "Smith, Jane", AcademicYear = "2024-25" }
            }
        };
        _typeServiceMock.GetInstructorsByTypeAsync(1, Arg.Any<CancellationToken>()).Returns(response);

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);
        Assert.Equal("Lecture", returnedResponse.TypeName);
        Assert.Equal(2, returnedResponse.Instructors.Count);
    }

    [Fact]
    public async Task GetInstructorsByType_ReturnsNotFound_WhenTypeMissing()
    {
        // Arrange
        _typeServiceMock.GetInstructorsByTypeAsync(999, Arg.Any<CancellationToken>()).Returns((InstructorsByPercentAssignTypeResponseDto?)null);

        // Act
        var result = await _controller.GetInstructorsByType(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task GetInstructorsByType_ReturnsOk_WithEmptyInstructorList()
    {
        // Arrange
        var response = new InstructorsByPercentAssignTypeResponseDto
        {
            TypeId = 1,
            TypeName = "Lecture",
            TypeClass = "Teaching",
            Instructors = new List<InstructorByPercentAssignTypeDto>()
        };
        _typeServiceMock.GetInstructorsByTypeAsync(1, Arg.Any<CancellationToken>()).Returns(response);

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);
        Assert.Empty(returnedResponse.Instructors);
    }

    #endregion
}
