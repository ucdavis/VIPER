using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Controllers;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.test.Students;

/// <summary>
/// minClassYear comes from the query string and sets how long GetClassYears loops, so its lower
/// bound must be clamped. Term 202609 makes the active class years 2027 to 2030.
/// </summary>
public sealed class DvmStudentsControllerClassYearRangeTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly DvmStudentsController _controller;

    public DvmStudentsControllerClassYearRangeTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        _context.Terms.Add(new Term
        {
            TermCode = 202609,
            AcademicYear = 2026,
            Description = "Fall Semester 2026",
            TermType = "SS",
            CurrentTerm = true,
        });
        _context.SaveChanges();
        _controller = new DvmStudentsController(_context, _rapsContext);
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    [Fact]
    public async Task GetClassYears_ActiveOnly_ReturnsActiveYears()
    {
        var result = await _controller.GetClassYears();

        Assert.Equal([2027, 2028, 2029, 2030], result.Value!);
    }

    [Fact]
    public async Task GetClassYears_NoMinimum_GoesBackTenYears()
    {
        var result = await _controller.GetClassYears(activeOnly: false);

        Assert.Equal(Enumerable.Range(2017, 14), result.Value!);
    }

    [Fact]
    public async Task GetClassYears_MinimumInRange_IsHonored()
    {
        var result = await _controller.GetClassYears(activeOnly: false, minClassYear: 2010);

        Assert.Equal(Enumerable.Range(2010, 21), result.Value!);
    }

    [Theory]
    [InlineData(1927)]
    [InlineData(int.MinValue)]
    public async Task GetClassYears_MinimumFarBelowRange_IsClamped(int minClassYear)
    {
        var result = await _controller.GetClassYears(activeOnly: false, minClassYear: minClassYear);

        Assert.Equal(Enumerable.Range(2007, 24), result.Value!);
    }

    [Fact]
    public async Task GetClassYears_MinimumAboveActiveYears_ReturnsActiveYears()
    {
        var result = await _controller.GetClassYears(activeOnly: false, minClassYear: 2100);

        Assert.Equal([2027, 2028, 2029, 2030], result.Value!);
    }
}
