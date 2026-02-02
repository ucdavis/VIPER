using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.Controllers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Integration tests for PercentAssignTypesController.
/// Tests the full stack: Controller -> Service -> DbContext.
/// </summary>
public class PercentAssignTypesControllerIntegrationTests : EffortIntegrationTestBase
{
    private readonly PercentAssignTypesController _controller;
    private readonly PercentAssignTypeService _service;

    public PercentAssignTypesControllerIntegrationTests()
    {
        // Create real service with real DbContext (in-memory)
        var mapper = AutoMapperHelper.CreateMapper();
        _service = new PercentAssignTypeService(EffortContext, mapper);

        var mockLogger = new Mock<ILogger<PercentAssignTypesController>>();
        _controller = new PercentAssignTypesController(_service, mockLogger.Object);
        SetupControllerContext(_controller);

        // Seed percent assignment types for tests
        SeedPercentAssignTypesAsync().GetAwaiter().GetResult();
    }

    private async Task SeedPercentAssignTypesAsync()
    {
        EffortContext.PercentAssignTypes.AddRange(
            new PercentAssignType { Id = 1, Class = "Teaching", Name = "Lecture", IsActive = true, ShowOnTemplate = true },
            new PercentAssignType { Id = 2, Class = "Teaching", Name = "Lab", IsActive = true, ShowOnTemplate = true },
            new PercentAssignType { Id = 3, Class = "Admin", Name = "Department Chair", IsActive = true, ShowOnTemplate = false },
            new PercentAssignType { Id = 4, Class = "Research", Name = "Grant Writing", IsActive = false, ShowOnTemplate = true }
        );
        await EffortContext.SaveChangesAsync();
    }

    private Percentage CreateTestPercentage(int personId, int percentAssignTypeId, string academicYear) => new()
    {
        PersonId = personId,
        PercentAssignTypeId = percentAssignTypeId,
        AcademicYear = academicYear,
        PercentageValue = 0.5,
        StartDate = DateTime.Now
    };

    #region GetPercentAssignTypes Tests

    [Fact]
    public async Task GetPercentAssignTypes_ReturnsAllTypes_OrderedByClassThenName()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetPercentAssignTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        var list = types.ToList();

