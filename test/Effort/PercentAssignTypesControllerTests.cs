using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentAssignTypesController API endpoints.
/// </summary>
public sealed class PercentAssignTypesControllerTests
{
    private readonly Mock<IPercentAssignTypeService> _typeServiceMock;
    private readonly Mock<ILogger<PercentAssignTypesController>> _loggerMock;
    private readonly PercentAssignTypesController _controller;

    public PercentAssignTypesControllerTests()
    {
        _typeServiceMock = new Mock<IPercentAssignTypeService>();
        _loggerMock = new Mock<ILogger<PercentAssignTypesController>>();

        _controller = new PercentAssignTypesController(
            _typeServiceMock.Object,
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
        _typeServiceMock.Setup(s => s.GetPercentAssignTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(types);

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
        _typeServiceMock.Setup(s => s.GetPercentAssignTypesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(types);

        // Act
        var result = await _controller.GetPercentAssignTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTypes = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        Assert.Single(returnedTypes);
        _typeServiceMock.Verify(s => s.GetPercentAssignTypesAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPercentAssignTypes_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _typeServiceMock.Setup(s => s.GetPercentAssignTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PercentAssignTypeDto>());

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
        _typeServiceMock.Setup(s => s.GetPercentAssignTypeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

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
        _typeServiceMock.Setup(s => s.GetPercentAssignTypeAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PercentAssignTypeDto?)null);

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
        _typeServiceMock.Setup(s => s.GetPercentAssignTypeClassesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classes);

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
        _typeServiceMock.Setup(s => s.GetPercentAssignTypeClassesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

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
        _typeServiceMock.Setup(s => s.GetInstructorsByTypeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

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
        _typeServiceMock.Setup(s => s.GetInstructorsByTypeAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstructorsByPercentAssignTypeResponseDto?)null);

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
        _typeServiceMock.Setup(s => s.GetInstructorsByTypeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);
        Assert.Empty(returnedResponse.Instructors);
    }

    #endregion
}
