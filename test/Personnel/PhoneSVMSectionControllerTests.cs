using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;

namespace Viper.test.Personnel;

/// <summary>
/// Controller tests for PhoneSVMSectionController. Sections are the page's top-level grouping, so
/// the order they come back in is the order the page renders; unsorted sections fall to the end
/// alphabetically rather than jumping to the front on a null SortOrder.
/// </summary>
public sealed class PhoneSVMSectionControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneSVMSectionController _controller;

    public PhoneSVMSectionControllerTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        _controller = new PhoneSVMSectionController(new PhoneSVMSectionService(_context));
    }

    public void Dispose() => _context.Dispose();

    private async Task<List<SVMSection>> GetSections()
    {
        var result = await _controller.GetSections(TestContext.Current.CancellationToken);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<List<SVMSection>>(okResult.Value);
    }

    [Fact]
    public async Task GetSections_ReturnsSortedSectionsBeforeUnsortedOnes()
    {
        _context.SVMSection.AddRange(
            new SVMSection { SectionId = 1, Name = "Anatomy", SortOrder = null },
            new SVMSection { SectionId = 2, Name = "Dean's Office", SortOrder = 1 },
            new SVMSection { SectionId = 3, Name = "Zoology", SortOrder = 2 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sections = await GetSections();

        Assert.Equal(["Dean's Office", "Zoology", "Anatomy"], sections.Select(s => s.Name));
    }

    [Fact]
    public async Task GetSections_ReturnsNotFound_WhenThereAreNoSections()
    {
        var result = await _controller.GetSections(TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
