using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for PercentAssignTypeService read operations.
/// </summary>
public sealed class PercentAssignTypeServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly IMapper _mapper;
    private readonly PercentAssignTypeService _service;

    public PercentAssignTypeServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfileEffort>();
        });
        _mapper = mapperConfig.CreateMapper();

        _service = new PercentAssignTypeService(_context, _mapper);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<PercentAssignType> CreatePercentAssignTypeAsync(
        string className,
        string name,
        bool isActive = true,
        bool showOnTemplate = true)
    {
        var type = new PercentAssignType
        {
            Class = className,
            Name = name,
            IsActive = isActive,
            ShowOnTemplate = showOnTemplate
        };
        _context.PercentAssignTypes.Add(type);
        await _context.SaveChangesAsync();
        return type;
    }

    private async Task<EffortPerson> CreatePersonAsync(int personId, int termCode, string firstName, string lastName)
    {
        var person = new EffortPerson
        {
            PersonId = personId,
            TermCode = termCode,
            FirstName = firstName,
            LastName = lastName,
            EffortDept = "TST"
        };
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    private async Task<Percentage> CreatePercentageAsync(
        int personId,
        int percentAssignTypeId,
        string academicYear,
        double percentageValue = 0.5)
    {
        var percentage = new Percentage
        {
            PersonId = personId,
            PercentAssignTypeId = percentAssignTypeId,
            AcademicYear = academicYear,
            PercentageValue = percentageValue,
            StartDate = DateTime.Now
        };
        _context.Percentages.Add(percentage);
        await _context.SaveChangesAsync();
        return percentage;
    }

    #endregion

    #region GetPercentAssignTypesAsync Tests

    [Fact]
    public async Task GetPercentAssignTypesAsync_ReturnsAllTypes_OrderedByClassThenName()
    {
        // Arrange
        await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePercentAssignTypeAsync("Admin", "Department Chair");
        await CreatePercentAssignTypeAsync("Teaching", "Lab");
        await CreatePercentAssignTypeAsync("Admin", "Associate Dean");

        // Act
        var types = await _service.GetPercentAssignTypesAsync();

        // Assert
        Assert.Equal(4, types.Count);
        Assert.Equal("Admin", types[0].Class);
        Assert.Equal("Associate Dean", types[0].Name);
        Assert.Equal("Admin", types[1].Class);
        Assert.Equal("Department Chair", types[1].Name);
        Assert.Equal("Teaching", types[2].Class);
        Assert.Equal("Lab", types[2].Name);
        Assert.Equal("Teaching", types[3].Class);
        Assert.Equal("Lecture", types[3].Name);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_FiltersActiveOnly_WhenActiveOnlyIsTrue()
    {
        // Arrange
        await CreatePercentAssignTypeAsync("Teaching", "Active Type", isActive: true);
        await CreatePercentAssignTypeAsync("Teaching", "Inactive Type", isActive: false);

        // Act
        var types = await _service.GetPercentAssignTypesAsync(activeOnly: true);

        // Assert
        Assert.Single(types);
        Assert.Equal("Active Type", types[0].Name);
        Assert.True(types[0].IsActive);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_ReturnsAllIncludingInactive_WhenActiveOnlyIsFalse()
    {
        // Arrange
        await CreatePercentAssignTypeAsync("Teaching", "Active Type", isActive: true);
        await CreatePercentAssignTypeAsync("Teaching", "Inactive Type", isActive: false);

        // Act
        var types = await _service.GetPercentAssignTypesAsync(activeOnly: false);

        // Assert
        Assert.Equal(2, types.Count);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_ReturnsEmptyList_WhenNoTypesExist()
    {
        // Act
        var types = await _service.GetPercentAssignTypesAsync();

        // Assert
        Assert.Empty(types);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_IncludesInstructorCounts()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePersonAsync(2, 202410, "Jane", "Smith");
        await CreatePercentageAsync(1, type.Id, "2024-2025");
        await CreatePercentageAsync(2, type.Id, "2024-2025");

        // Act
        var types = await _service.GetPercentAssignTypesAsync();

        // Assert
        Assert.Single(types);
        Assert.Equal(2, types[0].InstructorCount);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_CountsDistinctPersonYearCombinations()
    {
        // Arrange - same person in different years counts as multiple
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePercentageAsync(1, type.Id, "2023-2024");
        await CreatePercentageAsync(1, type.Id, "2024-2025");

        // Act
        var types = await _service.GetPercentAssignTypesAsync();

        // Assert
        Assert.Single(types);
        Assert.Equal(2, types[0].InstructorCount);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_CountsSamePersonYearOnce()
    {
        // Arrange - same person/year should only count once even with multiple percentages
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePercentageAsync(1, type.Id, "2024-2025", 0.3);
        await CreatePercentageAsync(1, type.Id, "2024-2025", 0.2);

        // Act
        var types = await _service.GetPercentAssignTypesAsync();

        // Assert
        Assert.Single(types);
        Assert.Equal(1, types[0].InstructorCount);
    }

    [Fact]
    public async Task GetPercentAssignTypesAsync_ReturnsZeroInstructorCount_WhenNoPercentages()
    {
        // Arrange
        await CreatePercentAssignTypeAsync("Teaching", "Unused Type");

        // Act
        var types = await _service.GetPercentAssignTypesAsync();

        // Assert
        Assert.Single(types);
        Assert.Equal(0, types[0].InstructorCount);
    }

    #endregion

    #region GetPercentAssignTypeAsync Tests

    [Fact]
    public async Task GetPercentAssignTypeAsync_ReturnsType_WhenExists()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");

        // Act
        var result = await _service.GetPercentAssignTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(type.Id, result.Id);
        Assert.Equal("Teaching", result.Class);
        Assert.Equal("Lecture", result.Name);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetPercentAssignTypeAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetPercentAssignTypeAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPercentAssignTypeAsync_IncludesShowOnTemplate()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture", showOnTemplate: false);

        // Act
        var result = await _service.GetPercentAssignTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ShowOnTemplate);
    }

    #endregion

    #region GetPercentAssignTypeClassesAsync Tests

    [Fact]
    public async Task GetPercentAssignTypeClassesAsync_ReturnsDistinctClasses_Ordered()
    {
        // Arrange
        await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePercentAssignTypeAsync("Admin", "Chair");
        await CreatePercentAssignTypeAsync("Teaching", "Lab");
        await CreatePercentAssignTypeAsync("Research", "Grant");

        // Act
        var classes = await _service.GetPercentAssignTypeClassesAsync();

        // Assert
        Assert.Equal(3, classes.Count);
        Assert.Equal("Admin", classes[0]);
        Assert.Equal("Research", classes[1]);
        Assert.Equal("Teaching", classes[2]);
    }

    [Fact]
    public async Task GetPercentAssignTypeClassesAsync_ReturnsEmptyList_WhenNoTypes()
    {
        // Act
        var classes = await _service.GetPercentAssignTypeClassesAsync();

        // Assert
        Assert.Empty(classes);
    }

    #endregion

    #region GetInstructorsByTypeAsync Tests

    [Fact]
    public async Task GetInstructorsByTypeAsync_ReturnsNull_WhenTypeNotFound()
    {
        // Act
        var result = await _service.GetInstructorsByTypeAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstructorsByTypeAsync_ReturnsTypeInfo_WithEmptyInstructors()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");

        // Act
        var result = await _service.GetInstructorsByTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(type.Id, result.TypeId);
        Assert.Equal("Lecture", result.TypeName);
        Assert.Equal("Teaching", result.TypeClass);
        Assert.Empty(result.Instructors);
    }

    [Fact]
    public async Task GetInstructorsByTypeAsync_ReturnsInstructors_WithCorrectNames()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePercentageAsync(1, type.Id, "2024-2025");

        // Act
        var result = await _service.GetInstructorsByTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Instructors);
        var instructor = result.Instructors[0];
        Assert.Equal(1, instructor.PersonId);
        Assert.Equal("John", instructor.FirstName);
        Assert.Equal("Doe", instructor.LastName);
        Assert.Equal("Doe, John", instructor.FullName);
        Assert.Equal("2024-2025", instructor.AcademicYear);
    }

    [Fact]
    public async Task GetInstructorsByTypeAsync_ReturnsMultipleYears_ForSamePerson()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePercentageAsync(1, type.Id, "2023-2024");
        await CreatePercentageAsync(1, type.Id, "2024-2025");

        // Act
        var result = await _service.GetInstructorsByTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Instructors.Count);
        Assert.Contains(result.Instructors, i => i.AcademicYear == "2023-2024");
        Assert.Contains(result.Instructors, i => i.AcademicYear == "2024-2025");
    }

    [Fact]
    public async Task GetInstructorsByTypeAsync_OrdersByAcademicYear_ThenLastName_ThenFirstName()
    {
        // Arrange
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePersonAsync(2, 202410, "Jane", "Smith");
        await CreatePersonAsync(3, 202410, "Alice", "Doe");
        await CreatePercentageAsync(1, type.Id, "2024-2025");
        await CreatePercentageAsync(2, type.Id, "2023-2024");
        await CreatePercentageAsync(3, type.Id, "2024-2025");

        // Act
        var result = await _service.GetInstructorsByTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Instructors.Count);
        Assert.Equal("2023-2024", result.Instructors[0].AcademicYear);
        Assert.Equal("Smith", result.Instructors[0].LastName);
        Assert.Equal("2024-2025", result.Instructors[1].AcademicYear);
        Assert.Equal("Doe", result.Instructors[1].LastName);
        Assert.Equal("Alice", result.Instructors[1].FirstName);
        Assert.Equal("2024-2025", result.Instructors[2].AcademicYear);
        Assert.Equal("Doe", result.Instructors[2].LastName);
        Assert.Equal("John", result.Instructors[2].FirstName);
    }

    [Fact]
    public async Task GetInstructorsByTypeAsync_UsesLatestTermCode_ForPersonName()
    {
        // Arrange - person has records in multiple terms, should use most recent
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202310, "OldFirst", "OldLast");
        await CreatePersonAsync(1, 202410, "NewFirst", "NewLast");
        await CreatePercentageAsync(1, type.Id, "2024-2025");

        // Act
        var result = await _service.GetInstructorsByTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Instructors);
        Assert.Equal("NewFirst", result.Instructors[0].FirstName);
        Assert.Equal("NewLast", result.Instructors[0].LastName);
    }

    [Fact]
    public async Task GetInstructorsByTypeAsync_ReturnsDistinctPersonYearCombinations()
    {
        // Arrange - same person/year with multiple percentages should appear once
        var type = await CreatePercentAssignTypeAsync("Teaching", "Lecture");
        await CreatePersonAsync(1, 202410, "John", "Doe");
        await CreatePercentageAsync(1, type.Id, "2024-2025", 0.3);
        await CreatePercentageAsync(1, type.Id, "2024-2025", 0.2);

        // Act
        var result = await _service.GetInstructorsByTypeAsync(type.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Instructors);
    }

    #endregion
}
