using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Controllers;
using Viper.Areas.Students.Models;
using Viper.Classes.SQLContext;
using Viper.Models.Students;

namespace Viper.test.Students;

/// <summary>
/// UpdateClassYear deactivates a student's existing active class year before activating
/// another one. That lookup must be scoped to the student: an unscoped query takes the
/// first active row in the whole table, which belongs to an arbitrary other student.
/// </summary>
public sealed class DvmStudentsControllerClassYearTests : IDisposable
{
    private const int OtherStudentPersonId = 200;
    private const int StudentPersonId = 100;

    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly DvmStudentsController _controller;

    public DvmStudentsControllerClassYearTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        _controller = new DvmStudentsController(_context, _rapsContext);
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    /// <summary>
    /// The other student is seeded first on purpose: the in-memory provider enumerates in
    /// insertion order, so an unscoped FirstOrDefault deterministically picks their row.
    /// </summary>
    private void SeedStudents()
    {
        _context.StudentClassYears.AddRange(
            new StudentClassYear
            {
                StudentClassYearId = 1,
                PersonId = OtherStudentPersonId,
                ClassYear = 2026,
                Active = true,
                Added = DateTime.Now,
            },
            new StudentClassYear
            {
                StudentClassYearId = 2,
                PersonId = StudentPersonId,
                ClassYear = 2025,
                Active = true,
                Added = DateTime.Now,
            },
            new StudentClassYear
            {
                StudentClassYearId = 3,
                PersonId = StudentPersonId,
                ClassYear = 2024,
                Active = false,
                Added = DateTime.Now,
            });
        _context.SaveChanges();
    }

    private async Task ActivateStudentsInactiveYear()
    {
        var update = new StudentClassYearUpdate { StudentClassYearId = 3, Active = true };
        await _controller.UpdateClassYear(2024, StudentPersonId, update);
    }

    [Fact]
    public async Task UpdateClassYear_ActivatingInactiveYear_LeavesOtherStudentUntouched()
    {
        SeedStudents();

        await ActivateStudentsInactiveYear();

        var otherStudent = await _context.StudentClassYears.AsNoTracking()
            .SingleAsync(s => s.StudentClassYearId == 1, TestContext.Current.CancellationToken);
        Assert.True(otherStudent.Active);
    }

    [Fact]
    public async Task UpdateClassYear_ActivatingInactiveYear_DeactivatesStudentsOwnActiveYear()
    {
        SeedStudents();

        await ActivateStudentsInactiveYear();

        var previouslyActive = await _context.StudentClassYears.AsNoTracking()
            .SingleAsync(s => s.StudentClassYearId == 2, TestContext.Current.CancellationToken);
        Assert.False(previouslyActive.Active);
    }

    [Fact]
    public async Task UpdateClassYear_ActivatingInactiveYear_LeavesStudentWithOneActiveYear()
    {
        SeedStudents();

        await ActivateStudentsInactiveYear();

        var activeYears = await _context.StudentClassYears.AsNoTracking()
            .Where(s => s.PersonId == StudentPersonId && s.Active)
            .Select(s => s.StudentClassYearId)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([3], activeYears);
    }

    /// <summary>
    /// The bug could leave a student with two active rows, so the fix deactivates every
    /// active row for that student rather than just the first one it finds.
    /// </summary>
    [Fact]
    public async Task UpdateClassYear_StudentWithMultipleActiveYears_DeactivatesAllOfThem()
    {
        _context.StudentClassYears.AddRange(
            new StudentClassYear
            {
                StudentClassYearId = 1,
                PersonId = StudentPersonId,
                ClassYear = 2025,
                Active = true,
                Added = DateTime.Now,
            },
            new StudentClassYear
            {
                StudentClassYearId = 2,
                PersonId = StudentPersonId,
                ClassYear = 2026,
                Active = true,
                Added = DateTime.Now,
            },
            new StudentClassYear
            {
                StudentClassYearId = 3,
                PersonId = StudentPersonId,
                ClassYear = 2024,
                Active = false,
                Added = DateTime.Now,
            });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await ActivateStudentsInactiveYear();

        var activeYears = await _context.StudentClassYears.AsNoTracking()
            .Where(s => s.PersonId == StudentPersonId && s.Active)
            .Select(s => s.StudentClassYearId)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([3], activeYears);
    }
}