        Assert.Equal(4, list.Count);
        // Verify ordered by Class, then Name
        Assert.Equal("Admin", list[0].Class);
        Assert.Equal("Department Chair", list[0].Name);
        Assert.Equal("Research", list[1].Class);
        Assert.Equal("Grant Writing", list[1].Name);
        Assert.Equal("Teaching", list[2].Class);
        Assert.Equal("Lab", list[2].Name);
        Assert.Equal("Teaching", list[3].Class);
        Assert.Equal("Lecture", list[3].Name);
    }

    [Fact]
    public async Task GetPercentAssignTypes_WithActiveOnly_ReturnsOnlyActiveTypes()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetPercentAssignTypes(activeOnly: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        var list = types.ToList();

        Assert.Equal(3, list.Count);
        Assert.DoesNotContain(list, t => t.Name == "Grant Writing");
    }

    [Fact]
    public async Task GetPercentAssignTypes_IncludesInstructorCount()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Add percentage assignments for type 1 (Lecture)
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        EffortContext.Percentages.Add(CreateTestPercentage(1001, 1, "2024-25"));
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetPercentAssignTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        var lectureType = types.First(t => t.Name == "Lecture");

        Assert.Equal(2, lectureType.InstructorCount);
    }

    [Fact]
    public async Task GetPercentAssignTypes_CountsDistinctPersonYearCombinations()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Same person, different years = counts as 2
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2023-24"));
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetPercentAssignTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        var lectureType = types.First(t => t.Name == "Lecture");

        Assert.Equal(2, lectureType.InstructorCount);
    }

    [Fact]
    public async Task GetPercentAssignTypes_CountsSamePersonYearOnce()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Same person, same year with multiple entries = counts as 1
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        var secondPercentage = CreateTestPercentage(TestUserAaudId, 1, "2024-25");
        secondPercentage.PercentageValue = 0.25;
        EffortContext.Percentages.Add(secondPercentage);
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetPercentAssignTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(okResult.Value);
        var lectureType = types.First(t => t.Name == "Lecture");

        Assert.Equal(1, lectureType.InstructorCount);
    }

    #endregion

    #region GetPercentAssignType Tests

    [Fact]
    public async Task GetPercentAssignType_ReturnsType_WhenExists()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetPercentAssignType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var type = Assert.IsType<PercentAssignTypeDto>(okResult.Value);

        Assert.Equal(1, type.Id);
        Assert.Equal("Teaching", type.Class);
        Assert.Equal("Lecture", type.Name);
        Assert.True(type.IsActive);
        Assert.True(type.ShowOnTemplate);
    }

    [Fact]
    public async Task GetPercentAssignType_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetPercentAssignType(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    #endregion

    #region GetClasses Tests

    [Fact]
    public async Task GetClasses_ReturnsDistinctClasses_Ordered()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetClasses(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var classes = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        var list = classes.ToList();

        Assert.Equal(3, list.Count);
        Assert.Equal("Admin", list[0]);
        Assert.Equal("Research", list[1]);
        Assert.Equal("Teaching", list[2]);
    }

    #endregion

    #region GetInstructorsByType Tests

    [Fact]
    public async Task GetInstructorsByType_ReturnsNotFound_WhenTypeNotExists()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetInstructorsByType(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("999", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task GetInstructorsByType_ReturnsTypeInfo_WithEmptyInstructors()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);

        Assert.Equal(1, response.TypeId);
        Assert.Equal("Lecture", response.TypeName);
        Assert.Equal("Teaching", response.TypeClass);
        Assert.Empty(response.Instructors);
    }

    [Fact]
    public async Task GetInstructorsByType_ReturnsInstructors_WithCorrectInfo()
    {
        // Arrange
        SetupUserWithFullAccess();

        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);

        Assert.Single(response.Instructors);
        var instructor = response.Instructors[0];
        Assert.Equal(TestUserAaudId, instructor.PersonId);
        Assert.Equal("Test", instructor.FirstName);
        Assert.Equal("User", instructor.LastName);
        Assert.Equal("User, Test", instructor.FullName);
        Assert.Equal("2024-25", instructor.AcademicYear);
    }

    [Fact]
    public async Task GetInstructorsByType_ReturnsMultipleYears_ForSamePerson()
    {
        // Arrange
        SetupUserWithFullAccess();

        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2023-24"));
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);

        Assert.Equal(2, response.Instructors.Count);
        Assert.Contains(response.Instructors, i => i.AcademicYear == "2023-24");
        Assert.Contains(response.Instructors, i => i.AcademicYear == "2024-25");
    }

    [Fact]
    public async Task GetInstructorsByType_OrdersByAcademicYear_ThenLastName_ThenFirstName()
    {
        // Arrange
        SetupUserWithFullAccess();

        // TestUser (User, Test), Person 1001 (Johnson, Alice), Person 1002 (Smith, Bob)
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        EffortContext.Percentages.Add(CreateTestPercentage(1001, 1, "2023-24"));
        EffortContext.Percentages.Add(CreateTestPercentage(1002, 1, "2024-25"));
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);

        Assert.Equal(3, response.Instructors.Count);
        // First: 2023-24, Johnson
        Assert.Equal("2023-24", response.Instructors[0].AcademicYear);
        Assert.Equal("Johnson", response.Instructors[0].LastName);
        // Second: 2024-25, Smith (S comes before U)
        Assert.Equal("2024-25", response.Instructors[1].AcademicYear);
        Assert.Equal("Smith", response.Instructors[1].LastName);
        // Third: 2024-25, User
        Assert.Equal("2024-25", response.Instructors[2].AcademicYear);
        Assert.Equal("User", response.Instructors[2].LastName);
    }

    [Fact]
    public async Task GetInstructorsByType_ReturnsDistinctPersonYearCombinations()
    {
        // Arrange - same person/year with multiple percentage entries should appear once
        SetupUserWithFullAccess();

        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        var secondPercentage = CreateTestPercentage(TestUserAaudId, 1, "2024-25");
        secondPercentage.PercentageValue = 0.25;
        EffortContext.Percentages.Add(secondPercentage);
        await EffortContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetInstructorsByType(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(okResult.Value);

        Assert.Single(response.Instructors);
    }

    #endregion

    #region Full Read Workflow Test

    [Fact]
    public async Task FullReadWorkflow_SuccessfullyReadsAllData()
    {
        // Arrange
        SetupUserWithFullAccess();

        // Add some percentage data
        EffortContext.Percentages.Add(CreateTestPercentage(TestUserAaudId, 1, "2024-25"));
        EffortContext.Percentages.Add(CreateTestPercentage(1001, 1, "2024-25"));
        EffortContext.Percentages.Add(CreateTestPercentage(1002, 2, "2024-25"));
        await EffortContext.SaveChangesAsync();

        // Get all types
        var typesResult = await _controller.GetPercentAssignTypes(activeOnly: true);
        var typesOk = Assert.IsType<OkObjectResult>(typesResult.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<PercentAssignTypeDto>>(typesOk.Value).ToList();
        Assert.Equal(3, types.Count);

        // Get classes
        var classesResult = await _controller.GetClasses(CancellationToken.None);
        var classesOk = Assert.IsType<OkObjectResult>(classesResult.Result);
        var classes = Assert.IsAssignableFrom<IEnumerable<string>>(classesOk.Value).ToList();
        Assert.Equal(3, classes.Count);

        // Get single type
        var typeResult = await _controller.GetPercentAssignType(1, CancellationToken.None);
        var typeOk = Assert.IsType<OkObjectResult>(typeResult.Result);
        var type = Assert.IsType<PercentAssignTypeDto>(typeOk.Value);
        Assert.Equal("Lecture", type.Name);

        // Get instructors for type
        var instructorsResult = await _controller.GetInstructorsByType(1, CancellationToken.None);
        var instructorsOk = Assert.IsType<OkObjectResult>(instructorsResult.Result);
        var instructorsResponse = Assert.IsType<InstructorsByPercentAssignTypeResponseDto>(instructorsOk.Value);
        Assert.Equal(2, instructorsResponse.Instructors.Count);
    }

    #endregion
}
