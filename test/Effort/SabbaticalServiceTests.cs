using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Models.Entities;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

public sealed class SabbaticalServiceTests : IDisposable
{
    private readonly EffortDbContext _context;
    private readonly SabbaticalService _service;

    public SabbaticalServiceTests()
    {
        var effortOptions = new DbContextOptionsBuilder<EffortDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EffortDbContext(effortOptions);
        _service = new SabbaticalService(_context, Mock.Of<ILogger<SabbaticalService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<ViperPerson> CreateViperPersonAsync(int personId, string firstName, string lastName)
    {
        var person = new ViperPerson
        {
            PersonId = personId,
            FirstName = firstName,
            LastName = lastName,
            MothraId = $"M{personId:D9}"
        };
        _context.ViperPersons.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    private async Task<Sabbatical> CreateSabbaticalAsync(int personId, int modifiedBy,
        string? excludeClinical = "202301,202302", string? excludeDidactic = "202303")
    {
        var entity = new Sabbatical
        {
            PersonId = personId,
            ExcludeClinicalTerms = excludeClinical,
            ExcludeDidacticTerms = excludeDidactic,
            ModifiedDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Local),
            ModifiedBy = modifiedBy
        };
        _context.Sabbaticals.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    private async Task<EffortPerson> CreateEffortPersonAsync(int personId, int termCode, string dept)
    {
        var person = new EffortPerson
        {
            PersonId = personId,
            TermCode = termCode,
            FirstName = "Test",
            LastName = "User",
            EffortDept = dept
        };
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    #endregion

    #region GetByPersonIdAsync

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetByPersonIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsDto_WhenFound()
    {
        var modifier = await CreateViperPersonAsync(10, "Admin", "User");
        await CreateSabbaticalAsync(42, modifier.PersonId);

        var result = await _service.GetByPersonIdAsync(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.PersonId);
        Assert.Equal("202301,202302", result.ExcludeClinicalTerms);
        Assert.Equal("202303", result.ExcludeDidacticTerms);
        Assert.Equal("Admin User", result.ModifiedBy);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ModifiedByIsNull_WhenPersonMissing()
    {
        await CreateSabbaticalAsync(42, modifiedBy: 999);

        var result = await _service.GetByPersonIdAsync(42);

        Assert.NotNull(result);
        Assert.Null(result.ModifiedBy);
    }

    #endregion

    #region SaveAsync – Insert Path

    [Fact]
    public async Task SaveAsync_InsertsNewRecord_WhenNoneExists()
    {
        var modifier = await CreateViperPersonAsync(10, "Admin", "User");

        var result = await _service.SaveAsync(42, "202301", "202302", modifier.PersonId);

        Assert.Equal(42, result.PersonId);
        Assert.Equal("202301", result.ExcludeClinicalTerms);
        Assert.Equal("202302", result.ExcludeDidacticTerms);
        Assert.Equal("Admin User", result.ModifiedBy);
        Assert.NotNull(result.ModifiedDate);

        var entity = await _context.Sabbaticals.FirstOrDefaultAsync(s => s.PersonId == 42);
        Assert.NotNull(entity);
    }

    [Fact]
    public async Task SaveAsync_InsertsWithNullTerms()
    {
        var modifier = await CreateViperPersonAsync(10, "Admin", "User");

        var result = await _service.SaveAsync(42, null, null, modifier.PersonId);

        Assert.Null(result.ExcludeClinicalTerms);
        Assert.Null(result.ExcludeDidacticTerms);
    }

    #endregion

    #region SaveAsync – Update Path

    [Fact]
    public async Task SaveAsync_UpdatesExistingRecord()
    {
        var modifier = await CreateViperPersonAsync(10, "Admin", "User");
        await CreateSabbaticalAsync(42, modifier.PersonId,
            excludeClinical: "202301", excludeDidactic: "202302");

        var result = await _service.SaveAsync(42, "202401,202402", "202403", modifier.PersonId);

        Assert.Equal("202401,202402", result.ExcludeClinicalTerms);
        Assert.Equal("202403", result.ExcludeDidacticTerms);

        var count = await _context.Sabbaticals.CountAsync(s => s.PersonId == 42);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveAsync_UpdatesModifiedDate()
    {
        var modifier = await CreateViperPersonAsync(10, "Admin", "User");
        var original = await CreateSabbaticalAsync(42, modifier.PersonId);
        var originalDate = original.ModifiedDate;

        await _service.SaveAsync(42, "new", "new", modifier.PersonId);

        var entity = await _context.Sabbaticals.FirstAsync(s => s.PersonId == 42);
        Assert.True(entity.ModifiedDate > originalDate);
    }

    #endregion

    #region GetPersonDepartmentAsync

    [Fact]
    public async Task GetPersonDepartmentAsync_ReturnsNull_WhenNoPerson()
    {
        var result = await _service.GetPersonDepartmentAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonDepartmentAsync_ReturnsMostRecentDept()
    {
        await CreateEffortPersonAsync(42, 202301, "VME");
        await CreateEffortPersonAsync(42, 202401, "PMI");

        var result = await _service.GetPersonDepartmentAsync(42);

        Assert.Equal("PMI", result);
    }

    #endregion
}
